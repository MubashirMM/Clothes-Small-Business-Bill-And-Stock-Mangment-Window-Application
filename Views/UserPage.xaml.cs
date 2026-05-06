using System.Windows.Controls;
using WpfApp2.ViewModel;

namespace WpfApp2.Views
{
    public partial class UserPage : Page
    {
        private UserViewModel _viewModel;

        public UserPage(NavigationService ns, int userId)
        {
            InitializeComponent();
            _viewModel = new UserViewModel(ns, userId);
            DataContext = _viewModel;
        }
    }
}