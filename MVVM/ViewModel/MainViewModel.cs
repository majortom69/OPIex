using ihatecs.Core;
using ihatecs.Models;
using ihatecs.MVVM.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ihatecs.MVVM.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        // window managment
        public RelayCommand MoveWindowCommand { get; set; }
        public RelayCommand ShutdownWindowCommand { get; set; }
        public RelayCommand MinimizeWindowCommand { get; set; }
        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnProperyChanged();
            }
        }


        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnProperyChanged();
            }
        }

        public SignInViewModel SignInVM { get; set; }
        public HomeViewModel HomeVM { get; set; }

        private async void InitializeAsync()
        {
            // Load session data and check if it exists
            if (SessionManager.LoadFromFile())
            {
                // Fetch servers asynchronously and wait for it to complete
                await SignInVM.FetchServersAsync(SessionManager.Id, SessionManager.JWT);

                // Initialize countries after servers are fetched
                HomeVM.InitializeCountries();

                // Set the initial view to HomeView
                CurrentView = HomeVM;
            }
            else
            {
                // Set initial view to SignInView if no session data is found
                CurrentView = SignInVM;
            }
        }

        public MainViewModel()
        {
            SignInVM = new SignInViewModel(this);
            HomeVM = new HomeViewModel();

            // if
            InitializeAsync();



            Title = "DowngradVPN(v1.1.0)";
            Application.Current.MainWindow.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MoveWindowCommand = new RelayCommand(o => { Application.Current.MainWindow.DragMove(); });
            ShutdownWindowCommand = new RelayCommand(o => {
                Application.Current.MainWindow.Hide();
                //Application.Current.Shutdown(); Environment.Exit(0); 
            });
            MinimizeWindowCommand = new RelayCommand(o=> {  Application.Current.MainWindow.WindowState = WindowState.Minimized; });

        }
    }
}
