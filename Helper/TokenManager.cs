using System;
using System.Threading.Tasks;
using System.Windows;
using WpfApp2.DAL;
using WpfApp2.Services;

namespace WpfApp2.Helpers
{
    public class TokenManager
    {
        private readonly JwtService _jwtService;

        public TokenManager()
        {
            _jwtService = new JwtService();
        }

        public async Task<bool> RefreshTokenIfNeeded()
        {
            var tokenData = TokenStorage.LoadTokens();

            if (string.IsNullOrEmpty(tokenData.AccessToken))
                return false;

            // Check if token is valid
            if (_jwtService.ValidateToken(tokenData.AccessToken))
                return true;

            // Token expired, try to refresh
            return await RefreshToken();
        }

        private async Task<bool> RefreshToken()
        {
            try
            {
                var tokenData = TokenStorage.LoadTokens();
                var refreshToken = tokenData.RefreshToken;
                var userId = tokenData.UserId;

                if (string.IsNullOrEmpty(refreshToken) || userId == 0)
                    return false;

                using (var db = new DatabaseContext())
                {
                    var user = await db.GetUserByRefreshToken(refreshToken);

                    if (user != null && user.UserId == userId)
                    {
                        var newAccessToken = _jwtService.GenerateAccessToken(user);
                        var newRefreshToken = _jwtService.GenerateRefreshToken();

                        await db.UpdateUserTokens(user, newAccessToken, newRefreshToken,
                            DateTime.Now.AddMinutes(15), DateTime.Now.AddDays(7));

                        // Update stored tokens
                        tokenData.AccessToken = newAccessToken;
                        tokenData.RefreshToken = newRefreshToken;
                        TokenStorage.SaveTokens(tokenData);

                        return true;
                    }
                }

                await Logout();
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Token refresh error: {ex.Message}");
                await Logout();
                return false;
            }
        }

        public async Task Logout()
        {
            var tokenData = TokenStorage.LoadTokens();
            var userId = tokenData.UserId;

            if (userId > 0)
            {
                using (var db = new DatabaseContext())
                {
                    var user = await db.Users.FindAsync(userId);
                    if (user != null)
                    {
                        await db.ClearUserTokens(user);
                    }
                }
            }

            TokenStorage.ClearTokens();
            MessageBox.Show("Logged out successfully.", "Session Ended",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}