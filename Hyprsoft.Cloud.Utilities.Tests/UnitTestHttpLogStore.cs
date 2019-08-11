using Hyprsoft.Cloud.Utilities.HttpLogs;
using Hyprsoft.Cloud.Utilities.HttpLogs.Stores;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Tests
{
    public class UnitTestHttpLogStore : HttpLogStore
    {
        #region Properties

        internal List<HttpLogEntry> Entries { get; } = new List<HttpLogEntry>();

        #endregion

        #region Methods

        protected override Task OnSaveEntriesAsync(IEnumerable<HttpLogEntry> entries, CancellationToken cancellationToken = default)
        {
            Entries.AddRange(entries);

            return Task.CompletedTask;
        }

        #endregion
    }
}