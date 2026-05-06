using System.Windows.Controls;
using WpfApp2.ViewModel;

namespace WpfApp2.Views
{
    public partial class SuperAdminPage : Page
    {
        private SuperAdminViewModel _viewModel;

        public SuperAdminPage(NavigationService ns)
        {
            InitializeComponent();
            _viewModel = new SuperAdminViewModel(ns);
            DataContext = _viewModel;
        }
    }
}