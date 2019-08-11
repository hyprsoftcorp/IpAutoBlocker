using System;
using System.Collections.Generic;
using System.Linq;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class AppServiceIpRestrictionsProviderSettings : IValidatable
    {
        #region Properties

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Tenant { get; set; }

        public string SubscriptionId { get; set; }

        public string WebsiteName { get; set; }

        #endregion

        #region Methods

        public IEnumerable<string> IsValid()
        {
            var errors = new List<string>();

            if (String.IsNullOrWhiteSpace(ClientId))
                errors.Add($"'{nameof(ClientId)}' cannot be null or whitespace.");

            if (String.IsNullOrWhiteSpace(ClientSecret))
                errors.Add($"'{nameof(ClientSecret)}' cannot be null or whitespace.");

            if (String.IsNullOrWhiteSpace(Tenant))
                errors.Add($"'{nameof(Tenant)}' cannot be null or whitespace.");

            if (String.IsNullOrWhiteSpace(SubscriptionId))
                errors.Add($"'{nameof(SubscriptionId)}' cannot be null or whitespace.");

            if (String.IsNullOrWhiteSpace(WebsiteName))
                errors.Add($"'{nameof(WebsiteName)}' cannot be null or whitespace.");

            return errors;
        }

        public override string ToString()
        {
            return $"Client Id: '{ClientId}'\n\t" +
                $"Tenant: '{Tenant}'\n\t" +
                $"Subscription Id: '{SubscriptionId}'";
        }

        #endregion
    }
}
