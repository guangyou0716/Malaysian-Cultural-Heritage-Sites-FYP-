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
        Task<bool> UpdateProfileAsync(int userId, string name, string? phoneNumber, string? bio, string? accessibilityPreference);
        Task<bool> UpdateNotificationSettingsAsync(int userId, bool emailNotifications, bool pushNotifications);
        Task<bool> UpdateDeviceTokenAsync(int userId, string deviceToken);
        Task<User?> GetCurrentUserAsync();
        Task<bool> RestoreSessionAsync();
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
        private readonly ITokenStore _tokenStore;
        private readonly ILogger<AuthService> _logger;
        private User? _currentUser;

        public AuthService(IApiService apiService, ITokenStore tokenStore, ILogger<AuthService> logger)
        {
            _apiService = apiService;
            _tokenStore = tokenStore;
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
                    // Persist tokens so authenticated requests work and the session survives restarts
                    await _tokenStore.SaveTokensAsync(response.AccessToken, response.RefreshToken, response.SessionToken);

                    // Fetch full user profile after login
                    var fullUser = await _apiService.GetProfileAsync();
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

        public async Task<bool> LogoutAsync()
        {
            try
            {
                // Revoke the session server-side, then clear local credentials
                await _apiService.LogoutAsync(_tokenStore.SessionToken, await _tokenStore.GetRefreshTokenAsync());
                await _tokenStore.ClearAsync();
                _currentUser = null;
                _logger.LogInformation("User logged out successfully");
                AuthStateChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        public async Task<bool> RestoreSessionAsync()
        {
            if (_currentUser != null) return true;

            var refreshToken = await _tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            try
            {
                var user = await _apiService.GetProfileAsync();
                if (user == null)
                    return false;

                _currentUser = user;
                _logger.LogInformation("Session restored for user: {Email}", user.Email);
                AuthStateChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring session");
                await _tokenStore.ClearAsync();
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                // Server-side verification (the client never has the password hash)
                var ok = await _apiService.ChangePasswordAsync(currentPassword, newPassword);
                if (ok)
                    _logger.LogInformation("Password changed for user: {UserId}", userId);
                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, string name, string? phoneNumber, string? bio, string? accessibilityPreference)
        {
            try
            {
                var user = await _apiService.GetUserByIdAsync(userId);
                if (user == null) return false;

                user.Name = name;
                user.PhoneNumber = phoneNumber;
                user.Bio = bio;
                user.AccessibilityPreference = accessibilityPreference;

                var updatedUser = await _apiService.UpdateUserAsync(user);
                if (updatedUser != null)
                {
                    if (_currentUser != null)
                    {
                        _currentUser.Name = name;
                        _currentUser.PhoneNumber = phoneNumber;
                        _currentUser.Bio = bio;
                        _currentUser.AccessibilityPreference = accessibilityPreference;
                    }
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
    }
}