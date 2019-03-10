using Hyprsoft.IpAutoBlocker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyprsoft.Cloud.Utilities.Tests
{
    [TestClass]
    public class IpAutoBlockerConsoleTests
    {
        #region Methods

        [TestMethod]
        public void Encryption()
        {
            const string secret = "1234567890";

            var encrypted = Program.EncryptString(secret);
            Assert.AreEqual(secret, Program.DecryptString(encrypted));
        }

        #endregion
    }
}
