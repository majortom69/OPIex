using ihatecs.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ihatecs.MVVM.View
{
    /// <summary>
    /// Interaction logic for SignInView.xaml
    /// </summary>
    public partial class SignInView : UserControl
    {
        public SignInView()
        {
            InitializeComponent();
        }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                // Set the password in the ViewModel
                (DataContext as SignInViewModel).Password = passwordBox.Password;
            }
        }


        //private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        //{
        //    if (DataContext is SignInViewModel viewModel)
        //    {
        //        viewModel.Password = ((PasswordBox)sender).Password;
        //    }
        //}
    }
}
