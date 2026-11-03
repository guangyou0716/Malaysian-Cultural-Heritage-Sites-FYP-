using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services;

public interface IApiService
{
    Task<AuthResponse?> LoginAsync(string email, string password);
    Task<AuthResponse?> SignupAsync(string name, string email, string password);
    Task<bool> LogoutAsync(string? sessionToken, string? refreshToken);
    Task<User?> GetProfileAsync();
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);

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
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; }
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public HttpClient HttpClient => _httpClient;

    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        try
        {
            var request = new { email, password };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/login", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthResponse>(responseContent, _jsonOptions);
        }
        catch { return new AuthResponse { Success = false, Message = "Network error" }; }
    }

    public async Task<AuthResponse?> SignupAsync(string name, string email, string password)
    {
        try
        {
            var request = new { name, email, password };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/signup", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AuthResponse>(responseContent, _jsonOptions);
        }
        catch { return new AuthResponse { Success = false, Message = "Network error" }; }
    }

    public async Task<bool> LogoutAsync(string? sessionToken, string? refreshToken)
    {
        try
        {
            var request = new { sessionToken, refreshToken };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/logout", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<User?> GetProfileAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/users/profile");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(content, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            var request = new { currentPassword, newPassword };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/change-password", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            var request = new { email };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/forgot-password", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            var request = new { token, newPassword };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/reset-password", content);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/users");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<User>>(content, _jsonOptions) ?? new();
        }
        catch { return new List<User>(); }
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/users/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(content, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<User?> CreateUserAsync(User user)
    {
        try
        {
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/users", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(responseContent, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<User?> UpdateUserAsync(User user)
    {
        try
        {
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/users/{user.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(responseContent, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/users/{id}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<HeritageSite>> GetHeritageSitesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/heritagesites");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HeritageSite>>(content, _jsonOptions) ?? new();
        }
        catch { return new List<HeritageSite>(); }
    }

    public async Task<HeritageSite?> GetHeritageSiteByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/heritagesites/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HeritageSite>(content, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<HeritageSite?> CreateHeritageSiteAsync(HeritageSite site)
    {
        try
        {
            var json = JsonSerializer.Serialize(site, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/heritagesites", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HeritageSite>(responseContent, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<HeritageSite?> UpdateHeritageSiteAsync(HeritageSite site)
    {
        try
        {
            var json = JsonSerializer.Serialize(site, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/heritagesites/{site.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HeritageSite>(responseContent, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<bool> DeleteHeritageSiteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/heritagesites/{id}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<Event>> GetEventsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/events");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Event>>(content, _jsonOptions) ?? new();
        }
        catch { return new List<Event>(); }
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/events/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Event>(content, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<Event?> CreateEventAsync(Event eventItem)
    {
        try
        {
            var json = JsonSerializer.Serialize(eventItem, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/events", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Event>(responseContent, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<Event?> UpdateEventAsync(Event eventItem)
    {
        try
        {
            var json = JsonSerializer.Serialize(eventItem, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/events/{eventItem.Id}", content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Event>(responseContent, _jsonOptions);
        }
        catch { return null; }
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/events/{id}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
