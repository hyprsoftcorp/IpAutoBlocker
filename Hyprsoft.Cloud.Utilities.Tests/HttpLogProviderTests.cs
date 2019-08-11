using Hyprsoft.Cloud.Utilities.HttpLogs.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Tests
{
    [TestClass]
    public class HttpLogProviderTests
    {
        #region Methods

        [TestMethod]
        public async Task LocalProviderEntriesCount()
        {
            var provider = new LocalHttpLogProvider()
            {
                LocalLogsFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            };
            Assert.IsNull(provider.Logger);
            var entries = await provider.GetEntriesAsync();
            Assert.AreEqual(2059, entries.Count());
        }

        [TestMethod]
        public async Task NoLogs()
        {
            var provider = new LocalHttpLogProvider()
            {
                LocalLogsFolder = Path.Combine(Path.GetTempPath(), "test")
            };
            Assert.IsNull(provider.Logger);
            var entries = await provider.GetEntriesAsync();
            Assert.AreEqual(0, entries.Count());

            if (Directory.Exists(provider.LocalLogsFolder))
                Directory.Delete(provider.LocalLogsFolder);
        }

        [TestMethod]
        public void BadFtpProviderCtor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new FtpHttpLogProvider(null));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new FtpHttpLogProvider(new FtpHttpLogProviderSettings()));
        }

        #endregion
    }
}
