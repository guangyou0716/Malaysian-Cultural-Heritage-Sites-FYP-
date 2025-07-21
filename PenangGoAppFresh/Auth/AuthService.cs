using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using PenangGoApp.DataAccess;
using PenangGoAppFresh.Services;

namespace PenangGoAppFresh.Auth
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password);
        Task<AuthResult> SignupAsync(string name, string email, string password, string? phoneNumber = null);
        Task<bool> LogoutAsync();
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> UpdateProfileAsync(int userId, string name, string? phoneNumber, string? bio);
        Task<bool> UpdateNotificationSettingsAsync(int userId, bool emailNotifications, bool pushNotifications);
        Task<bool> UpdateDeviceTokenAsync(int userId, string deviceToken);
        Task<User?> GetCurrentUserAsync();
        Task<bool> IsAdminAsync(int userId);
        Task<bool> IsLoggedInAsync();
        Task<bool> HasPermissionAsync(int userId, string permission);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ActivateUserAsync(int userId);
        Task<bool> PromoteToAdminAsync(int userId);
        Task<bool> DemoteFromAdminAsync(int userId);
        
        // Properties for Blazor pages
        User? CurrentUser { get; }
        bool IsLoggedIn { get; }
        bool IsAdmin { get; }
        event Action? AuthStateChanged;
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
        public string? Token { get; set; }
    }

    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private readonly ILogger<AuthService> _logger;
        private User? _currentUser;

        public AuthService(IApiService apiService, ILogger<AuthService> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // Properties for Blazor pages
        public User? CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null;
        public bool IsAdmin => _currentUser?.IsAdmin == true;
        public event Action? AuthStateChanged;

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _apiService.LoginAsync(email, password);
                
                if (response?.Success == true && response.User != null)
                {
                    // Fetch full user profile after login
                    var fullUser = await _apiService.GetUserByIdAsync(response.User.Id);
                    _currentUser = fullUser ?? response.User;
                    _logger.LogInformation("User {Email} logged in successfully", email);
                    AuthStateChanged?.Invoke();

                    return new AuthResult 
                    { 
                        Success = true, 
                        Message = "Login successful.", 
                        User = _currentUser,
                        Token = response.SessionToken
                    };
                }
                else
                {
                    return new AuthResult 
                    { 
                        Success = false, 
                        Message = response?.Message ?? "Login failed." 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", email);
                return new AuthResult 
                { 
                    Success = false, 
                    Message = "An error occurred during login. Please try again." 
                };
            }
        }

        public async Task<AuthResult> SignupAsync(string name, string email, string password, string? phoneNumber = null)
        {
            try
            {
                var response = await _apiService.SignupAsync(name, email, password);
                
                if (response?.Success == true && response.User != null)
                {
                    // Fetch full user profile after signup
                    var fullUser = await _apiService.GetUserByIdAsync(response.User.Id);
                    _currentUser = fullUser ?? response.User;
                    _logger.LogInformation("New user registered: {Email}", email);
                    AuthStateChanged?.Invoke();

                    return new AuthResult 
                    { 
                        Success = true, 
                        Message = "Registration successful.", 
                        User = _currentUser,
                        Token = response.SessionToken
                    };
                }
                else
                {
                    return new AuthResult 
                    { 
                        Success = false, 
                        Message = response?.Message ?? "Registration failed." 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup for email: {Email}", email);
                return new AuthResult 
                { 
                    Success = false, 
                    Message = "An error occurred during registration. Please try again." 
                };
            }
        }

        public Task<bool> LogoutAsync()
        {
            try
            {
                _currentUser = null;
                _logger.LogInformation("User logged out successfully");
                AuthStateChanged?.Invoke();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return Task.FromResult(false);
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                if (!VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    return false;
                }

                if (!IsPasswordStrong(newPassword))
                {
                    return false;
                }

                CreatePasswordHash(newPassword, out string passwordHash, out string passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    _logger.LogInformation("Password changed for user: {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, string name, string? phoneNumber, string? bio)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                user.Name = name;
                user.PhoneNumber = phoneNumber;
                user.Bio = bio;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    _logger.LogInformation("Profile updated for user: {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateNotificationSettingsAsync(int userId, bool emailNotifications, bool pushNotifications)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                user.EmailNotifications = emailNotifications;
                user.PushNotifications = pushNotifications;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    _logger.LogInformation("Notification settings updated for user: {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateDeviceTokenAsync(int userId, string deviceToken)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                user.DeviceToken = deviceToken;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    _logger.LogInformation("Device token updated for user: {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device token for user: {UserId}", userId);
                return false;
            }
        }

        public Task<User?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        public async Task<bool> IsAdminAsync(int userId)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                return user?.IsAdmin == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status for user: {UserId}", userId);
                return false;
            }
        }

        public Task<bool> IsLoggedInAsync()
        {
            return Task.FromResult(_currentUser != null);
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                // For now, only admins have special permissions
                if (permission == "admin" || permission == "manage_users" || permission == "manage_content")
                {
                    return user.IsAdmin;
                }

                return true; // Regular users have basic permissions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user: {UserId}", permission, userId);
                return false;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                return await _apiService.GetUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new List<User>();
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                user.IsActive = false;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    _logger.LogInformation("User deactivated: {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                user.IsActive = true;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    _logger.LogInformation("User activated: {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> PromoteToAdminAsync(int userId)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                user.IsAdmin = true;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    _logger.LogInformation("User promoted to admin: {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting user to admin: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DemoteFromAdminAsync(int userId)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                user.IsAdmin = false;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    _logger.LogInformation("User demoted from admin: {UserId}", userId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error demoting user from admin: {UserId}", userId);
                return false;
            }
        }

        private void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            var (hash, salt) = PenangGoApp.DataAccess.PasswordUtility.CreatePasswordHash(password);
            passwordHash = hash;
            passwordSalt = salt;
        }

        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            return PenangGoApp.DataAccess.PasswordUtility.VerifyPassword(password, storedHash, storedSalt);
        }

        private bool IsPasswordStrong(string password)
        {
            return PenangGoApp.DataAccess.PasswordUtility.IsPasswordStrong(password);
        }
    }
} 