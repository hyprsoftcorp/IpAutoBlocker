using System;
using System.Collections.Generic;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Providers
{
    public class FtpHttpLogProviderSettings : IValidatable
    {
        #region Properties

        public string Host { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string LogsFolder { get; set; } = "/LogFiles/http/RawLogs";

        public bool AutoDeleteLogs { get; set; }

        #endregion

        #region Methods

        public IEnumerable<string> IsValid()
        {
            var errors = new List<string>();

            if (String.IsNullOrWhiteSpace(Host))
                errors.Add($"'{nameof(Host)}' cannot be null or whitespace.");

            if (String.IsNullOrWhiteSpace(Username))
                errors.Add($"'{nameof(Username)}' cannot be null or whitespace.");

            if (String.IsNullOrWhiteSpace(Password))
                errors.Add($"'{nameof(Password)}' cannot be null or whitespace.");

            if (String.IsNullOrWhiteSpace(LogsFolder))
                errors.Add($"'{nameof(LogsFolder)}' cannot be null or whitespace.");

            return errors;
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
