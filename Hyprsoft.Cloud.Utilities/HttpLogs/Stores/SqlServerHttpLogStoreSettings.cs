using System;
using System.Collections.Generic;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Stores
{
    public class SqlServerHttpLogStoreSettings : IValidatable
    {
        #region Properties

        public string ConnectionString { get; set; }

        #endregion

        #region Methods

        public IEnumerable<string> IsValid()
        {
            var errors = new List<string>();

            if (String.IsNullOrWhiteSpace(ConnectionString))
                errors.Add($"'{nameof(ConnectionString)}' cannot be null or whitespace.");

            return errors;
        }

        #endregion
    }
}
