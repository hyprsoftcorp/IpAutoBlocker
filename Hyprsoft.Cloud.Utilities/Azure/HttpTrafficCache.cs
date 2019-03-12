using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Hyprsoft.Cloud.Utilities.Azure
{
    public class HttpTrafficCache
    {
        #region CacheData Class

        public class CacheData
        {
            public DateTime LastSyncedUtc { get; set; } = DateTime.UtcNow;

            public Dictionary<string, int> Entries { get; } = new Dictionary<string, int>();
        }

        #endregion

        #region Properties

        public bool IsLoaded { get; private set; }

        public string Filename { get; set; } = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "http-traffic-cache.json");

        public CacheData Cache { get; private set; } = new CacheData();

        #endregion

        #region Methods

        public void Load()
        {
            if (File.Exists(Filename))
                Cache = JsonConvert.DeserializeObject<CacheData>(File.ReadAllText(Filename));

            IsLoaded = true;
        }

        public void Save()
        {
            File.WriteAllText(Filename, JsonConvert.SerializeObject(Cache, Formatting.Indented));
        }

        public override string ToString()
        {
            return $"Loaded: '{IsLoaded}'\n\t" +
                $"Last Synced: '{Cache.LastSyncedUtc}'\n\t" +
                $"Entries: '{Cache.Entries.Count}'\n\t" +
                $"Filename: '{Filename}'";
        }

        #endregion
    }
}
