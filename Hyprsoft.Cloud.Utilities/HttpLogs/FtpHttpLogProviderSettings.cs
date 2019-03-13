using System;

namespace Hyprsoft.Cloud.Utilities.HttpLogs
{
    public class FtpHttpLogProviderSettings
    {
        #region Properties

        public string Host { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string LogsFolder { get; set; } = "/LogFiles/http/RawLogs";

        public bool AutoDeleteLogs { get; set; }

        #endregion

        #region Methods

        public bool IsValid()
        {
            return !String.IsNullOrWhiteSpace(Host)
                && !String.IsNullOrWhiteSpace(Username)
                && !String.IsNullOrWhiteSpace(Password)
                && !String.IsNullOrWhiteSpace(LogsFolder);
        }

        public override string ToString()
        {
            return $"Host: '{Host}'\n\t" +
                $"Username: '{Username}'\n\t" +
                $"Auto delete: '{AutoDeleteLogs}'\n\t" +
                $"Logs Folder: '{LogsFolder.ToLower()}'";
        }

        #endregion
    }
}
