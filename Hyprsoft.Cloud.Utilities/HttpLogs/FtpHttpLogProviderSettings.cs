using System;

namespace Hyprsoft.Cloud.Utilities.HttpLogs
{
    public class FtpHttpLogProviderSettings
    {
        #region Fields

        public string Host { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string LogsFolder { get; set; } = "/LogFiles/http/RawLogs";

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
            return $"Host: {Host}\n\tUsername: {Username}\n\tPassword: *****\n\tLogs Folder: {LogsFolder}";
        }

        #endregion
    }
}
