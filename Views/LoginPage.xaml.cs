using System.Windows.Controls;
using WpfApp2.ViewModel;

namespace WpfApp2.Views
{
    public partial class LoginPage : Page
    {
        private LoginViewModel _viewModel;

        public LoginPage(NavigationService ns)
        {
            InitializeComponent();
            _viewModel = new LoginViewModel(ns);
            DataContext = _viewModel;

            PasswordBox.PasswordChanged += (s, e) =>
            {
                _viewModel.UserPassword = PasswordBox.Password;
            };
        }
    }
}