using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WpfApp2.Model;

namespace WpfApp2.DAL
{
    internal class DatabaseContext : DbContext
    {
        public DatabaseContext() : base("myConnection") { }

        public DbSet<Users> Users { get; set; }
        public DbSet<ClothingProduct> ClothingProducts { get; set; }

        // User methods
        public async Task<bool> IsEmailExists(string email)
        {
            return await Users.AnyAsync(u => u.UserEmail == email);
        }

        public async Task<Users> GetUserByCredentials(string email, string password)
        {
            return await Users.FirstOrDefaultAsync(u => u.UserEmail == email && u.UserPassword == password);
        }

        public async Task<bool> CreateUser(Users user)
        {
            try
            {
                Users.Add(user);
                await SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Users> GetUserByEmail(string email)
        {
            return await Users.FirstOrDefaultAsync(u => u.UserEmail == email);
        }

        public async Task UpdateUserTokens(Users user, string accessToken, string refreshToken, DateTime accessExpiry, DateTime refreshExpiry)
        {
            user.AccessToken = accessToken;
            user.RefreshToken = refreshToken;
            user.AccessTokenExpiryTime = accessExpiry;
            user.RefreshTokenExpiryTime = refreshExpiry;
            await SaveChangesAsync();
        }

        public async Task<Users> GetUserByRefreshToken(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return null;

            return await Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken &&
                                                        u.RefreshTokenExpiryTime > DateTime.Now);
        }

        public async Task ClearUserTokens(Users user)
        {
            user.AccessToken = null;
            user.RefreshToken = null;
            user.AccessTokenExpiryTime = null;
            user.RefreshTokenExpiryTime = null;
            await SaveChangesAsync();
        }

        // Product CRUD methods
        public async Task<bool> CreateProduct(ClothingProduct product)
        {
            try
            {
                ClothingProducts.Add(product);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating product: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateProduct(ClothingProduct product)
        {
            try
            {
                product.UpdatedDate = DateTime.Now;
                Entry(product).State = EntityState.Modified;
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating product: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProduct(int productId)
        {
            try
            {
                var product = await ClothingProducts.FindAsync(productId);
                if (product != null)
                {
                    ClothingProducts.Remove(product);
                    await SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting product: {ex.Message}");
                return false;
            }
        }

        public async Task<ClothingProduct> GetProductById(int productId)
        {
            return await ClothingProducts.FindAsync(productId);
        }

        public async Task<List<ClothingProduct>> GetAllProducts()
        {
            return await ClothingProducts.OrderByDescending(p => p.CreatedDate).ToListAsync();
        }

        public async Task<List<ClothingProduct>> SearchProductsByName(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllProducts();

            return await ClothingProducts
                .Where(p => p.Name.Contains(searchTerm))
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Order methods
        public async Task<bool> SaveOrder(Order order)
        {
            try
            {
                Orders.Add(order);
                await SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving order: {ex.Message}");
                return false;
            }
        }
        public async Task<Users> GetUserById(int userId)
        {
            try
            {
                return await Users.FirstOrDefaultAsync(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetUserById error: {ex.Message}");
                return null;
            }
        }
        public async Task<List<Order>> GetUserOrders(int userId)
        {
            return await Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrders()
        {
            return await Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

    }
}