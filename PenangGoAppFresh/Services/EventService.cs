using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class EventService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public EventService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<Event>> GetAllAsync()
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

        public async Task<Event?> GetByIdAsync(int id)
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

        public async Task AddAsync(Event ev)
        {
            var json = JsonSerializer.Serialize(ev, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/events", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/events/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(Event ev)
        {
            var json = JsonSerializer.Serialize(ev, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/events/{ev.Id}", content);
            response.EnsureSuccessStatusCode();
        }
    }
}
