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
        private string _customerName;
        public string CustomerName
        {
            get => _customerName;
            set
            {
                _customerName = value;
                OnPropertyChanged();
            }
        }
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
            LoadCustomerName();
        }
        private async void LoadCustomerName()
        {
            try
            {
                using (var db = new DatabaseContext())
                {
                    var user = await db.GetUserById(_currentUserId);
                    if (user != null)
                    {
                        CustomerName = user.UserName;
                    }
                    else
                    {
                        CustomerName = "Customer";
                    }
                }
            }
            catch
            {
                CustomerName = "Customer";
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
                string fileName = $"Bill_{order.OrderId}_{order.OrderDate:yyyyMMdd_HHmmss}.pdf";
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
                            .Column(header =>
                            {
                                header.Spacing(5);

                                header.Item().Text("BILL")
                                    .SemiBold().FontSize(24).FontColor(QuestPDF.Helpers.Colors.Green.Darken2)
                                    .AlignCenter();

                                header.Item().Text("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
                                    .AlignCenter();
                            });

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(8);

                                // Customer Information
                                column.Item().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(10).Column(info =>
                                {
                                    info.Spacing(5);
                                    info.Item().Text("CUSTOMER INFORMATION").Bold().FontSize(12);
                                    info.Item().Text($"Customer Name: {CustomerName}");
                                    info.Item().Text($"Order Date: {order.OrderDate:MMM dd, yyyy hh:mm tt}");
                                    info.Item().Text($"Bill ID: #{order.OrderId}");
                                });

                                // Bill Items Table
                                column.Item().Text("BILL ITEMS").Bold().FontSize(12);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(40);
                                        columns.RelativeColumn(2);
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(100);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Product").Bold();
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Qty").Bold().AlignCenter();
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Unit Price").Bold().AlignRight();
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Total").Bold().AlignRight();
                                    });

                                    int index = 1;
                                    foreach (var item in order.OrderItems)
                                    {
                                        table.Cell().Padding(5).Text(index.ToString());
                                        table.Cell().Padding(5).Text(item.ProductName);
                                        table.Cell().Padding(5).Text(item.Quantity.ToString()).AlignCenter();
                                        table.Cell().Padding(5).Text($"${item.UnitPrice:F2}").AlignRight();
                                        table.Cell().Padding(5).Text($"${item.TotalPrice:F2}").AlignRight();
                                        index++;
                                    }
                                });

                                // Total Section
                                column.Item().AlignRight().Column(total =>
                                {
                                    total.Spacing(3);
                                    total.Item().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Width(200);
                                    total.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("Subtotal:").Bold();
                                        row.RelativeItem().Text($"${order.TotalAmount:F2}").AlignRight();
                                    });
                                    total.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("Tax (0%):").Bold();
                                        row.RelativeItem().Text("$0.00").AlignRight();
                                    });
                                    total.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("GRAND TOTAL:").Bold().FontSize(14);
                                        row.RelativeItem().Text($"${order.TotalAmount:F2}").Bold().FontSize(14).AlignRight();
                                    });
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Column(footer =>
                            {
                                footer.Spacing(3);
                                footer.Item().Text("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                                footer.Item().Text("Thank you for shopping with us!");
                                footer.Item().Text($"Generated on: {DateTime.Now:MMM dd, yyyy hh:mm tt}");
                                footer.Item().Text("FASHION HUB - Your Style, Our Passion");
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
                string fileName = $"Bill_{order.OrderId}_{order.OrderDate:yyyyMMdd_HHmmss}.pdf";
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
                            .Column(header =>
                            {
                                header.Spacing(5);
                                header.Item().Text("BILL")
                                    .SemiBold().FontSize(24).FontColor(QuestPDF.Helpers.Colors.Green.Darken2)
                                    .AlignCenter();
                                header.Item().Text("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
                                    .AlignCenter();
                            });

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Spacing(8);

                                column.Item().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(10).Column(info =>
                                {
                                    info.Spacing(5);
                                    info.Item().Text("CUSTOMER INFORMATION").Bold().FontSize(12);
                                    info.Item().Text($"Customer Name: {CustomerName}");
                                    info.Item().Text($"Order Date: {order.OrderDate:MMM dd, yyyy hh:mm tt}");
                                    info.Item().Text($"Bill ID: #{order.OrderId}");
                                });

                                column.Item().Text("BILL ITEMS").Bold().FontSize(12);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(40);
                                        columns.RelativeColumn(2);
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(100);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Product").Bold();
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Qty").Bold().AlignCenter();
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Unit Price").Bold().AlignRight();
                                        header.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(5).Text("Total").Bold().AlignRight();
                                    });

                                    int index = 1;
                                    foreach (var item in order.OrderItems)
                                    {
                                        table.Cell().Padding(5).Text(index.ToString());
                                        table.Cell().Padding(5).Text(item.ProductName);
                                        table.Cell().Padding(5).Text(item.Quantity.ToString()).AlignCenter();
                                        table.Cell().Padding(5).Text($"${item.UnitPrice:F2}").AlignRight();
                                        table.Cell().Padding(5).Text($"${item.TotalPrice:F2}").AlignRight();
                                        index++;
                                    }
                                });

                                column.Item().AlignRight().Column(total =>
                                {
                                    total.Spacing(3);
                                    total.Item().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Width(200);
                                    total.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("Subtotal:").Bold();
                                        row.RelativeItem().Text($"${order.TotalAmount:F2}").AlignRight();
                                    });
                                    total.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("GRAND TOTAL:").Bold().FontSize(14);
                                        row.RelativeItem().Text($"${order.TotalAmount:F2}").Bold().FontSize(14).AlignRight();
                                    });
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Column(footer =>
                            {
                                footer.Spacing(3);
                                footer.Item().Text("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                                footer.Item().Text("Thank you for shopping with us!");
                                footer.Item().Text($"Generated on: {DateTime.Now:MMM dd, yyyy hh:mm tt}");
                                footer.Item().Text("FASHION HUB - Your Style, Our Passion");
                            });
                    });
                }).GeneratePdf(filePath);

                Process.Start(filePath);
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