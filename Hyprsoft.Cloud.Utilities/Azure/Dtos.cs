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
            return $"WebApps: '{WebApps.Length}'";
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
            return $"Id: {Id}\n\tName: '{Name}'\n\t" +
                $"Type: '{Type}'\n\t" +
                $"Location: '{Location}'\n\t" +
                $"Restrictions: '{Properties.Restrictions.Count}'";
        }
    }

    public class Properties
    {
        [JsonProperty("IpSecurityRestrictions")]
        public List<IpRestriction> Restrictions { get; set; }
    }

    public class IpRestriction
    {
        [JsonIgnore]
        public bool IsNew { get; set; }

        public string IpAddress { get; set; }

        public string Action { get; set; }

        public string Tag { get; set; }

        public int Priority { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return $"IP: {IpAddress}\n\t" +
                $"Name: '{Name}'\n\t" +
                $"Action: '{Action}'\n\t" +
                $"Priority: '{Priority}'\n\t" +
                $"New: '{IsNew}'";
        }
    }
}
