using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ihatecs.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("user_id")]
        public int UserID { get; set; }

        [JsonPropertyName("user_email")]
        public string UserEmail { get; set; }

        // Store the JWT as SessionKey
        [JsonPropertyName("jwt")]
        public string JWT { get; set; }
    }
}