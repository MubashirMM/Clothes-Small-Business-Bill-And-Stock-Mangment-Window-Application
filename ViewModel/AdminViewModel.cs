using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp2.DAL;
using WpfApp2.Model;
using WpfApp2.Views;

namespace WpfApp2.ViewModel
{
    public class AdminViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _ns;
        private ObservableCollection<ClothingProduct> _products;
        private ClothingProduct _selectedProduct;
        private string _searchText;
        private string _errorMessage;
        private string _validationError;
        private bool _isLoading;
        private bool _isEditMode;

        // Product properties for form binding
        private string _productName;
        private string _size;
        private string _color;
        private string _material;
        private string _brand;
        private string _gender;
        private string _season;
        private string _price;
        private string _stockQuantity;

        // Default values for dropdowns
        private const string DEFAULT_SIZE = "M";
        private const string DEFAULT_GENDER = "Male";
        private const string DEFAULT_SEASON = "Summer";

        // Commands
        public ICommand SaveProductCommand { get; private set; }
        public ICommand UpdateProductCommand { get; private set; }
        public ICommand DeleteProductCommand { get; private set; }
        public ICommand ClearFormCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand LoadProductsCommand { get; private set; }
        public ICommand BackToHomeCommand { get; private set; }
        public ICommand EditProductCommand { get; private set; }
        public ICommand DeleteProductDirectCommand { get; private set; }

        // Properties
        public ObservableCollection<ClothingProduct> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged();
            }
        }

        public ClothingProduct SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LoadProductToForm(value);
                    IsEditMode = true;
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public string ValidationError
        {
            get => _validationError;
            set
            {
                _validationError = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                ((RelayCommand)SaveProductCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)UpdateProductCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)DeleteProductCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
                ((RelayCommand)SaveProductCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)UpdateProductCommand)?.RaiseCanExecuteChanged();
            }
        }

        // Form properties
        public string ProductName
        {
            get => _productName;
            set
            {
                _productName = value;
                OnPropertyChanged();
                ValidateForm();
                ((RelayCommand)SaveProductCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)UpdateProductCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string Size
        {
            get => _size;
            set
            {
                _size = value;
                OnPropertyChanged();
            }
        }

        public string Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged();
            }
        }

        public string Material
        {
            get => _material;
            set
            {
                _material = value;
                OnPropertyChanged();
            }
        }

        public string Brand
        {
            get => _brand;
            set
            {
                _brand = value;
                OnPropertyChanged();
            }
        }

        public string Gender
        {
            get => _gender;
            set
            {
                _gender = value;
                OnPropertyChanged();
            }
        }

        public string Season
        {
            get => _season;
            set
            {
                _season = value;
                OnPropertyChanged();
            }
        }

        public string Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
                ValidateForm();
                ((RelayCommand)SaveProductCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)UpdateProductCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string StockQuantity
        {
            get => _stockQuantity;
            set
            {
                _stockQuantity = value;
                OnPropertyChanged();
            }
        }

        // Dropdown lists
        public List<string> GenderList { get; set; }
        public List<string> SeasonList { get; set; }
        public List<string> SizeList { get; set; }

        public AdminViewModel(NavigationService ns)
        {
            _ns = ns;
            Products = new ObservableCollection<ClothingProduct>();

            // Initialize dropdowns
            GenderList = new List<string> { "Male", "Female", "Unisex" };
            SeasonList = new List<string> { "Summer", "Winter", "Spring", "Fall", "All Season" };
            SizeList = new List<string> { "XS", "S", "M", "L", "XL", "XXL" };

            // Initialize commands
            SaveProductCommand = new RelayCommand(SaveProduct, CanSaveProduct);
            UpdateProductCommand = new RelayCommand(UpdateProduct, CanUpdateProduct);
            DeleteProductCommand = new RelayCommand(DeleteProduct, CanDeleteProduct);
            ClearFormCommand = new RelayCommand(ClearForm);
            SearchCommand = new RelayCommand(SearchProducts);
            LoadProductsCommand = new RelayCommand(async () => await LoadProducts());
            BackToHomeCommand = new RelayCommand(BackToHome);
            EditProductCommand = new RelayCommand<ClothingProduct>(EditProduct);
            DeleteProductDirectCommand = new RelayCommand<ClothingProduct>(DeleteProductDirect);

            // Reset form to default empty state
            ResetFormToDefault();

            // Load products
            LoadProducts();
        }

        private void ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                ValidationError = "Product Name is required";
            }
            else if (string.IsNullOrWhiteSpace(Price) || !decimal.TryParse(Price, out decimal price) || price <= 0)
            {
                ValidationError = "Please enter a valid price greater than 0";
            }
            else
            {
                ValidationError = string.Empty;
            }
        }

        private bool CanSaveProduct()
        {
            bool hasName = !string.IsNullOrWhiteSpace(ProductName);
            bool hasValidPrice = !string.IsNullOrWhiteSpace(Price) && decimal.TryParse(Price, out decimal priceValue) && priceValue > 0;
            bool notInEditMode = !IsEditMode;
            bool notLoading = !IsLoading;
            bool noValidationError = string.IsNullOrWhiteSpace(ValidationError);

            return hasName && hasValidPrice && notInEditMode && notLoading && noValidationError;
        }

        private bool CanUpdateProduct()
        {
            bool hasName = !string.IsNullOrWhiteSpace(ProductName);
            bool hasValidPrice = !string.IsNullOrWhiteSpace(Price) && decimal.TryParse(Price, out decimal priceValue) && priceValue > 0;
            bool inEditMode = IsEditMode;
            bool hasSelected = SelectedProduct != null;
            bool notLoading = !IsLoading;
            bool noValidationError = string.IsNullOrWhiteSpace(ValidationError);

            return hasName && hasValidPrice && inEditMode && hasSelected && notLoading && noValidationError;
        }

        private bool CanDeleteProduct()
        {
            return SelectedProduct != null && !IsLoading;
        }

        private void ResetFormToDefault()
        {
            ProductName = string.Empty;
            Brand = string.Empty;
            Color = string.Empty;
            Material = string.Empty;
            Price = "0";
            StockQuantity = "0";

            // Set dropdowns to default values
            Size = DEFAULT_SIZE;
            Gender = DEFAULT_GENDER;
            Season = DEFAULT_SEASON;
        }

        private async void SaveProduct()
        {
            // Validate before saving
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                ValidationError = "Product Name is required";
                return;
            }

            if (!decimal.TryParse(Price, out decimal priceValue) || priceValue <= 0)
            {
                ValidationError = "Please enter a valid price greater than 0";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            ValidationError = string.Empty;

            try
            {
                if (!int.TryParse(StockQuantity, out int stockValue))
                {
                    stockValue = 0;
                }

                var product = new ClothingProduct
                {
                    Name = ProductName.Trim(),
                    Size = Size ?? string.Empty,
                    Color = Color ?? string.Empty,
                    Material = Material ?? string.Empty,
                    Brand = Brand ?? string.Empty,
                    Gender = Gender ?? string.Empty,
                    Season = Season ?? string.Empty,
                    Price = priceValue,
                    StockQuantity = stockValue
                };

                using (var db = new DatabaseContext())
                {
                    bool success = await db.CreateProduct(product);

                    if (success)
                    {
                        MessageBox.Show("Product added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetFormToDefault();
                        await LoadProducts();
                    }
                    else
                    {
                        ErrorMessage = "Failed to add product. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void UpdateProduct()
        {
            // Validate before updating
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                ValidationError = "Product Name is required";
                return;
            }

            if (!decimal.TryParse(Price, out decimal priceValue) || priceValue <= 0)
            {
                ValidationError = "Please enter a valid price greater than 0";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            ValidationError = string.Empty;

            try
            {
                if (!int.TryParse(StockQuantity, out int stockValue))
                {
                    stockValue = 0;
                }

                SelectedProduct.Name = ProductName.Trim();
                SelectedProduct.Size = Size ?? string.Empty;
                SelectedProduct.Color = Color ?? string.Empty;
                SelectedProduct.Material = Material ?? string.Empty;
                SelectedProduct.Brand = Brand ?? string.Empty;
                SelectedProduct.Gender = Gender ?? string.Empty;
                SelectedProduct.Season = Season ?? string.Empty;
                SelectedProduct.Price = priceValue;
                SelectedProduct.StockQuantity = stockValue;

                using (var db = new DatabaseContext())
                {
                    bool success = await db.UpdateProduct(SelectedProduct);

                    if (success)
                    {
                        MessageBox.Show("Product updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetFormToDefault();
                        await LoadProducts();
                    }
                    else
                    {
                        ErrorMessage = "Failed to update product. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void DeleteProduct()
        {
            if (SelectedProduct == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete '{SelectedProduct.Name}'?",
                                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                using (var db = new DatabaseContext())
                {
                    bool success = await db.DeleteProduct(SelectedProduct.ProductId);

                    if (success)
                    {
                        MessageBox.Show("Product deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetFormToDefault();
                        await LoadProducts();
                    }
                    else
                    {
                        ErrorMessage = "Failed to delete product. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void EditProduct(ClothingProduct product)
        {
            if (product != null)
            {
                LoadProductToForm(product);
                IsEditMode = true;
                SelectedProduct = product;
                ValidationError = string.Empty;
            }
        }

        private async void DeleteProductDirect(ClothingProduct product)
        {
            if (product == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete '{product.Name}'?",
                                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                using (var db = new DatabaseContext())
                {
                    bool success = await db.DeleteProduct(product.ProductId);

                    if (success)
                    {
                        MessageBox.Show("Product deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        ResetFormToDefault();
                        await LoadProducts();
                    }
                    else
                    {
                        ErrorMessage = "Failed to delete product. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void SearchProducts()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadProducts();
                return;
            }

            IsLoading = true;
            try
            {
                using (var db = new DatabaseContext())
                {
                    var products = await db.SearchProductsByName(SearchText);
                    Products.Clear();
                    foreach (var product in products)
                    {
                        Products.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Search error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadProducts()
        {
            IsLoading = true;
            try
            {
                using (var db = new DatabaseContext())
                {
                    var products = await db.GetAllProducts();
                    Products.Clear();
                    foreach (var product in products)
                    {
                        Products.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Load error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LoadProductToForm(ClothingProduct product)
        {
            ProductName = product.Name;
            Size = string.IsNullOrEmpty(product.Size) ? DEFAULT_SIZE : product.Size;
            Color = product.Color ?? string.Empty;
            Material = product.Material ?? string.Empty;
            Brand = product.Brand ?? string.Empty;
            Gender = string.IsNullOrEmpty(product.Gender) ? DEFAULT_GENDER : product.Gender;
            Season = string.IsNullOrEmpty(product.Season) ? DEFAULT_SEASON : product.Season;
            Price = product.Price.ToString();
            StockQuantity = product.StockQuantity.ToString();
        }

        private void ClearForm()
        {
            ResetFormToDefault();
            SelectedProduct = null;
            IsEditMode = false;
            ErrorMessage = string.Empty;
            ValidationError = string.Empty;
        }

        private void BackToHome()
        {
            _ns.Navigate(new HomePage(_ns));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}