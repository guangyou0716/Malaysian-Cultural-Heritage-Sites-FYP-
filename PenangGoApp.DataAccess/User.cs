using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PenangGoApp.DataAccess
{
    [Table("users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string PasswordSalt { get; set; } = string.Empty;

        public bool IsAdmin { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? ProfilePicture { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public bool EmailNotificationsEnabled { get; set; } = true;

        public bool PushNotificationsEnabled { get; set; } = true;

        public string? DeviceToken { get; set; }

        // Additional properties for enhanced functionality
        public string? PreferredLanguage { get; set; } = "en"; // Language preference
        
        public string? TimeZone { get; set; } = "Asia/Kuala_Lumpur"; // User's timezone
        
        public DateTime? LastSyncAt { get; set; } // Last offline sync timestamp
        
        public string? OfflineData { get; set; } // JSON data for offline mode
        
        public bool IsOnline { get; set; } = true; // Online/offline status
        
        public string? SessionToken { get; set; } // Current session token
        
        public DateTime? SessionExpiresAt { get; set; } // Session expiration
        
        public int LoginAttempts { get; set; } = 0; // Failed login attempts
        
        public DateTime? LockedUntil { get; set; } // Account lockout until
        
        public string? ResetPasswordToken { get; set; } // Password reset token
        
        public DateTime? ResetPasswordExpiresAt { get; set; } // Reset token expiration
        
        public string? EmailVerificationToken { get; set; } // Email verification token
        
        public bool IsEmailVerified { get; set; } = false; // Email verification status
        
        public DateTime? EmailVerifiedAt { get; set; } // Email verification timestamp

        // Compatibility properties for AuthService
        public bool EmailNotifications 
        { 
            get => EmailNotificationsEnabled; 
            set => EmailNotificationsEnabled = value; 
        }

        public bool PushNotifications 
        { 
            get => PushNotificationsEnabled; 
            set => PushNotificationsEnabled = value; 
        }

        // Navigation properties for related entities
        public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Helper methods
        public bool HasAdminPrivileges() => IsAdmin && IsActive;

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            LoginAttempts = 0; // Reset failed attempts on successful login
        }

        public bool CanAccessARContent() => IsActive;

        public bool CanManageEvents() => IsAdmin && IsActive;

        public bool CanManageHeritageSites() => IsAdmin && IsActive;

        public bool CanViewAnalytics() => IsAdmin && IsActive;
        
        public bool IsAccountLocked() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
        
        public void IncrementLoginAttempts()
        {
            LoginAttempts++;
            if (LoginAttempts >= 5) // Lock account after 5 failed attempts
            {
                LockedUntil = DateTime.UtcNow.AddMinutes(30);
            }
        }
        
        public void ResetPasswordAttempts()
        {
            LoginAttempts = 0;
            LockedUntil = null;
        }
    }
} 