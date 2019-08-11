using System;
using System.Collections.Generic;
using System.Linq;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class IpAutoBlockerSettings : IValidatable
    {
        #region Properties

        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromDays(1);

        public TimeSpan SyncIntervalSkew { get; set; } = TimeSpan.FromMinutes(3);

        #endregion

        #region Methods

        public IEnumerable<string> IsValid()
        {
            return Enumerable.Empty<string>();
        }

        public override string ToString()
        {
            return $"Sync Interval: '{SyncInterval.TotalHours} hours'\n\t" +
                $"Sync Skew: '{SyncIntervalSkew.TotalMinutes} mins'";
        }

        #endregion
    }
}
