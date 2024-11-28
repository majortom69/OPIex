using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ihatecs.Models
{
    public static class SessionManager
    {
        public static int Id { get; private set; }
        public static string Email { get; private set; }
        public static string JWT { get; private set; }

        private static string userDataFile = "user_data.json";

        public static void SetUserSession(int userId, string email, string sessionKey)
        {
            Id = userId;
            Email = email;
            JWT = sessionKey; // This will now store the JWT token
        }

        public static void ClearSession()
        {
            Id = -1;
            Email = string.Empty;
            JWT = string.Empty;
        }

        public static void SaveUserData()
        {
            var userData = new LoginResponse
            {
                UserID = Id,
                UserEmail = Email,
                JWT = JWT
            };

            var json = JsonSerializer.Serialize(userData);
            File.WriteAllText(userDataFile, json);
        }

        public static bool LoadFromFile()
        {
            if (File.Exists(userDataFile))
            {
                var json = File.ReadAllText(userDataFile);
                var userData = JsonSerializer.Deserialize<UserDataFile>(json);
                if (userData != null)
                {
                    Id = userData.Id;
                    Email = userData.Email;
                    JWT = userData.JWT;
                    return true;
                }
            }
            return false;
        }


        private class UserDataFile
        {
            [JsonPropertyName("user_id")]
            public int Id { get; set; }

            [JsonPropertyName("user_email")]
            public string Email { get; set; }

            [JsonPropertyName("jwt")]
            public string JWT { get; set; }
        }


    }
}