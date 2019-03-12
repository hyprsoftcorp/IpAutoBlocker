using System;
using System.Collections.Generic;
using System.Linq;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class IpAutoBlockerSummary
    {
        public TimeSpan SyncInterval { get; set; }

        public string HttpLogsFilter { get; set; }

        public string HttpTrafficCacheFilter { get; set; }

        public IEnumerable<KeyValuePair<string, int>> HttpTrafficeCache { get; set; } = Enumerable.Empty<KeyValuePair<string, int>>();

        public IEnumerable<IpRestriction> Restrictions { get; set; } = Enumerable.Empty<IpRestriction>();

        public int NewHttpLogEntries { get; set; }

        public override string ToString()
        {
            return $"Sync Interval: '{SyncInterval.TotalHours}' hours\n\t" +
                $"HTTP Traffic Cache: '{HttpTrafficeCache.Count()}'\n\t" +
                $"Restrictions: '{Restrictions.Count()}'\n\t" +
                $"New HTTP Logs: '{NewHttpLogEntries}'";
        }
    }
}
