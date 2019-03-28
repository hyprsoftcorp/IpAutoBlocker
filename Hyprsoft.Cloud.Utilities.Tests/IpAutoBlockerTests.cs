using Hyprsoft.Cloud.Utilities.Azure;
using Hyprsoft.Cloud.Utilities.HttpLogs;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Tests
{
    [TestClass]
    public class IpAutoBlockerTests
    {
        #region Fields

        private ILogger _logger;

        #endregion

        #region Methods

        [TestInitialize]
        public void Initialize()
        {
            var factory = new LoggerFactory();
            _logger = factory.CreateLogger<IpAutoBlockerTests>();
#pragma warning disable CS0618 // Type or member is obsolete
            factory.AddDebug(LogLevel.Debug);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [TestMethod]
        public async Task CheckDefaultsAndRunWithCustomFilters()
        {
            using (var blocker = new Azure.IpAutoBlocker(_logger, new IpAutoBlockerSettings
            {
                ClientId = "clientid",
                ClientSecret = "clientsecret",
                SubscriptionId = Guid.NewGuid().ToString(),
                Tenant = Guid.NewGuid().ToString(),
                WebsiteName = "MyWebSite"
            }))
            {
                // Make sure we start with a fresh cache.
                await blocker.ClearHttpTrafficCacheAsync();

                // Defaults
                Assert.AreEqual(typeof(LocalHttpLogProvider), blocker.HttpLogProvider.GetType());
                Assert.AreEqual(typeof(AppServiceIpRestrictionsProvider), blocker.IpRestrictionsProvider.GetType());

                blocker.HttpLogProvider = new UnitTestHttpLogProvider();
                Assert.IsNotNull(blocker.HttpLogProvider.Logger);

                blocker.IpRestrictionsProvider = new UnitTestIpRestictionsProvider();
                Assert.IsNotNull(blocker.IpRestrictionsProvider.Logger);
                Assert.AreEqual(0, blocker.IpRestrictionsProvider.Restrictions.Count);

                // This should place 4.4.4.4 in our cache with a count of 3.
                blocker.HttpTrafficCacheFilter = items => items.Where(x => x.Value >= 3);
                // Our default sync interval is 24-hours, so no sync should be performed on this run (meaning no new IP restrictions).
                var summary = await blocker.RunAsync();

                Assert.AreEqual(blocker.HttpLogsFilter.ToString(), summary.HttpLogsFilter);
                Assert.AreEqual(blocker.HttpTrafficCacheFilter.ToString(), summary.HttpTrafficCacheFilter);
                Assert.AreEqual(10, summary.NewHttpLogEntries);

                Assert.AreEqual(1, summary.HttpTrafficeCache.Count());
                Assert.AreEqual(1, summary.HttpTrafficeCache.Where(x => x.Key == "4.4.4.4" && x.Value == 3).Count());
                Assert.AreEqual(2, summary.Restrictions.Count());
                Assert.AreEqual(1, summary.Restrictions.Where(x => x.IpAddress == $"1.1.1.1{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}" && !x.IsNew).Count());
                Assert.AreEqual(1, summary.Restrictions.Where(x => x.IpAddress == $"2.2.2.2{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}" && !x.IsNew).Count());

                // Force a sync.
                blocker.Settings.SyncInterval = TimeSpan.FromTicks(1);
                blocker.HttpLogsFilter = entries => entries.Where(x => x.Status == HttpStatusCode.OK);
                summary = await blocker.RunAsync();

                Assert.AreEqual(10, summary.NewHttpLogEntries);

                Assert.AreEqual(3, summary.HttpTrafficeCache.Count());
                Assert.AreEqual(1, summary.HttpTrafficeCache.Where(x => x.Key == "3.3.3.3" && x.Value == 3).Count());
                Assert.AreEqual(1, summary.HttpTrafficeCache.Where(x => x.Key == "4.4.4.4" && x.Value == 3).Count());
                Assert.AreEqual(1, summary.HttpTrafficeCache.Where(x => x.Key == "5.5.5.5" && x.Value == 3).Count());
                Assert.AreEqual(6, summary.Restrictions.Count());

                Assert.AreEqual(6, summary.Restrictions.Count());
                Assert.AreEqual(1, summary.Restrictions.Where(x => x.IpAddress == $"1.1.1.1{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}" && !x.IsNew).Count());
                Assert.AreEqual(1, summary.Restrictions.Where(x => x.IpAddress == $"2.2.2.2{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}" && !x.IsNew).Count());

                Assert.AreEqual(1, summary.Restrictions.Where(x => x.IpAddress == $"0.0.0.0/0" && x.IsNew).Count());
                Assert.AreEqual(1, summary.Restrictions.Where(x => x.IpAddress == $"3.3.3.3{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}" && x.IsNew).Count());
                Assert.AreEqual(1, summary.Restrictions.Where(x => x.IpAddress == $"4.4.4.4{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}" && x.IsNew).Count());
                Assert.AreEqual(1, summary.Restrictions.Where(x => x.IpAddress == $"5.5.5.5{AppServiceIpRestrictionsProvider.IpAddressCidrBlockSuffix}" && x.IsNew).Count());
            }   // using blocker
        }

        [TestMethod]
        public void BadCtor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Azure.IpAutoBlocker(null, new IpAutoBlockerSettings
            {
                ClientId = "clientid",
                ClientSecret = "clientsecret",
                SubscriptionId = Guid.NewGuid().ToString(),
                Tenant = Guid.NewGuid().ToString(),
                WebsiteName = "MyWebSite"
            }));
            Assert.ThrowsException<ArgumentNullException>(() => new Azure.IpAutoBlocker(_logger, null));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Azure.IpAutoBlocker(_logger, new IpAutoBlockerSettings()));
        }

        #endregion
    }
}
