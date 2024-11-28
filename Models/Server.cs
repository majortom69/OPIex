using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ihatecs.Models
{
    public class Server
    {
        [JsonPropertyName("id")]
        public int ServerID { get; set; }

        [JsonPropertyName("server_name")]
        public string ServerName { get; set; }

        [JsonPropertyName("server_ip")]
        public string ServerIp { get; set; }

        [JsonPropertyName("server_country")]
        public string ServerCountry { get; set; }


    }
}
