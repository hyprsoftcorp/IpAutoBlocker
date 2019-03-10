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
                Assert.AreEqual(typeof(LocalHttpLogProvider), blocker.HttpLogProvider.GetType());
                Assert.AreEqual(typeof(AppServiceIpRestrictionsProvider), blocker.IpRestrictionsProvider.GetType());
                Assert.AreEqual(TimeSpan.FromDays(1), blocker.SyncInterval);
                Assert.IsNotNull(blocker.LogEntriesToCacheFilter);
                Assert.IsNotNull(blocker.CacheItemsToIpRestictionsFilter);

                var logProvider = new UnitTestHttpLogProvider();
                blocker.HttpLogProvider = logProvider;
                Assert.IsNotNull(blocker.HttpLogProvider.Logger);
                Assert.AreEqual(9, logProvider.Entries.Count);

                var ipRestrictionsProvider = new UnitTestIpRestictionsProvider();
                blocker.IpRestrictionsProvider = ipRestrictionsProvider;
                Assert.IsNotNull(blocker.IpRestrictionsProvider.Logger);
                Assert.AreEqual(2, ipRestrictionsProvider.Restrictions.Count);

                // By default our IP restrictions are synched every 24 hours, let's force a sync.
                blocker.SyncInterval = TimeSpan.FromTicks(1);
                blocker.CacheItemsToIpRestictionsFilter = items => items.Where(x => x.Value >= 3);
                await blocker.RunAsync();

                Assert.AreEqual(1, ipRestrictionsProvider.Restrictions.Count());    // 404s
                Assert.AreEqual(1, ipRestrictionsProvider.Restrictions.Where(x => x.IpAddress == $"4.4.4.4{AppServiceIpRestrictionsProvider.IpAddressBlockSuffix}").Count());

                blocker.LogEntriesToCacheFilter = entries => entries.Where(x => x.Status == HttpStatusCode.OK);
                await blocker.RunAsync();

                Assert.AreEqual(2, ipRestrictionsProvider.Restrictions.Count());    // 200s
                Assert.AreEqual(1, ipRestrictionsProvider.Restrictions.Where(x => x.IpAddress == $"3.3.3.3{AppServiceIpRestrictionsProvider.IpAddressBlockSuffix}").Count());
                Assert.AreEqual(1, ipRestrictionsProvider.Restrictions.Where(x => x.IpAddress == $"5.5.5.5{AppServiceIpRestrictionsProvider.IpAddressBlockSuffix}").Count());
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
