using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Providers
{
    public class FtpHttpLogProvider : LocalHttpLogProvider
    {
        #region Constructors

        public FtpHttpLogProvider(FtpHttpLogProviderSettings settings) : base()
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            var errors = Settings.IsValid();
            if (errors.Count() > 0)
                throw new ArgumentOutOfRangeException($"FTP HTTP log provider settings are missing or invalid. {string.Join(" ", errors)}");
        }

        #endregion

        #region Properties

        public FtpHttpLogProviderSettings Settings { get; }

        #endregion

        #region Methods

        protected override async Task<IEnumerable<HttpLogEntry>> OnGetEntriesAsync(CancellationToken cancellationToken = default)
        {
            // This FtpClient doesn't work on on Linux :-(
            // https://github.com/robinrodricks/FluentFTP/issues/347
            using (var client = new FtpClient(Settings.Host, Settings.Username, Settings.Password)
            {
                EncryptionMode = FtpEncryptionMode.Explicit
            })
            {
                Logger?.LogInformation($"Connecting to FTP host '{Settings.Host}'.");
                await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                Logger?.LogInformation("Connected.  Getting remote files list.");
                foreach (var file in await client.GetListingAsync(Settings.LogsFolder, cancellationToken).ConfigureAwait(false))
                {
                    if (String.Compare(Path.GetExtension(file.FullName), ".log", true) != 0)
                        continue;

                    Logger?.LogInformation($"Downloading remote file '{file.FullName}'.");
                    await client.DownloadFileAsync(Path.Combine(LocalLogsFolder, file.Name), file.FullName, FtpLocalExists.Overwrite, FtpVerify.None, null, cancellationToken).ConfigureAwait(false);
                    // Once our log is downloaded don't allow our delete or rename to be canceled.
                    if (Settings.AutoDeleteLogs)
                    {
                        Logger?.LogInformation($"Deleting remote file '{file.FullName}'.");
                        await client.DeleteFileAsync(file.FullName).ConfigureAwait(false);
                    }
                    else
                    {
                        var dest = $"{Settings.LogsFolder}" +
                            $"{(Settings.LogsFolder.EndsWith("/") ? String.Empty : "/")}" +
                            $"{Path.GetFileNameWithoutExtension(file.Name)}-{Guid.NewGuid().ToString("N")}.bak";
                        Logger?.LogInformation($"Backing up remote file '{file.FullName}' to '{dest}'.");
                        await client.RenameAsync(file.FullName, dest).ConfigureAwait(false);
                    }
                }   // for each file
                Logger?.LogInformation($"Disconnecting from FTP host '{Settings.Host}'.");
                await client.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }   // using ftp client

            return await base.OnGetEntriesAsync(cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}