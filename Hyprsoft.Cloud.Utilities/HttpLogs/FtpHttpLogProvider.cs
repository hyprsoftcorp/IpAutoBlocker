using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.HttpLogs
{
    public class FtpHttpLogProvider : LocalHttpLogProvider
    {
        #region Fields

        private readonly FtpHttpLogProviderSettings _settings;

        #endregion

        #region Constructors

        public FtpHttpLogProvider(FtpHttpLogProviderSettings settings) : base()
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (!_settings.IsValid())
                throw new ArgumentOutOfRangeException("FTP HTTP log provider settings are missing or invalid.");
        }

        #endregion

        #region Methods

        protected override async Task<IEnumerable<HttpLogEntry>> OnGetEntriesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // This FtpClient doesn't work on on Linux :-(
            // https://github.com/robinrodricks/FluentFTP/issues/347
            using (var client = new FtpClient(_settings.Host, _settings.Username, _settings.Password)
            {
                EncryptionMode = FtpEncryptionMode.Explicit
            })
            {
                Logger?.LogInformation($"Connecting to FTP host '{_settings.Host}'.");
                await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                Logger?.LogInformation("Connected.  Getting remote files list.");
                foreach (var file in await client.GetListingAsync(_settings.LogsFolder, cancellationToken).ConfigureAwait(false))
                {
                    Logger?.LogInformation($"Downloading remote file '{file.FullName}'.");
                    await client.DownloadFileAsync(Path.Combine(LocalLogsFolder, file.Name), file.FullName, FtpLocalExists.Overwrite, FtpVerify.None, null, cancellationToken).ConfigureAwait(false);
                    Logger?.LogInformation($"Deleting remote file '{file.FullName}'.");
                    // Once our log is downloaded don't allow our delete to be cancelled.
                    await client.DeleteFileAsync(file.FullName).ConfigureAwait(false);
                    await client.DisconnectAsync().ConfigureAwait(false);
                }   // for each file
            }   // using ftp client

            return await base.OnGetEntriesAsync(cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}