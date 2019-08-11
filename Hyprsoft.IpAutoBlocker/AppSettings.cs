using Hyprsoft.Cloud.Utilities.Azure;
using Hyprsoft.Cloud.Utilities.HttpLogs.Providers;
using Hyprsoft.Cloud.Utilities.HttpLogs.Stores;
using System;

namespace Hyprsoft.IpAutoBlocker
{
    public class AppSettings
    {
        public bool FirstRun { get; set; } = true;

        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(8);

        public IpAutoBlockerSettings IpAutoBlockerSettings { get; set; } = new IpAutoBlockerSettings();

        public SqlServerHttpLogStoreSettings SqlServerHttpLogStoreSettings { get; set; } = new SqlServerHttpLogStoreSettings();
    }
}
