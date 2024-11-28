using ihatecs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ihatecs.Services
{
    public class AuthenticationService
    {
        public async Task<bool> LoginAsync(string username, string password, string computerName)
        {
            using (var client = new HttpClient())
            {
                var endpoint = new Uri("http://localhost:4040/api/login");
                var loginData = new { email = username, password = password, device = computerName };
                var jsonContent = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

                try
                {
                    var result = await client.PostAsync(endpoint, jsonContent);
                    if (result.IsSuccessStatusCode)
                    {
                        var json = await result.Content.ReadAsStringAsync();
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json);

                        if (loginResponse != null)
                        {
                            // Store JWT and user data in session manager
                            SessionManager.SetUserSession(loginResponse.UserID, username, loginResponse.JWT);
                            SessionManager.SaveUserData();
                        }
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}

