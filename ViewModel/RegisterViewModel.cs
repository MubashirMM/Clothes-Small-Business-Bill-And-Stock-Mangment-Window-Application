using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp2.DAL;
using WpfApp2.Model;
using WpfApp2.Views;

namespace WpfApp2.ViewModel
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _ns;
        private string _userName;
        private string _userEmail;
        private string _userPassword;
        private string _confirmPassword;
        private UserRole _selectedUserRole;
        private string _errorMessage;
        private bool _isLoading;
        private Dictionary<string, string> _fieldErrors;
        private Timer _validationTimer;

        public ICommand RegisterCommand { get; }
        public ICommand BackToHomeCommand { get; }

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
                DelayedValidation(nameof(UserName), value);
            }
        }

        public string UserEmail
        {
            get => _userEmail;
            set
            {
                _userEmail = value;
                OnPropertyChanged();
                DelayedValidation(nameof(UserEmail), value);
            }
        }

        public string UserPassword
        {
            get => _userPassword;
            set
            {
                _userPassword = value;
                OnPropertyChanged();
                DelayedValidation(nameof(UserPassword), value);
                if (!string.IsNullOrEmpty(_confirmPassword))
                    DelayedValidation(nameof(ConfirmPassword), _confirmPassword);
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
                DelayedValidation(nameof(ConfirmPassword), value);
            }
        }

        public UserRole SelectedUserRole
        {
            get => _selectedUserRole;
            set
            {
                _selectedUserRole = value;
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
                ((RelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public Dictionary<string, string> FieldErrors
        {
            get => _fieldErrors;
            set
            {
                _fieldErrors = value;
                OnPropertyChanged();
            }
        }

        public List<UserRole> UserRoles { get; set; }

        public RegisterViewModel(NavigationService ns)
        {
            _ns = ns;
            FieldErrors = new Dictionary<string, string>();
            UserRoles = Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();
            SelectedUserRole = UserRole.User;

            RegisterCommand = new RelayCommand(Register, CanRegister);
            BackToHomeCommand = new RelayCommand(BackToHome);
        }

        private void DelayedValidation(string fieldName, string value)
        {
            _validationTimer?.Dispose();
            _validationTimer = new Timer(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ValidateField(fieldName, value);
                });
            }, null, 400, Timeout.Infinite);
        }

        private bool CanRegister()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(UserName) &&
                   !string.IsNullOrWhiteSpace(UserEmail) &&
                   !string.IsNullOrWhiteSpace(UserPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   FieldErrors.Count == 0;
        }

        private async void Register()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                using (var db = new DatabaseContext())
                {
                    bool emailExists = await db.IsEmailExists(UserEmail);

                    if (emailExists)
                    {
                        ErrorMessage = "Email already exists. Please use a different email or login.";
                        IsLoading = false;
                        return;
                    }

                    var newUser = new Users
                    {
                        UserName = UserName,
                        UserEmail = UserEmail,
                        UserPassword = UserPassword,
                        UserRole = SelectedUserRole
                    };

                    bool isSuccess = await db.CreateUser(newUser);

                    if (isSuccess)
                    {
                        string roleMessage = SelectedUserRole.ToString();
                        MessageBox.Show($"Registration successful! You have registered as {roleMessage}.\n\nPlease login to continue.",
                                      "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        _ns.Navigate(new LoginPage(_ns));
                    }
                    else
                    {
                        ErrorMessage = "Registration failed. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Database error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ValidateField(string fieldName, string value)
        {
            string error = null;

            switch (fieldName)
            {
                case nameof(UserName):
                    if (string.IsNullOrWhiteSpace(value))
                        error = "Username required";
                    else if (value.Length < 3)
                        error = "Min 3 characters";
                    else if (value.Length > 50)
                        error = "Max 50 characters";
                    break;

                case nameof(UserEmail):
                    if (string.IsNullOrWhiteSpace(value))
                        error = "Email required";
                    else if (!IsValidEmail(value))
                        error = "Invalid email format";
                    break;

                case nameof(UserPassword):
                    if (string.IsNullOrWhiteSpace(value))
                        error = "Password required";
                    else if (value.Length < 8)
                        error = "Min 8 characters";
                    else if (value.Length > 12)
                        error = "Max 12 characters";
                    else if (!IsStrongPassword(value))
                        error = "Need: Aa1@";
                    break;

                case nameof(ConfirmPassword):
                    if (string.IsNullOrWhiteSpace(value))
                        error = "Please confirm password";
                    else if (value != UserPassword)
                        error = "Passwords don't match";
                    break;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (error != null)
                    FieldErrors[fieldName] = error;
                else
                    FieldErrors.Remove(fieldName);

                OnPropertyChanged(nameof(FieldErrors));
                ((RelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            });
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsStrongPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Length <= 12 &&
                   Regex.IsMatch(password, "[A-Z]") &&
                   Regex.IsMatch(password, "[a-z]") &&
                   Regex.IsMatch(password, "[0-9]") &&
                   Regex.IsMatch(password, @"[\W_]"); 
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