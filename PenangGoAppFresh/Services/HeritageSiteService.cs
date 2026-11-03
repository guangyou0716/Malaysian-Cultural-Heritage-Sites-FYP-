using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class HeritageSiteService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public HeritageSiteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<HeritageSite>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/heritagesites");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HeritageSite>>(json, _jsonOptions) ?? new();
        }

        public async Task<List<HeritageSite>> GetAllForManagementAsync()
        {
            var response = await _httpClient.GetAsync("api/heritagesites/manage");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HeritageSite>>(json, _jsonOptions) ?? new();
        }

        public async Task<HeritageSite?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/heritagesites/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<HeritageSite>(json, _jsonOptions);
        }

        public async Task AddAsync(HeritageSite site)
        {
            var json = JsonSerializer.Serialize(site, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("api/heritagesites", content);
        }

        public async Task DeleteAsync(int id)
        {
            await _httpClient.DeleteAsync($"api/heritagesites/{id}");
        }

        public async Task UpdateAsync(HeritageSite site)
        {
            var json = JsonSerializer.Serialize(site, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"api/heritagesites/{site.Id}", content);
        }

        public async Task ApproveSiteAsync(int siteId)
        {
            await _httpClient.PutAsync($"api/heritagesites/{siteId}/approve", null);
        }

        public async Task RejectSiteAsync(int siteId, string reason)
        {
            var request = new { Reason = reason };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"api/heritagesites/{siteId}/reject", content);
        }

        public async Task SubmitForApprovalAsync(HeritageSite site)
        {
            var json = JsonSerializer.Serialize(new
            {
                name = site.Name,
                description = site.Description,
                location = site.Location,
                latitude = site.Latitude,
                longitude = site.Longitude,
                category = site.Category,
                historicalPeriod = site.HistoricalPeriod,
                culturalSignificance = site.CulturalSignificance,
                visitingHours = site.VisitingHours,
                entryFee = site.EntryFee,
                contactInfo = site.ContactInfo,
                website = site.Website,
                isAccessible = site.IsAccessible,
                imageUrl = site.ImageUrl
            }, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("api/heritagesites/submissions", content);
        }

        public async Task<List<HeritageSite>> GetMySubmissionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/heritagesites/my-submissions");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<HeritageSite>>(json, _jsonOptions) ?? new();
            }
            catch { return new List<HeritageSite>(); }
        }
    }
}
