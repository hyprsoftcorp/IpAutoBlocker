using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.Cloud.Utilities.HttpLogs.Providers
{
    public class LocalHttpLogProvider : HttpLogProvider
    {
        #region Methods

        protected override Task<IEnumerable<HttpLogEntry>> OnGetEntriesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO: Implement yield return.
            var entries = new List<HttpLogEntry>();
            foreach (var file in Directory.GetFiles(LocalLogsFolder, "*.log"))
            {
                Logger?.LogInformation($"Getting entries from HTTP log file '{file}'.");
                using (var stream = File.OpenRead(file))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string line = null;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (String.IsNullOrWhiteSpace(line))
                                break;

                            if (line.StartsWith("#"))
                                continue;

                            var entry = HttpLogEntry.FromString(line);
                            if (entry != null)
                                entries.Add(entry);

                            if (cancellationToken.IsCancellationRequested)
                                break;
                        }   // read line while loop
                    }   // log file stream reader
                }   // log file stream
            }   // for each file

            return Task.FromResult((IEnumerable<HttpLogEntry>)entries);
        }

        #endregion
    }
}
