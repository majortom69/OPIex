using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

using Tunnel;

namespace ihatecs
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string ServiceArg = "/service";
        private static Mutex _mutex = null;
        private static EventWaitHandle _eventWaitHandle;

        void App_Startup(object sender, StartupEventArgs e)
        {
            const string appName = "DVPN";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "DVPN_ShowWindow");
            if (!createdNew)
            {
                _eventWaitHandle.Set();
                //MessageBox.Show("Another instance is already running.");

                Environment.Exit(0);
            }

            TaskbarIcon trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            Task.Run(() =>
            {
                while (true)
                {
                    // Wait for the signal from another instance
                    _eventWaitHandle.WaitOne();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.MainWindow.Show();
                    });
                }
            });

            // Check command-line arguments for service mode
            if (e.Args.Length == 3 && e.Args[0] == ServiceArg)
            {
                var configFile = e.Args[1];
                var uiProcessId = int.Parse(e.Args[2]);

                var t = new Thread(() =>
                {
                    try
                    {
                        var currentProcess = Process.GetCurrentProcess();
                        var uiProcess = Process.GetProcessById(uiProcessId);
                        if (uiProcess.MainModule.FileName != currentProcess.MainModule.FileName)
                            return;
                        uiProcess.WaitForExit();
                        Service.Remove(configFile, false); // Adjust Service class usage as needed
                    }
                    catch { }
                });
                t.Start();
                Service.Run(configFile); // Run the service logic
                t.Interrupt();

                // Exit application after running service logic
                Shutdown();
            }
            else
            {
                // If no /service argument, start as regular WPF app
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            string userDirectory = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Config");
            string configFile = Path.Combine(userDirectory, "dvpnwg.conf");

            // Ensure the WireGuard service is stopped
            try
            {
                if (File.Exists(configFile))
                {
                    Tunnel.Service.Remove(configFile, true);
                    Thread.Sleep(1000); // Allow cleanup
                    File.Delete(configFile);
                }
            }
            catch (Exception ex)
            {
                // Log or handle errors
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        




        private void OnShowClicked(object sender, RoutedEventArgs e)
        {
            // Show the main window if hidden
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

            if (mainWindow.WindowState == WindowState.Minimized || !mainWindow.IsVisible)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        }

        private async void OnExitClicked(object sender, RoutedEventArgs e)
        {
           string  userDirectory = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Config");
           string configFile = Path.Combine(userDirectory, "dvpnwg.conf");
           string logFile = Path.Combine(userDirectory, "log.bin");

            await Task.Run(() =>
            {
                Tunnel.Service.Remove(configFile, true);
                Thread.Sleep(1000); // to make shoure that program have enoght timne to finsh the process
                try { File.Delete(configFile); } catch { }
            });
            Application.Current.Shutdown();
        }

    }
}
