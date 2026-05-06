using System.Windows;
using System.Windows.Controls;
using WpfApp2.Model;
using WpfApp2.ViewModel;

namespace WpfApp2.Views
{
    public partial class AdminPage : Page
    {
        private AdminViewModel _viewModel;

        public AdminPage(NavigationService ns)
        {
            InitializeComponent();
            _viewModel = new AdminViewModel(ns);
            DataContext = _viewModel;
        }

        // Edit button click handler
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.Tag as ClothingProduct;

            if (product != null)
            {
                _viewModel.SelectedProduct = product;
                _viewModel.IsEditMode = true;
                _viewModel.LoadProductToForm(product);
            }  
        }

        // Delete button click handler
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.Tag as ClothingProduct;

            if (product != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete '{product.Name}'?",
                                            "Confirm Delete",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.SelectedProduct = product;
                    _viewModel.DeleteProductCommand.Execute(null);
                }
            }
        }
    }
}