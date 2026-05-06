using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp2.Model
{
    public enum UserRole
    {
        SuperAdminUser = 1,
        Admin = 2,
        User = 3
    }

    public class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string UserEmail { get; set; }

        [Required]
        public string UserPassword { get; set; }

        [Required]
        public UserRole UserRole { get; set; }

        public DateTime UserJoiningDateTime { get; set; }

        // JWT Token fields - using regular strings (can be null in C# 7.3)
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string AccessToken { get; set; }
        public DateTime? AccessTokenExpiryTime { get; set; }

        public Users()
        {
            UserJoiningDateTime = DateTime.Now;
        }
    }
}