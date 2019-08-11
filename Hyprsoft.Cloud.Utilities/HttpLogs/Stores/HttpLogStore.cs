using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Stores
{
    public abstract class HttpLogStore
    {
        #region Properties

        internal ILogger Logger { get; set; }

        #endregion

        #region Methods

        public Task SaveEntriesAsync(IEnumerable<HttpLogEntry> entries, CancellationToken cancellationToken = default)
        {
            var count = entries != null ? entries.Count() : 0;
            Logger?.LogInformation($"Saving '{count}' new HTTP log entries.");

            return count > 0 ? OnSaveEntriesAsync(entries, cancellationToken) : Task.CompletedTask;
        }

        protected abstract Task OnSaveEntriesAsync(IEnumerable<HttpLogEntry> entries, CancellationToken cancellationToken = default);

        #endregion
    }
}
