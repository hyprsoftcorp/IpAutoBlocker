using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Stores
{
    public class NoOpHttpLogStore : HttpLogStore
    {
        protected override Task OnSaveEntriesAsync(IEnumerable<HttpLogEntry> entries, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
