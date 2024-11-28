using ihatecs.Core;
using ihatecs.Models;
using ihatecs.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using static ihatecs.MVVM.ViewModel.HomeViewModel;

namespace ihatecs.MVVM.ViewModel
{
    public class SignInViewModel : ObservableObject
    {
        private MainViewModel _mainViewModel;
        //private AuthenticationService _authenticationService;

        private string _username;
        private string _password;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnProperyChanged(); // Ensure proper spelling: OnPropertyChanged
            }
        }
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnProperyChanged();
            }
        }


        public RelayCommand SignInCommand { get; set; }


        public SignInViewModel(MainViewModel mainViewModel)
        {
            
            _mainViewModel = mainViewModel;
            SignInCommand = new RelayCommand(async o =>
            {

                var loginSuccess = await LoginAsync(Username, Password, Environment.MachineName);
                if (loginSuccess)
                {
                    await FetchServersAsync(SessionManager.Id, SessionManager.JWT);
                    _mainViewModel.HomeVM.InitializeCountries();


                    _mainViewModel.CurrentView = _mainViewModel.HomeVM;
                } else
                {
                    MessageBox.Show("Obosralsa, wrong login .");
                }
                // TODO : change view if successful login
                
            });
        }



        public static class GlobalData
        {
            public static ResponseJSON SerializedJson { get; set; }
        }
        public async Task<bool> LoginAsync(string username, string password, string computerName)
        {
            using (var client = new HttpClient())
            {
                var endpoint = new Uri("http://147.45.77.19:4040/api/login");
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


        public async Task FetchServersAsync(int userId, string jwtToken)
        {
            using (var client = new HttpClient())
            {
                // Set the endpoint URI, appending the user ID as needed
                var endpoint = new Uri($"http://147.45.77.19:4040/api/servers/{userId}");

                // Add the JWT token to the Authorization header
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                try
                {
                    // Make GET request to the server endpoint
                    var response = await client.GetAsync(endpoint);

                    // Check if request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Deserialize the JSON response
                        var json = await response.Content.ReadAsStringAsync();
                        var serverResponse = JsonSerializer.Deserialize<ResponseJSON>(json);

                        // Access the servers in GlobalData or elsewhere in your app
                        GlobalData.SerializedJson = serverResponse;
                    }
                    else
                    {
                        Console.WriteLine("Failed to fetch servers");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }


        
    }
}
