using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;
using Microsoft.Maui.Devices;

namespace PenangGoAppFresh.Services;

public interface IUserService
{
    Task<List<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> CreateUserAsync(User user);
    Task<User?> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> ToggleUserRoleAsync(int userId, bool isAdmin);
    Task<bool> ToggleUserStatusAsync(int userId, bool isActive);
}

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Use different URLs for different platforms
    private static string BaseUrl
    {
        get
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                return DeviceInfo.DeviceType == DeviceType.Physical 
                    ? "http://192.168.0.20:7000/api" // Physical Android device
                    : "http://10.0.2.2:7000/api"; // Android emulator
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                return "http://localhost:7000/api"; // iOS simulator
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                return "http://localhost:7000/api"; // Windows
            }
            else if (DeviceInfo.Platform == DevicePlatform.macOS)
            {
                return "http://localhost:7000/api"; // macOS
            }
            else
            {
                return "http://localhost:7000/api"; // Default fallback
            }
        }
    }

    public UserService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            Console.WriteLine($"Fetching users from: {BaseUrl}/users");
            var response = await _httpClient.GetAsync($"{BaseUrl}/users");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<User>>(content, _jsonOptions);
                Console.WriteLine($"Successfully fetched {users?.Count ?? 0} users");
                return users ?? new List<User>();
            }
            else
            {
                Console.WriteLine($"Failed to fetch users. Status: {response.StatusCode}");
                return new List<User>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting users: {ex.Message}");
            return new List<User>();
        }
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            Console.WriteLine($"Fetching user {id} from: {BaseUrl}/users/{id}");
            var response = await _httpClient.GetAsync($"{BaseUrl}/users/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(content, _jsonOptions);
                Console.WriteLine($"Successfully fetched user: {user?.Name}");
                return user;
            }
            else
            {
                Console.WriteLine($"Failed to fetch user {id}. Status: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<User?> CreateUserAsync(User user)
    {
        try
        {
            Console.WriteLine($"Creating user: {user.Name}");
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/users", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createdUser = JsonSerializer.Deserialize<User>(responseContent, _jsonOptions);
                Console.WriteLine($"Successfully created user: {createdUser?.Name}");
                return createdUser;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to create user. Status: {response.StatusCode}, Error: {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating user: {ex.Message}");
            return null;
        }
    }

    public async Task<User?> UpdateUserAsync(User user)
    {
        try
        {
            Console.WriteLine($"Updating user {user.Id}: {user.Name}");
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/users/{user.Id}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var updatedUser = JsonSerializer.Deserialize<User>(responseContent, _jsonOptions);
                Console.WriteLine($"Successfully updated user: {updatedUser?.Name}");
                return updatedUser;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to update user {user.Id}. Status: {response.StatusCode}, Error: {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user {user.Id}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            Console.WriteLine($"Deleting user {id}");
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/users/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully deleted user {id}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to delete user {id}. Status: {response.StatusCode}, Error: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting user {id}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ToggleUserRoleAsync(int userId, bool isAdmin)
    {
        try
        {
            Console.WriteLine($"Toggling role for user {userId} to {(isAdmin ? "Admin" : "User")}");
            
            // First get the current user
            var currentUser = await GetUserByIdAsync(userId);
            if (currentUser == null)
            {
                Console.WriteLine($"User {userId} not found");
                return false;
            }

            // Update the role
            currentUser.IsAdmin = isAdmin;
            var updatedUser = await UpdateUserAsync(currentUser);
            
            if (updatedUser != null)
            {
                Console.WriteLine($"Successfully toggled role for user {userId}");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to toggle role for user {userId}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling role for user {userId}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ToggleUserStatusAsync(int userId, bool isActive)
    {
        try
        {
            Console.WriteLine($"Toggling status for user {userId} to {(isActive ? "Active" : "Inactive")}");
            
            // First get the current user
            var currentUser = await GetUserByIdAsync(userId);
            if (currentUser == null)
            {
                Console.WriteLine($"User {userId} not found");
                return false;
            }

            // Update the status
            currentUser.IsActive = isActive;
            var updatedUser = await UpdateUserAsync(currentUser);
            
            if (updatedUser != null)
            {
                Console.WriteLine($"Successfully toggled status for user {userId}");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to toggle status for user {userId}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling status for user {userId}: {ex.Message}");
            return false;
        }
    }
} 