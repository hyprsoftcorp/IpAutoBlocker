using System;
using System.Net;

namespace Hyprsoft.Cloud.Utilities.HttpLogs
{
    public class HttpLogEntry
    {
        #region Properties

        public DateTime DateTime { get; set; }

        public string SiteName { get; set; }

        public string Method { get; set; }

        public Uri Uri { get; set; }

        public int Port { get; set; }

        public string Username { get; set; }

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public string Cookie { get; set; }

        public string Referer { get; set; }

        public string Host { get; set; }

        public HttpStatusCode Status { get; set; }

        public int SubStatus { get; set; }

        public int BytesReceived { get; set; }

        public int BytesSent { get; set; }

        public TimeSpan Duration { get; set; }

        # endregion

        #region Methods

        public static HttpLogEntry FromString(string source)
        {
            var fields = source.Split(' ');
            if (fields.Length != 19)
                return null;

            return new HttpLogEntry
            {
                DateTime = DateTime.Parse($"{fields[0]} {fields[1]}"),
                SiteName = fields[2],
                Method = fields[3],
                Uri = new Uri($"{(fields[6] == "443" ? "https://" : "http://")}{fields[12]}{(fields[6] == "80" ? String.Empty : ":" + fields[6])}{fields[4]}{(String.IsNullOrWhiteSpace(fields[5]) ? String.Empty : "?" + fields[5])}".ToLower()),
                Port = int.Parse(fields[6]),
                Username = fields[7],
                IpAddress = fields[8],
                UserAgent = fields[9],
                Cookie = fields[10],
                Referer = fields[11],
                Host = fields[12],
                Status = (HttpStatusCode)int.Parse(fields[13]),
                SubStatus = int.Parse(fields[14]),
                BytesSent = int.Parse(fields[16]),
                BytesReceived = int.Parse(fields[17]),
                Duration = TimeSpan.FromMilliseconds(int.Parse(fields[18]))
            };
        }

        public override string ToString()
        {
            return $"Timestamp: {DateTime.ToString("g")}\n\tStatus: {Status}\n\tUri: {Uri}";
        }

        # endregion
    }
}
