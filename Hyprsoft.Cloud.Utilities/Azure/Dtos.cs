using Newtonsoft.Json;
using System.Collections.Generic;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class WebAppsList
    {
        [JsonProperty("Value")]
        public WebApp[] WebApps { get; set; }

        public override string ToString()
        {
            return $"WebApps: {WebApps.Length}";
        }
    }

    public class WebApp
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string Location { get; set; }

        public Properties Properties { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}\n\tName: {Name}\n\tType: {Type}\n\tLocation: {Location}\n\tRestrictions: {Properties.Restrictions.Count}";
        }
    }

    public class Properties
    {
        [JsonProperty("IpSecurityRestrictions")]
        public List<IpRestriction> Restrictions { get; set; }
    }

    public class IpRestriction
    {
        public string IpAddress { get; set; }

        public string Action { get; set; }

        public string Tag { get; set; }

        public int Priority { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return $"IP: {IpAddress}\n\tName: {Name}\n\tAction: {Action}\n\tPriority: {Priority}\n\tTag: {Tag}";
        }
    }
}
