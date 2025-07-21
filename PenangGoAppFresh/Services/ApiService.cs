using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;
using Microsoft.Maui.Devices;

namespace PenangGoAppFresh.Services;

public interface IApiService
{
    // Authentication endpoints
    Task<AuthResponse?> LoginAsync(string email, string password);
    Task<AuthResponse?> SignupAsync(string name, string email, string password);
    Task<bool> LogoutAsync(string sessionToken);
    
    Task<List<User>> GetUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> CreateUserAsync(User user);
    Task<User?> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);
    
    Task<List<HeritageSite>> GetHeritageSitesAsync();
    Task<HeritageSite?> GetHeritageSiteByIdAsync(int id);
    Task<HeritageSite?> CreateHeritageSiteAsync(HeritageSite site);
    Task<HeritageSite?> UpdateHeritageSiteAsync(HeritageSite site);
    Task<bool> DeleteHeritageSiteAsync(int id);
    
    Task<List<Event>> GetEventsAsync();
    Task<Event?> GetEventByIdAsync(int id);
    Task<Event?> CreateEventAsync(Event eventItem);
    Task<Event?> UpdateEventAsync(Event eventItem);
    Task<bool> DeleteEventAsync(int id);
}

public class AuthResponse
{
    public User? User { get; set; }
    public string? SessionToken { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; }
}

public class ApiService : IApiService
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

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // Public property to access HttpClient for direct calls
    public HttpClient HttpClient => _httpClient;

    // Authentication endpoints
    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var request = new { email, password };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Debug: Log the URL being used
            var url = $"{BaseUrl}/auth/login";
            Console.WriteLine($"Attempting login to: {url}");
            Console.WriteLine($"Platform: {DeviceInfo.Platform}, DeviceType: {DeviceInfo.DeviceType}");
            
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthResponse>(responseContent, _jsonOptions);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var error = JsonSerializer.Deserialize<AuthResponse>(errorContent, _jsonOptions);
                return error;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during login: {ex.Message}");
            return new AuthResponse { Success = false, Message = "Network error during login" };
        }
    }

    public async Task<AuthResponse?> SignupAsync(string name, string email, string password)
    {
        try
        {
            var request = new { name, email, password };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/auth/signup", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthResponse>(responseContent, _jsonOptions);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var error = JsonSerializer.Deserialize<AuthResponse>(errorContent, _jsonOptions);
                return error;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during signup: {ex.Message}");
            return new AuthResponse { Success = false, Message = "Network error during signup" };
        }
    }

    public async Task<bool> LogoutAsync(string sessionToken)
    {
        try
        {
            var request = new { sessionToken };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/auth/logout", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during logout: {ex.Message}");
            return false;
        }
    }

    // User endpoints
    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/users");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<User>>(content, _jsonOptions) ?? new List<User>();
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
            var response = await _httpClient.GetAsync($"{BaseUrl}/users/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(content, _jsonOptions);
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
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/users", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(responseContent, _jsonOptions);
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
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/users/{user.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/users/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting user {id}: {ex.Message}");
            return false;
        }
    }

    // Heritage Site endpoints
    public async Task<List<HeritageSite>> GetHeritageSitesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/heritagesites");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HeritageSite>>(content, _jsonOptions) ?? new List<HeritageSite>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting heritage sites: {ex.Message}");
            return new List<HeritageSite>();
        }
    }

    public async Task<HeritageSite?> GetHeritageSiteByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/heritagesites/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HeritageSite>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting heritage site {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<HeritageSite?> CreateHeritageSiteAsync(HeritageSite site)
    {
        try
        {
            var json = JsonSerializer.Serialize(site, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/heritagesites", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HeritageSite>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating heritage site: {ex.Message}");
            return null;
        }
    }

    public async Task<HeritageSite?> UpdateHeritageSiteAsync(HeritageSite site)
    {
        try
        {
            var json = JsonSerializer.Serialize(site, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/heritagesites/{site.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HeritageSite>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating heritage site: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteHeritageSiteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/heritagesites/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting heritage site {id}: {ex.Message}");
            return false;
        }
    }

    // Event endpoints
    public async Task<List<Event>> GetEventsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/events");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Event>>(content, _jsonOptions) ?? new List<Event>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting events: {ex.Message}");
            return new List<Event>();
        }
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/events/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Event>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting event {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<Event?> CreateEventAsync(Event eventItem)
    {
        try
        {
            var json = JsonSerializer.Serialize(eventItem, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/events", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Event>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating event: {ex.Message}");
            return null;
        }
    }

    public async Task<Event?> UpdateEventAsync(Event eventItem)
    {
        try
        {
            var json = JsonSerializer.Serialize(eventItem, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/events/{eventItem.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Event>(responseContent, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating event: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/events/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting event {id}: {ex.Message}");
            return false;
        }
    }
} 