using GalaSoft.MvvmLight.Command;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp2.DAL;
using WpfApp2.Model;
using WpfApp2.Views;
using QuestPDF.Infrastructure;

namespace WpfApp2.ViewModel
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _ns;
        private readonly int _currentUserId;
        private ObservableCollection<ClothingProduct> _products;
        private ObservableCollection<CartItem> _cartItems;
        private ObservableCollection<Order> _orderHistory;
        private string _searchText;
        private string _errorMessage;
        private bool _isLoading;
        private decimal _totalBill;
        private int _selectedTabIndex;
        private ClothingProduct _selectedProduct;

        public ICommand AddToCartCommand { get; private set; }
        public ICommand IncreaseQuantityCommand { get; private set; }
        public ICommand DecreaseQuantityCommand { get; private set; }
        public ICommand RemoveFromCartCommand { get; private set; }
        public ICommand GenerateBillCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand LoadProductsCommand { get; private set; }
        public ICommand ViewOrderDetailsCommand { get; private set; }
        public ICommand ViewPDFCommand { get; private set; }
        public ICommand BackToHomeCommand { get; private set; }

        public ObservableCollection<ClothingProduct> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set
            {
                _cartItems = value;
                OnPropertyChanged();
                CalculateTotalBill();
            }
        }

        public ObservableCollection<Order> OrderHistory
        {
            get => _orderHistory;
            set
            {
                _orderHistory = value;
                OnPropertyChanged();
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

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalBill
        {
            get => _totalBill;
            set
            {
                _totalBill = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
                if (value == 1) // History tab selected
                {
                    LoadOrderHistory();
                }
            }
        }

        public ClothingProduct SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
            }
        }

        public UserViewModel(NavigationService ns, int userId)
        {
            _ns = ns;
            _currentUserId = userId;
            Products = new ObservableCollection<ClothingProduct>();
            CartItems = new ObservableCollection<CartItem>();
            OrderHistory = new ObservableCollection<Order>();

            AddToCartCommand = new RelayCommand<ClothingProduct>(AddToCart);
            IncreaseQuantityCommand = new RelayCommand<CartItem>(IncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<CartItem>(DecreaseQuantity);
            RemoveFromCartCommand = new RelayCommand<CartItem>(RemoveFromCart);
            GenerateBillCommand = new RelayCommand(GenerateBill);
            SearchCommand = new RelayCommand(SearchProducts);
            LoadProductsCommand = new RelayCommand(async () => await LoadProducts());
            ViewOrderDetailsCommand = new RelayCommand<Order>(ViewOrderDetails);
            ViewPDFCommand = new RelayCommand<Order>(ViewPDF);
            BackToHomeCommand = new RelayCommand(BackToHome);

            LoadProducts();
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

        private void AddToCart(ClothingProduct product)
        {
            if (product == null) return;

            var existingItem = CartItems.FirstOrDefault(x => x.ProductId == product.ProductId);
            if (existingItem != null)
            {
                if (existingItem.Quantity < product.StockQuantity)
                {
                    existingItem.Quantity++;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
                    RefreshCart();
                    CalculateTotalBill();
                    MessageBox.Show($"Increased {product.Name} quantity!", "Cart Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Only {product.StockQuantity} items available in stock!", "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                if (product.StockQuantity > 0)
                {
                    CartItems.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = 1,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price,
                        StockQuantity = product.StockQuantity
                    });
                    CalculateTotalBill();
                    MessageBox.Show($"{product.Name} added to cart!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Product out of stock!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void IncreaseQuantity(CartItem item)
        {
            if (item.Quantity < item.StockQuantity)
            {
                item.Quantity++;
                item.TotalPrice = item.Quantity * item.UnitPrice;
                RefreshCart();
                CalculateTotalBill();
            }
            else
            {
                MessageBox.Show($"Only {item.StockQuantity} items available in stock!", "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DecreaseQuantity(CartItem item)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
                item.TotalPrice = item.Quantity * item.UnitPrice;
                RefreshCart();
                CalculateTotalBill();
            }
        }

        private void RemoveFromCart(CartItem item)
        {
            var result = MessageBox.Show($"Remove '{item.ProductName}' from cart?", "Confirm",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                CartItems.Remove(item);
                CalculateTotalBill();
                MessageBox.Show($"{item.ProductName} removed from cart!", "Removed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RefreshCart()
        {
            var temp = CartItems.ToList();
            CartItems.Clear();
            foreach (var item in temp)
            {
                CartItems.Add(item);
            }
        }

        private void CalculateTotalBill()
        {
            TotalBill = CartItems.Sum(x => x.TotalPrice);
        }

        private async void GenerateBill()
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Cart is empty! Please add items to cart.", "Empty Cart", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Generate bill for ${TotalBill:F2}?\n\nProceed to checkout?", "Confirm Order",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;

            try
            {
                var order = new Order
                {
                    UserId = _currentUserId,
                    TotalAmount = TotalBill,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Completed",
                    PaymentMethod = "Cash",
                    ShippingAddress = "Store Pickup"
                };

                foreach (var item in CartItems)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    });
                }

                using (var db = new DatabaseContext())
                {
                    bool success = await db.SaveOrder(order);
                    if (success)
                    {
                        GenerateAndOpenPDF(order);

                        MessageBox.Show($"Order placed successfully!\n\nOrder ID: {order.OrderId}\nTotal: ${TotalBill:F2}\nPDF saved to Desktop",
                                      "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        CartItems.Clear();
                        CalculateTotalBill();
                        await LoadProducts();
                        LoadOrderHistory();
                        SelectedTabIndex = 2; // Switch to history tab
                    }
                    else
                    {
                        ErrorMessage = "Failed to place order. Please try again.";
                        MessageBox.Show("Failed to place order. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GenerateAndOpenPDF(Order order)
        {
            try
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"Invoice_{order.OrderId}_{order.OrderDate:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(desktopPath, fileName);

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(QuestPDF.Helpers.Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .Text("INVOICE")
                            .SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Medium)
                            .AlignCenter();

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(10);

                                column.Item().Text($"Order ID: {order.OrderId}").Bold();
                                column.Item().Text($"Order Date: {order.OrderDate:MMM dd, yyyy hh:mm tt}");
                                column.Item().Text($"Total Amount: ${order.TotalAmount:F2}");

                                column.Item().Text("Items:").Bold();

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(50);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Text("#");
                                        header.Cell().Text("Product");
                                        header.Cell().Text("Qty");
                                        header.Cell().Text("Price");
                                        header.Cell().Text("Total");
                                    });

                                    int index = 1;
                                    foreach (var item in order.OrderItems)
                                    {
                                        table.Cell().Text(index.ToString());
                                        table.Cell().Text(item.ProductName);
                                        table.Cell().Text(item.Quantity.ToString());
                                        table.Cell().Text($"${item.UnitPrice:F2}");
                                        table.Cell().Text($"${item.TotalPrice:F2}");
                                        index++;
                                    }
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Thank you for your purchase!");
                                x.Span(" - ");
                                x.Span("Generated by WPF App");
                            });
                    });
                }).GeneratePdf(filePath);

                // Open PDF in default browser
                Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF generation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewPDF(Order order)
        {
            if (order == null) return;

            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var files = Directory.GetFiles(desktopPath, $"Invoice_{order.OrderId}_*.pdf");

                if (files.Length > 0)
                {
                    Process.Start(files[0]);
                }
                else
                {
                    // Regenerate PDF if not found
                    RegeneratePDF(order);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegeneratePDF(Order order)
        {
            try
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"Invoice_{order.OrderId}_{order.OrderDate:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(desktopPath, fileName);

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(QuestPDF.Helpers.Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Header()
                            .Text("INVOICE")
                            .SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Medium)
                            .AlignCenter();

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(10);

                                column.Item().Text($"Order ID: {order.OrderId}").Bold();
                                column.Item().Text($"Order Date: {order.OrderDate:MMM dd, yyyy hh:mm tt}");
                                column.Item().Text($"Total Amount: ${order.TotalAmount:F2}");

                                column.Item().Text("Items:").Bold();

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(50);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Text("#");
                                        header.Cell().Text("Product");
                                        header.Cell().Text("Qty");
                                        header.Cell().Text("Price");
                                        header.Cell().Text("Total");
                                    });

                                    int index = 1;
                                    foreach (var item in order.OrderItems)
                                    {
                                        table.Cell().Text(index.ToString());
                                        table.Cell().Text(item.ProductName);
                                        table.Cell().Text(item.Quantity.ToString());
                                        table.Cell().Text($"${item.UnitPrice:F2}");
                                        table.Cell().Text($"${item.TotalPrice:F2}");
                                        index++;
                                    }
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Thank you for your purchase!");
                                x.Span(" - ");
                                x.Span("Generated by WPF App");
                            });
                    });
                }).GeneratePdf(filePath);

                Process.Start(filePath);
                MessageBox.Show($"PDF regenerated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF regeneration error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadOrderHistory()
        {
            IsLoading = true;
            try
            {
                using (var db = new DatabaseContext())
                {
                    var orders = await db.GetUserOrders(_currentUserId);
                    OrderHistory.Clear();
                    foreach (var order in orders)
                    {
                        OrderHistory.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Load history error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ViewOrderDetails(Order order)
        {
            if (order == null) return;

            string details = $"═══════════════════════════════════\n";
            details += $"          ORDER DETAILS\n";
            details += $"═══════════════════════════════════\n\n";
            details += $"Order ID: {order.OrderId}\n";
            details += $"Date: {order.OrderDate:MMM dd, yyyy hh:mm tt}\n";
            details += $"Status: {order.OrderStatus}\n";
            details += $"Payment: {order.PaymentMethod}\n";
            details += $"Total: ${order.TotalAmount:F2}\n\n";
            details += $"Items:\n";
            details += $"───────────────────────────────────\n";

            foreach (var item in order.OrderItems)
            {
                details += $"{item.ProductName}\n";
                details += $"  Quantity: {item.Quantity} x ${item.UnitPrice:F2} = ${item.TotalPrice:F2}\n";
            }

            details += $"───────────────────────────────────\n";
            details += $"Grand Total: ${order.TotalAmount:F2}\n";
            details += $"═══════════════════════════════════\n";

            MessageBox.Show(details, "Order Details", MessageBoxButton.OK, MessageBoxImage.Information);
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

    public class CartItem : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }

        public decimal UnitPrice { get; set; }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set
            {
                _totalPrice = value;
                OnPropertyChanged();
            }
        }

        public int StockQuantity { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}