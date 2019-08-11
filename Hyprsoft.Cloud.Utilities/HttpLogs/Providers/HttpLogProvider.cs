using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Providers
{
    public abstract class HttpLogProvider
    {
        #region Fields

        private string _localLogsFolder;

        #endregion

        #region Constructors

        public HttpLogProvider()
        {
            LocalLogsFolder = Path.Combine(Path.GetTempPath(), "httplogs");
        }

        #endregion

        #region Properties

        internal ILogger Logger { get; set; }

        public string LocalLogsFolder
        {
            get { return _localLogsFolder; }
            set
            {
                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);

                _localLogsFolder = value;
            }
        }

        #endregion

        #region Methods

        public Task<IEnumerable<HttpLogEntry>> GetEntriesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger?.LogInformation("Retrieving new HTTP traffic logs.");
            return OnGetEntriesAsync(cancellationToken);
        }

        protected abstract Task<IEnumerable<HttpLogEntry>> OnGetEntriesAsync(CancellationToken CancellationToken = default(CancellationToken));

        public override string ToString()
        {
            return $"Logs Folder: {LocalLogsFolder}";
        }

        #endregion
    }
}
