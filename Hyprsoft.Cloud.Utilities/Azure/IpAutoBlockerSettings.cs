using System;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class IpAutoBlockerSettings
    {
        #region Properties

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Tenant { get; set; }

        public string SubscriptionId { get; set; }

        public string WebsiteName { get; set; }

        #endregion

        #region Methods

        public bool IsValid()
        {
            return !String.IsNullOrWhiteSpace(ClientId)
                && !String.IsNullOrWhiteSpace(ClientSecret)
                && !String.IsNullOrWhiteSpace(Tenant)
                && !String.IsNullOrWhiteSpace(SubscriptionId)
                && !String.IsNullOrWhiteSpace(WebsiteName);
        }

        public override string ToString()
        {
            return $"Client Id: '{ClientId}'\n\t" +
                $"Tenant: '{Tenant}'\n\t" +
                $"Subscription Id: '{SubscriptionId}'\n\t" +
                $"Website Name: '{WebsiteName}'";
        }

        #endregion
    }
}
