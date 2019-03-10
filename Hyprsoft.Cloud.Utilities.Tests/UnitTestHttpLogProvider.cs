using Hyprsoft.Cloud.Utilities.HttpLogs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.Tests
{
    public class UnitTestHttpLogProvider : HttpLogProvider
    {
        public UnitTestHttpLogProvider()
        {
            Entries = new List<HttpLogEntry>
            {
                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /DebugConsole X-ARR-LOG-ID=6fdb40ea-8015-408d-acdb-1acacb387eb2 443 - 3.3.3.3 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/ hyprsoftweb.scm.azurewebsites.net 200 0 0 17898 1342 203"),
                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /Content/Styles/FileBrowser.css X-ARR-LOG-ID=48cf1318-482e-4a87-b7d9-a0cfb5192654 443 - 3.3.3.3 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/DebugConsole hyprsoftweb.scm.azurewebsites.net 200 0 0 5327 1311 75"),
                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /Content/Scripts/ext-modelist.js X-ARR-LOG-ID=59c65680-335a-4b59-b5d2-09cedc1ba3fe 443 - 3.3.3.3 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/DebugConsole hyprsoftweb.scm.azurewebsites.net 200 0 0 3953 1299 124"),

                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /DebugConsole X-ARR-LOG-ID=6fdb40ea-8015-408d-acdb-1acacb387eb2 443 - 4.4.4.4 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/ hyprsoftweb.scm.azurewebsites.net 404 0 0 17898 1342 203"),
                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /Content/Styles/FileBrowser.css X-ARR-LOG-ID=48cf1318-482e-4a87-b7d9-a0cfb5192654 443 - 4.4.4.4 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/DebugConsole hyprsoftweb.scm.azurewebsites.net 404 0 0 5327 1311 75"),
                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /Content/Scripts/ext-modelist.js X-ARR-LOG-ID=59c65680-335a-4b59-b5d2-09cedc1ba3fe 443 - 4.4.4.4 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/DebugConsole hyprsoftweb.scm.azurewebsites.net 404 0 0 3953 1299 124"),

                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /DebugConsole X-ARR-LOG-ID=6fdb40ea-8015-408d-acdb-1acacb387eb2 443 - 5.5.5.5 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/ hyprsoftweb.scm.azurewebsites.net 200 0 0 17898 1342 203"),
                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /Content/Styles/FileBrowser.css X-ARR-LOG-ID=48cf1318-482e-4a87-b7d9-a0cfb5192654 443 - 5.5.5.5 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/DebugConsole hyprsoftweb.scm.azurewebsites.net 200 0 0 5327 1311 75"),
                HttpLogEntry.FromString("2019-03-03 14:47:08 ~1HYPRSOFTWEB GET /Content/Scripts/ext-modelist.js X-ARR-LOG-ID=59c65680-335a-4b59-b5d2-09cedc1ba3fe 443 - 5.5.5.5 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/72.0.3626.119+Safari/537.36 ARRAffinity=7cb5014c4c8f87dcecaa92df96fe161a4a66f9618f642fc020d5546b9c0aa883 https://hyprsoftweb.scm.azurewebsites.net/DebugConsole hyprsoftweb.scm.azurewebsites.net 200 0 0 3953 1299 124")
            };
        }

        internal List<HttpLogEntry> Entries { get; }

        protected override Task<IEnumerable<HttpLogEntry>> OnGetEntriesAsync(CancellationToken CancellationToken = default(CancellationToken))
        {
            return Task.FromResult((IEnumerable<HttpLogEntry>)Entries);
        }
    }
}
