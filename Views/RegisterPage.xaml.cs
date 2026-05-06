using System.Windows.Controls;
using WpfApp2.ViewModel;

namespace WpfApp2.Views
{
    public partial class RegisterPage : Page
    {
        private RegisterViewModel _viewModel;

        public RegisterPage(NavigationService ns)
        {
            InitializeComponent();
            _viewModel = new RegisterViewModel(ns);
            DataContext = _viewModel;

            // Bind PasswordBox with immediate update
            PasswordBox.PasswordChanged += (s, e) =>
            {
                _viewModel.UserPassword = PasswordBox.Password;
            };

            ConfirmPasswordBox.PasswordChanged += (s, e) =>
            {
                _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            };
        }
    }
}