using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp2.Views;


namespace WpfApp2.ViewModel
{
    public class HomeViewModel
    {
        private readonly NavigationService _ns;

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }

        public HomeViewModel(NavigationService ns)
        {
            _ns = ns;
            RegisterCommand = new RelayCommand(Register); 
            LoginCommand = new RelayCommand(Login);
        }

        private void Register() => _ns.Navigate(new RegisterPage(_ns));
        private void Login() => _ns.Navigate(new LoginPage(_ns));
    }
}
