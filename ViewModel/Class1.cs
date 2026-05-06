using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp2.DAL;
using WpfApp2.Model;
using WpfApp2.Views;

namespace WpfApp2.ViewModel
{
    public class SuperAdminViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _ns;
        private ObservableCollection<UserDto> _users;
        private ObservableCollection<OrderDto> _allOrders;
        private ObservableCollection<ProductSalesDto> _topSellingProducts;
        private ObservableCollection<ClothingProduct> _lowStockProducts;
        private ObservableCollection<DailySalesDto> _dailySales;
        private ObservableCollection<MonthlySalesDto> _monthlySales;
        private ObservableCollection<WeeklySalesDto> _weeklySales;

        private string _searchUserText;
        private string _errorMessage;
        private bool _isLoading;
        private int _selectedTabIndex;
        private UserDto _selectedUser;
        private decimal _totalRevenue;
        private int _totalOrders;
        private int _totalUsers;
        private int _totalProducts;
        private string _selectedPeriod;

        public ICommand LoadUsersCommand { get; private set; }
        public ICommand LoadSalesReportCommand { get; private set; }
        public ICommand LoadTopSellingCommand { get; private set; }
        public ICommand LoadLowStockCommand { get; private set; }
        public ICommand ViewUserOrdersCommand { get; private set; }
        public ICommand NavigateToAdminPageCommand { get; private set; }
        public ICommand BackToHomeCommand { get; private set; }
        public ICommand RefreshAllCommand { get; private set; }

        public ObservableCollection<UserDto> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<OrderDto> AllOrders
        {
            get => _allOrders;
            set
            {
                _allOrders = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ProductSalesDto> TopSellingProducts
        {
            get => _topSellingProducts;
            set
            {
                _topSellingProducts = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ClothingProduct> LowStockProducts
        {
            get => _lowStockProducts;
            set
            {
                _lowStockProducts = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DailySalesDto> DailySales
        {
            get => _dailySales;
            set
            {
                _dailySales = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MonthlySalesDto> MonthlySales
        {
            get => _monthlySales;
            set
            {
                _monthlySales = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<WeeklySalesDto> WeeklySales
        {
            get => _weeklySales;
            set
            {
                _weeklySales = value;
                OnPropertyChanged();
            }
        }

        public string SearchUserText
        {
            get => _searchUserText;
            set
            {
                _searchUserText = value;
                OnPropertyChanged();
                LoadUsers();
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

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
                LoadTabData();
            }
        }

        public UserDto SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LoadUserOrders(value.UserId);
                }
            }
        }

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                _totalRevenue = value;
                OnPropertyChanged();
            }
        }

        public int TotalOrders
        {
            get => _totalOrders;
            set
            {
                _totalOrders = value;
                OnPropertyChanged();
            }
        }

        public int TotalUsers
        {
            get => _totalUsers;
            set
            {
                _totalUsers = value;
                OnPropertyChanged();
            }
        }

        public int TotalProducts
        {
            get => _totalProducts;
            set
            {
                _totalProducts = value;
                OnPropertyChanged();
            }
        }

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                _selectedPeriod = value;
                OnPropertyChanged();
                LoadSalesReport();
            }
        }

        public List<string> PeriodOptions { get; set; }

        public SuperAdminViewModel(NavigationService ns)
        {
            _ns = ns;
            Users = new ObservableCollection<UserDto>();
            AllOrders = new ObservableCollection<OrderDto>();
            TopSellingProducts = new ObservableCollection<ProductSalesDto>();
            LowStockProducts = new ObservableCollection<ClothingProduct>();
            DailySales = new ObservableCollection<DailySalesDto>();
            MonthlySales = new ObservableCollection<MonthlySalesDto>();
            WeeklySales = new ObservableCollection<WeeklySalesDto>();

            PeriodOptions = new List<string> { "Daily", "Weekly", "Monthly" };
            SelectedPeriod = "Daily";

            LoadUsersCommand = new RelayCommand(LoadUsers);
            LoadSalesReportCommand = new RelayCommand(LoadSalesReport);
            LoadTopSellingCommand = new RelayCommand(LoadTopSellingProducts);
            LoadLowStockCommand = new RelayCommand(LoadLowStockProducts);
            ViewUserOrdersCommand = new RelayCommand<int?>(ViewUserOrders);
            NavigateToAdminPageCommand = new RelayCommand(NavigateToAdminPage);
            BackToHomeCommand = new RelayCommand(BackToHome);
            RefreshAllCommand = new RelayCommand(RefreshAll);

            LoadAllData();
        }

        private async Task LoadAllData()
        {
            await Task.WhenAll(
                LoadUsersAsync(),
                LoadAllOrdersAsync(),
                LoadTopSellingProductsAsync(),
                LoadLowStockProductsAsync(),
                LoadSalesReportAsync(),
                LoadStatisticsAsync()
            );
        }

        private async void RefreshAll()
        {
            await LoadAllData();
            MessageBox.Show("All data refreshed successfully!", "Refresh", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void LoadTabData()
        {
            switch (SelectedTabIndex)
            {
                case 0: // Dashboard
                    await LoadStatisticsAsync();
                    await LoadSalesReportAsync();
                    await LoadTopSellingProductsAsync();
                    await LoadLowStockProductsAsync();
                    break;
                case 1: // Users
                    await LoadUsersAsync();
                    break;
                case 2: // Orders
                    await LoadAllOrdersAsync();
                    break;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                using (var db = new DatabaseContext())
                {
                    var allOrders = await db.GetAllOrders();
                    TotalRevenue = allOrders.Sum(o => o.TotalAmount);
                    TotalOrders = allOrders.Count;
                    TotalUsers = await db.Users.CountAsync();
                    TotalProducts = await db.ClothingProducts.CountAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Stats error: {ex.Message}";
            }
        }

        private async void LoadUsers()
        {
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            IsLoading = true;
            try
            {
                using (var db = new DatabaseContext())
                {
                    var users = await db.Users.ToListAsync();
                    var filteredUsers = string.IsNullOrWhiteSpace(SearchUserText)
                        ? users
                        : users.Where(u => u.UserName.Contains(SearchUserText) || u.UserEmail.Contains(SearchUserText)).ToList();

                    Users.Clear();
                    foreach (var user in filteredUsers)
                    {
                        var orderCount = await db.Orders.CountAsync(o => o.UserId == user.UserId);
                        var totalSpent = await db.Orders.Where(o => o.UserId == user.UserId).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                        Users.Add(new UserDto
                        {
                            UserId = user.UserId,
                            UserName = user.UserName,
                            UserEmail = user.UserEmail,
                            UserRole = user.UserRole.ToString(),
                            UserJoiningDateTime = user.UserJoiningDateTime,
                            OrderCount = orderCount,
                            TotalSpent = totalSpent
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Load users error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void LoadAllOrders()
        {
            await LoadAllOrdersAsync();
        }

        private async Task LoadAllOrdersAsync()
        {
            IsLoading = true;
            try
            {
                using (var db = new DatabaseContext())
                {
                    var orders = await db.GetAllOrders();
                    AllOrders.Clear();
                    foreach (var order in orders)
                    {
                        var user = await db.Users.FindAsync(order.UserId);
                        AllOrders.Add(new OrderDto
                        {
                            OrderId = order.OrderId,
                            UserName = user?.UserName ?? "Unknown",
                            OrderDate = order.OrderDate,
                            TotalAmount = order.TotalAmount,
                            Status = order.OrderStatus,
                            ItemCount = order.OrderItems.Count
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Load orders error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void LoadTopSellingProducts()
        {
            await LoadTopSellingProductsAsync();
        }

        private async Task LoadTopSellingProductsAsync()
        {
            try
            {
                using (var db = new DatabaseContext())
                {
                    var topProducts = await db.OrderItems
                        .GroupBy(i => i.ProductId)
                        .Select(g => new ProductSalesDto
                        {
                            ProductId = g.Key,
                            ProductName = g.First().ProductName,
                            TotalQuantitySold = g.Sum(i => i.Quantity),
                            TotalRevenue = g.Sum(i => i.TotalPrice)
                        })
                        .OrderByDescending(p => p.TotalQuantitySold)
                        .Take(10)
                        .ToListAsync();

                    TopSellingProducts.Clear();
                    foreach (var product in topProducts)
                    {
                        TopSellingProducts.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Top selling error: {ex.Message}";
            }
        }

        private async void LoadLowStockProducts()
        {
            await LoadLowStockProductsAsync();
        }

        private async Task LoadLowStockProductsAsync()
        {
            try
            {
                using (var db = new DatabaseContext())
                {
                    var lowStock = await db.ClothingProducts
                        .Where(p => p.StockQuantity < 10)
                        .OrderBy(p => p.StockQuantity)
                        .ToListAsync();

                    LowStockProducts.Clear();
                    foreach (var product in lowStock)
                    {
                        LowStockProducts.Add(product);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Low stock error: {ex.Message}";
            }
        }

        private async void LoadSalesReport()
        {
            await LoadSalesReportAsync();
        }

        private async Task LoadSalesReportAsync()
        {
            IsLoading = true;
            try
            {
                using (var db = new DatabaseContext())
                {
                    var orders = await db.GetAllOrders();
                    var now = DateTime.Now;

                    switch (SelectedPeriod)
                    {
                        case "Daily":
                            var dailyData = orders
                                .Where(o => o.OrderDate.Date == now.Date)
                                .GroupBy(o => o.OrderDate.Hour)
                                .Select(g => new DailySalesDto
                                {
                                    Hour = g.Key,
                                    OrdersCount = g.Count(),
                                    Revenue = g.Sum(o => o.TotalAmount)
                                })
                                .OrderBy(d => d.Hour)
                                .ToList();

                            DailySales.Clear();
                            foreach (var item in dailyData)
                            {
                                DailySales.Add(item);
                            }
                            break;

                        case "Weekly":
                            var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
                            var weeklyData = orders
                                .Where(o => o.OrderDate >= startOfWeek && o.OrderDate <= now)
                                .GroupBy(o => o.OrderDate.Date)
                                .Select(g => new WeeklySalesDto
                                {
                                    Date = g.Key,
                                    OrdersCount = g.Count(),
                                    Revenue = g.Sum(o => o.TotalAmount)
                                })
                                .OrderBy(w => w.Date)
                                .ToList();

                            WeeklySales.Clear();
                            foreach (var item in weeklyData)
                            {
                                WeeklySales.Add(item);
                            }
                            break;

                        case "Monthly":
                            var monthlyData = orders
                                .Where(o => o.OrderDate.Year == now.Year)
                                .GroupBy(o => o.OrderDate.Month)
                                .Select(g => new MonthlySalesDto
                                {
                                    Month = g.Key,
                                    MonthName = new DateTime(now.Year, g.Key, 1).ToString("MMMM"),
                                    OrdersCount = g.Count(),
                                    Revenue = g.Sum(o => o.TotalAmount)
                                })
                                .OrderBy(m => m.Month)
                                .ToList();

                            MonthlySales.Clear();
                            foreach (var item in monthlyData)
                            {
                                MonthlySales.Add(item);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Sales report error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ViewUserOrders(int? userId)
        {
            if (!userId.HasValue) return;
            await LoadUserOrders(userId.Value);
        }

        private async Task LoadUserOrders(int userId)
        {
            IsLoading = true;
            try
            {
                using (var db = new DatabaseContext())
                {
                    var orders = await db.GetUserOrders(userId);
                    var user = await db.Users.FindAsync(userId);

                    string details = $"═══════════════════════════════════\n";
                    details += $"     ORDERS FOR {user?.UserName}\n";
                    details += $"═══════════════════════════════════\n\n";

                    if (orders.Count == 0)
                    {
                        details += "No orders found for this user.\n";
                    }

                    foreach (var order in orders)
                    {
                        details += $"Order ID: {order.OrderId}\n";
                        details += $"Date: {order.OrderDate:MMM dd, yyyy hh:mm tt}\n";
                        details += $"Total: ${order.TotalAmount:F2}\n";
                        details += $"Items: {order.OrderItems.Count}\n";
                        details += $"───────────────────────────────────\n";
                    }

                    details += $"═══════════════════════════════════\n";
                    details += $"Total Orders: {orders.Count}\n";
                    details += $"Total Spent: ${orders.Sum(o => o.TotalAmount):F2}\n";
                    details += $"═══════════════════════════════════\n";

                    MessageBox.Show(details, $"Order History - {user?.UserName}", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Load user orders error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateToAdminPage()
        {
            _ns.Navigate(new AdminPage(_ns));
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

    // DTO Classes
    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserRole { get; set; }
        public DateTime UserJoiningDateTime { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public string UserName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public int ItemCount { get; set; }
    }

    public class ProductSalesDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailySalesDto
    {
        public int Hour { get; set; }
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class WeeklySalesDto
    {
        public DateTime Date { get; set; }
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MonthlySalesDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int OrdersCount { get; set; }
        public decimal Revenue { get; set; }
    }
}