using ihatecs.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.ComponentModel;
using Serilog;
using System.Diagnostics;
using ihatecs.Models;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using static ihatecs.MVVM.ViewModel.SignInViewModel;
using System.Text.Json.Serialization;

namespace ihatecs.MVVM.ViewModel
{
    public class HomeViewModel : ObservableObject
    {
        private static readonly string userDirectory = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Config");
        private static readonly string configFile = Path.Combine(userDirectory, "dvpnwg.conf");
        private static readonly string logFile = Path.Combine(userDirectory, "log.bin");
        private Tunnel.Ringlogger log;
        private Thread logPrintingThread, transferUpdateThread;
        private volatile bool threadsRunning;
        private bool connected;


        private string _log;
        public string Log
        {
            get { return _log; }
            set
            {
                _log = value;
                OnProperyChanged();
            }
        }

        private bool _isEnabledButton;
        public bool isEnabledButton
        {
            get { return _isEnabledButton; }
            set
            {
                _isEnabledButton = value;
                OnProperyChanged();
            }
        }


        private int _selectedCountryIndex;
        public int SelectedCountryIndex
        {
            get { return _selectedCountryIndex; }
            set
            {
                _selectedCountryIndex = value;
                OnProperyChanged();
            }
        }

        public ObservableCollection<CountryItem> Countries { get; set; }

        public RelayCommand ConnectCommand { get; set; }

        public HomeViewModel()
        {
            Countries = new ObservableCollection<CountryItem>();
            InitializeCountries(); // Initialize countries after data is fetched
            SelectedCountryIndex = Countries.Any() ? 0 : -1; // Select first if available



            //FetchServersAsync(SessionManager.Id, SessionManager.JWT);
            Log = string.Empty;
            isEnabledButton = true;
            ConnectCommand = new RelayCommand(async o => {
                



                await ToggleConnection();
            });

            CreateConfigDirectory();
            try { File.Delete(logFile); } catch { }

            log = new Tunnel.Ringlogger(logFile, "GUI");
            threadsRunning = true;

            logPrintingThread = new Thread(TailLog);
            logPrintingThread.Start();

            transferUpdateThread = new Thread(TailTransfer);
            transferUpdateThread.Start();
        }

        private void CreateConfigDirectory()
        {
            var ds = new DirectorySecurity();
            ds.SetSecurityDescriptorSddlForm("O:BAG:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)");
            FileSystemAclExtensions.CreateDirectory(ds, userDirectory);
        }

        private void TailLog()
        {
            var cursor = Tunnel.Ringlogger.CursorAll;
            while (threadsRunning)
            {
                var lines = log.FollowFromCursor(ref cursor);
                foreach (var line in lines)
                {
                    AppendLog(line);
                }
                Thread.Sleep(300);
            }
        }

        private void TailTransfer()
        {
            Tunnel.Driver.Adapter adapter = null;
            while (threadsRunning)
            {
                if (adapter == null)
                {
                    while (threadsRunning)
                    {
                        try
                        {
                            adapter = Tunnel.Service.GetAdapter(configFile);
                            break;
                        }
                        catch
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }

                if (adapter == null)
                    continue;

                try
                {
                    ulong rx = 0, tx = 0;
                    var config = adapter.GetConfiguration();
                    foreach (var peer in config.Peers)
                    {
                        rx += peer.RxBytes;
                        tx += peer.TxBytes;
                    }
                    Thread.Sleep(1000);
                }
                catch { adapter = null; }
            }
        }

        private async Task<string> GenerateNewConfig(WireGuardConfig wgconfig)
        {
            log.Write("Generating keys");

            // Generate WireGuard keypair
            var keys = Tunnel.Keypair.Generate();

            log.Write($"Public Key: {keys.Public}");
            log.Write("Exchanging keys with server");

            // Create a TCP connection to your server
            using (var client = new TcpClient())
            {
                // Connect to your WireGuard server using the IP and port from `config`
                await client.ConnectAsync(wgconfig.ServerIP, wgconfig.SkePort);

                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    // Send the public key to the server
                    var pubKeyBytes = Encoding.UTF8.GetBytes(keys.Public + "\n");
                    await stream.WriteAsync(pubKeyBytes, 0, pubKeyBytes.Length);
                    await stream.FlushAsync();

                    var response = await reader.ReadLineAsync();
                    var ret = response.Split(':');

                    client.Close();

                    var status = ret.Length >= 1 ? ret[0] : "";
                    var serverPublicKey = ret.Length >= 2 ? ret[1] : "";
                    var serverPort = ret.Length >= 3 ? ret[2] : "";
                    var internalIP = ret.Length >= 4 ? ret[3] : "";

                   
                    if (status != "OK")
                        throw new InvalidOperationException($"Server status is {status}");

                   
                    var configText = string.Format(
                        "[Interface]\nPrivateKey = {0}\nAddress = {1}/24\nDNS = {2}, {3}\n\n" +
                        "[Peer]\nPublicKey = {4}\nEndpoint = {5}:{6}\nAllowedIPs = 0.0.0.0/0\n",
                        keys.Private, internalIP, wgconfig.FDNS, wgconfig.SDNS, serverPublicKey, wgconfig.ServerIP, serverPort
                    );

                    return configText;
                }
            }
        }

        private async Task ToggleConnection()
        {
            if (connected)
            {
                isEnabledButton = false;
                await Task.Run(() =>
                {
                    Tunnel.Service.Remove(configFile, true);
                    Thread.Sleep(1000);
                    try { File.Delete(configFile); } catch { }
                });
                AppendLog("Disconnected");
                isEnabledButton = true;
                connected = false;
            }
            else
            {
                var selectedCountry = Countries[SelectedCountryIndex];

            

                var wireGuardConfig = await FetchWireGuardConfigAsync(selectedCountry.Id, SessionManager.JWT);
                isEnabledButton = false;
                try
                {
                    var config = await GenerateNewConfig(wireGuardConfig);
                    await File.WriteAllBytesAsync(configFile, Encoding.UTF8.GetBytes(config));
                    await Task.Run(() => Tunnel.Service.Add(configFile, true));
                    connected = true;
                    AppendLog("Connected");
                }
                catch (Exception ex)
                {
                    log.Write(ex.Message);
                    try { File.Delete(configFile); } catch { }
                }
                isEnabledButton = true;
            }
        }

        public void Dispose()
        {
            threadsRunning = false;
            logPrintingThread?.Join();
            transferUpdateThread?.Join();
        }


        public void AppendLog(string logMessage)
        {
            Log += $"{DateTime.Now}: {logMessage}{Environment.NewLine}";
        }



        public class CountryItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Country { get; set; }
            public string Ip { get; set; }
        }

        public void InitializeCountries()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var servers = GlobalData.SerializedJson?.Servers;
                if (servers != null)
                {
                    foreach (var server in servers)
                    {
                        Countries.Add(new CountryItem
                        {
                            Id = server.ServerID,
                            Name = server.ServerName,
                            Country = "../../Images/" + server.ServerCountry + ".png",
                            Ip = server.ServerIp
                        });
                    }
                }
            });
            SelectedCountryIndex = Countries.Any() ? 0 : -1;
        }

        public class WireGuardConfig
        {
            [JsonPropertyName("server_ip")]
            public string ServerIP { get; set; }

            [JsonPropertyName("fDNS")]
            public string FDNS { get; set; }

            [JsonPropertyName("sDNS")]
            public string SDNS { get; set; }

            [JsonPropertyName("ske_port")]
            public int SkePort { get; set; }
        }


        public async Task<WireGuardConfig> FetchWireGuardConfigAsync(int serverId, string jwtToken)
        {
            using (var client = new HttpClient())
            {
                // Set the endpoint URI, appending the server ID
                var endpoint = new Uri($"http://147.45.77.19:4040/api/wireguard/{serverId}");

                // Add the JWT token to the Authorization header
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                try
                {
                    // Make GET request to the server endpoint
                    var response = await client.GetAsync(endpoint);

                    // Check if request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Deserialize the JSON response into WireGuardConfig
                        var json = await response.Content.ReadAsStringAsync();
                        var config = JsonSerializer.Deserialize<WireGuardConfig>(json);

                        return config; // Return the WireGuard configuration
                    }
                    else
                    {
                        Console.WriteLine("Failed to fetch WireGuard configuration");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                return null; // Return null if the fetch failed
            }
        }



        //public async Task LoadWireGuardConfig(int serverId, string jwtToken)
        //{
            
        //    AppendLog($"Server IP: {wireGuardConfig.ServerIP}");
        //    AppendLog($"First DNS: {wireGuardConfig.FDNS}");
        //    AppendLog($"Second DNS: {wireGuardConfig.SDNS}");
        //    AppendLog($"Port: {wireGuardConfig.SkePort}");
        //}

        //public async Task FetchServersAsync(int userId, string jwtToken)
        //{
        //    using (var client = new HttpClient())
        //    {
        //        // Set the endpoint URI, appending the user ID as needed
        //        var endpoint = new Uri($"http://localhost:4040/api/servers/{userId}");

        //        // Add the JWT token to the Authorization header
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

        //        try
        //        {
        //            // Make GET request to the server endpoint
        //            var response = await client.GetAsync(endpoint);

        //            // Check if request was successful
        //            if (response.IsSuccessStatusCode)
        //            {
        //                // Deserialize the JSON response
        //                var json = await response.Content.ReadAsStringAsync();
        //                var serverResponse = JsonSerializer.Deserialize<ResponseJSON>(json);

        //                // Access the servers in GlobalData or elsewhere in your app
        //                GlobalData.SerializedJson = serverResponse;
        //            }
        //            else
        //            {
        //                Console.WriteLine("Failed to fetch servers");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"An error occurred: {ex.Message}");
        //        }
        //    }
        //}

    }
}
