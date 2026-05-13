using GalaSoft.MvvmLight.Command;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp2.DAL;
using WpfApp2.Helpers;
using WpfApp2.Model;
using WpfApp2.Services;
using WpfApp2.Views;

namespace WpfApp2.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _ns;
        private readonly JwtService _jwtService;
        private string _userEmail;
        private string _userPassword;
        private string _errorMessage;
        private bool _isLoading;

        public ICommand LoginCommand { get; }
        public ICommand BackToHomeCommand { get; }

        public string UserEmail
        {
            get => _userEmail;
            set
            {
                _userEmail = value;
                OnPropertyChanged();
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                ClearError();
            }
        }

        public string UserPassword
        {
            get => _userPassword;
            set
            {
                _userPassword = value;
                OnPropertyChanged();
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                ClearError();
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
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public LoginViewModel(NavigationService ns)
        {
            _ns = ns;
            _jwtService = new JwtService();
            LoginCommand = new RelayCommand(Login, CanLogin);
            BackToHomeCommand = new RelayCommand(BackToHome);
        }

        private bool CanLogin()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(UserEmail) &&
                   !string.IsNullOrWhiteSpace(UserPassword);
        }

        private async void Login()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                using (var db = new DatabaseContext())
                {
                    // First check if email exists
                    bool emailExists = await db.IsEmailExists(UserEmail);

                    if (!emailExists)
                    {
                        ErrorMessage = "Invalid email";
                        IsLoading = false;
                        return;
                    }

                    // If email exists, check credentials
                    var user = await db.GetUserByCredentials(UserEmail, UserPassword);

                    if (user != null)
                    {
                        // Generate JWT tokens
                        var accessToken = _jwtService.GenerateAccessToken(user);
                        var refreshToken = _jwtService.GenerateRefreshToken();

                        var accessExpiry = DateTime.Now.AddMinutes(15);
                        var refreshExpiry = DateTime.Now.AddDays(7);

                        // Save tokens to database
                        await db.UpdateUserTokens(user, accessToken, refreshToken, accessExpiry, refreshExpiry);

                        // Save tokens locally using TokenStorage
                        var tokenData = new TokenStorage.TokenData
                        {
                            AccessToken = accessToken,
                            RefreshToken = refreshToken,
                            UserId = user.UserId,
                            UserRole = user.UserRole.ToString()
                        };
                        TokenStorage.SaveTokens(tokenData);

                        MessageBox.Show($"Welcome {user.UserName}!\n\nAccess Token expires in 15 minutes\nRefresh Token expires in 7 days",
                                      "Login Success (JWT)", MessageBoxButton.OK, MessageBoxImage.Information);

                        switch (user.UserRole)
                        {
                            case UserRole.SuperAdminUser:
                                _ns.Navigate(new SuperAdminPage(_ns));
                                break;

                            case UserRole.Admin:
                                _ns.Navigate(new AdminPage(_ns));
                                break;

                            case UserRole.User:
                                _ns.Navigate(new UserPage(_ns, user.UserId));
                                break;
                        }
                    }
                    else
                    {
                        ErrorMessage = "Invalid password";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        private void BackToHome()
        {
            _ns.Navigate(new HomePage(_ns));
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}