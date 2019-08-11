using System;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Hyprsoft.Cloud.Utilities.HttpLogs
{
    public class HttpLogEntry
    {
        #region Properties

        [Required, Key]
        public int Id { get; set; }

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        public string SiteName { get; set; }

        [Required]
        public string Method { get; set; }

        [Required]
        public string Uri { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string IpAddress { get; set; }

        [Required]
        public string UserAgent { get; set; }

        [Required]
        public string Cookie { get; set; }

        [Required]
        public string Referer { get; set; }

        [Required]
        public string Host { get; set; }

        [Required]
        public HttpStatusCode Status { get; set; }

        [Required]
        public int SubStatus { get; set; }

        [Required]
        public int BytesReceived { get; set; }

        [Required]
        public int BytesSent { get; set; }

        [Required]
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
                Uri = $"{(fields[6] == "443" ? "https://" : "http://")}{fields[12]}{(fields[6] == "80" ? String.Empty : ":" + fields[6])}{fields[4]}{(String.IsNullOrWhiteSpace(fields[5]) ? String.Empty : "?" + fields[5])}".ToLower(),
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
            return $"Timestamp: '{DateTime}'\n\t" +
                $"Status: '{Status}'\n\t" +
                $"Uri: '{Uri}'";
        }

        # endregion
    }
}
