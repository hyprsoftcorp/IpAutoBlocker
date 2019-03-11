using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Net.Http;
using Hyprsoft.Cloud.Utilities.HttpLogs;
using System.Linq.Expressions;

[assembly: InternalsVisibleTo("Hyprsoft.Cloud.Utilities.Tests")]
namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class IpAutoBlocker : IDisposable
    {
        #region Properties

        private readonly ILogger _logger;
        private readonly IpAutoBlockerSettings _settings;

        private bool _isDisposed;
        private HttpLogProvider _httpLogProvider;
        private IpRestrictionsProvider _ipRestrictionsProvider;
        private HttpTrafficCache _httpTrafficCache = new HttpTrafficCache();
        private HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("https://api.ipify.org/") };

        #endregion

        #region Constructors

        public IpAutoBlocker(ILogger logger, IpAutoBlockerSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (!_settings.IsValid())
                throw new ArgumentOutOfRangeException("IP restriction mananger settings are missing or invalid.");

            var product = (((AssemblyProductAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product);
            var version = (((AssemblyInformationalVersionAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
            _logger.LogInformation($"{product} v{version}");

            HttpLogProvider = new LocalHttpLogProvider();
            IpRestrictionsProvider = new AppServiceIpRestrictionsProvider(settings);
        }

        #endregion

        #region Properties

        public HttpLogProvider HttpLogProvider
        {
            get { return _httpLogProvider; }
            set
            {
                _httpLogProvider = value;
                _httpLogProvider.Logger = _logger;
            }
        }

        public IpRestrictionsProvider IpRestrictionsProvider
        {
            get { return _ipRestrictionsProvider; }
            set
            {
                _ipRestrictionsProvider = value;
                _ipRestrictionsProvider.Logger = _logger;
            }
        }

        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromDays(1);

        public Expression<Func<IEnumerable<HttpLogEntry>, IEnumerable<HttpLogEntry>>> LogEntriesToCacheFilter { get; set; } = entries => entries.Where(entry => entry.Status == HttpStatusCode.NotFound);

        public Expression<Func<Dictionary<string, int>, IEnumerable<KeyValuePair<string, int>>>> CacheItemsToIpRestictionsFilter { get; set; } = items => items.Where(item => item.Value >= 25);

        #endregion

        #region Methods

        public async Task RunAsync(CancellationToken token = default(CancellationToken))
        {
            _logger.LogInformation($"IP Auto Blocker running using:\n\t" +
                $"HTTP Log Provider: '{HttpLogProvider.GetType().Name}'\n\t" +
                $"IP Restrictions Provider: '{IpRestrictionsProvider.GetType().Name}'\n\t" +
                $"Azure Web App: '{_settings.WebsiteName}'\n\t" +
                $"Logs Filter: '{LogEntriesToCacheFilter.ToString()}'\n\t" +
                $"Cache Filter: '{CacheItemsToIpRestictionsFilter.ToString()}'\n\t" +
                $"Sync Interval: '{SyncInterval.TotalHours}' hours.");

            if (!IpRestrictionsProvider.IsInitialized)
                await IpRestrictionsProvider.InitializeAsync(token).ConfigureAwait(false);
            await IpRestrictionsProvider.LoadAsync(token).ConfigureAwait(false);
            _logger.LogInformation($"Found '{IpRestrictionsProvider.Restrictions.Count}' existing IP restrictions.");

            if (!_httpTrafficCache.IsLoaded)
            {
                _logger.LogInformation("Loading HTTP traffic cache.");
                _httpTrafficCache.Load();
                _logger.LogInformation($"Found '{_httpTrafficCache.Cache.Entries.Count}' HTTP traffic cache items last synced at '{_httpTrafficCache.Cache.LastSyncedUtc.ToLocalTime()}'.");
            }

            var entries = await HttpLogProvider.GetEntriesAsync(token).ConfigureAwait(false);
            _logger.LogInformation($"Found '{entries.Count()}' new HTTP log entries.");

            await UpdateHttpTrafficCacheAsync(entries);

            // If we get to this point we want to delete our logs so they aren't reprocessed later.
            if (Directory.Exists(HttpLogProvider.LocalLogsFolder))
            {
                _logger.LogInformation($"Removing local HTTP logs folder '{HttpLogProvider.LocalLogsFolder}'.");
                Directory.Delete(HttpLogProvider.LocalLogsFolder, true);
            }   // Local logs folder exists?

            if (token.IsCancellationRequested)
                return;

            await AddIpRestictionsAsync(token);
        }

        private async Task UpdateHttpTrafficCacheAsync(IEnumerable<HttpLogEntry> entries)
        {
            var myIp = await _httpClient.GetStringAsync("/").ConfigureAwait(false);
            var entiresToCache = LogEntriesToCacheFilter.Compile().Invoke(entries).Where(x => x.IpAddress != myIp);
            _logger.LogInformation($"Updating HTTP traffic cache with '{entiresToCache.Count()}' HTTP log entries (excludes traffic for '{myIp}').");
            foreach (var entry in entiresToCache)
            {
                if (!_httpTrafficCache.Cache.Entries.ContainsKey(entry.IpAddress))
                    _httpTrafficCache.Cache.Entries.Add(entry.IpAddress, 0);

                _httpTrafficCache.Cache.Entries[entry.IpAddress]++;
                _logger.LogInformation($"IP address '{entry.IpAddress}' count is '{_httpTrafficCache.Cache.Entries[entry.IpAddress]}'.");
            }   // for each http log entry

            _logger.LogInformation("Saving HTTP traffic cache.");
            _httpTrafficCache.Save();
        }

        private async Task AddIpRestictionsAsync(CancellationToken token)
        {
            if (_httpTrafficCache.Cache.LastSyncedUtc.Add(SyncInterval) > DateTime.UtcNow)
                return;

            var items = CacheItemsToIpRestictionsFilter.Compile().Invoke(_httpTrafficCache.Cache.Entries).ToList();
            _logger.LogInformation($"Found '{items.Count}' HTTP traffic cache items to create IP restrictions for.");

            if (items.Count > 0)
            {
                var anyNewRestictions = false;
                // Add new IP restrictions for any IP address matching our CacheItemsToIpRestictionsFilter in the last SyncInterval.
                foreach (var item in items)
                {
                    // Skip any IP address that already has a restriction.
                    if (IpRestrictionsProvider.Restrictions.Any(x => x.IpAddress == $"{item.Key}{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}"))
                        continue;

                    var restriction = new IpRestriction
                    {
                        IpAddress = $"{item.Key}{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}",
                        Action = "Deny",
                        Priority = 1,
                        Name = "Block"
                    };
                    _logger.LogInformation($"Adding new IP restriction for '{restriction.IpAddress}' with action '{restriction.Action}'.");
                    IpRestrictionsProvider.Restrictions.Add(restriction);
                    anyNewRestictions = true;
                }   // for each ip restriction

                if (anyNewRestictions)
                    await IpRestrictionsProvider.SaveAsync(token).ConfigureAwait(false);
            }   // any new IP restrictions?

            _httpTrafficCache.Cache.LastSyncedUtc = DateTime.UtcNow;
            _logger.LogInformation($"Clearing HTTP traffic cache and setting last synced to '{_httpTrafficCache.Cache.LastSyncedUtc.ToLocalTime()}'.");
            _httpTrafficCache.Cache.Entries.Clear();
            _logger.LogInformation("Saving HTTP traffic cache.");
            _httpTrafficCache.Save();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            // Managed resources.
            if (disposing)
            {
                _httpClient?.Dispose();
                IpRestrictionsProvider?.Dispose();
            }

            // Unmanaged resources.

            _isDisposed = true;
        }

        #endregion
    }
}
