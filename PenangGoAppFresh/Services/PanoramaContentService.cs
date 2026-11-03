using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class PanoramaContentService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public PanoramaContentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<PanoramaContent>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/arcontent");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PanoramaContent>>(json, _jsonOptions) ?? new();
            }
            catch { return new List<PanoramaContent>(); }
        }

        public async Task<List<PanoramaContent>> GetByHeritageSiteIdAsync(int heritageSiteId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/arcontent/site/{heritageSiteId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PanoramaContent>>(json, _jsonOptions) ?? new();
            }
            catch { return new List<PanoramaContent>(); }
        }

        public async Task AddAsync(string title, string description, string panoramaUrl, int uploadedBy, int heritageSiteId)
        {
            var pano = new PanoramaContent
            {
                Title = title,
                Description = description,
                PanoramaUrl = panoramaUrl,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                HeritageSiteId = heritageSiteId
            };
            var json = JsonSerializer.Serialize(pano, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("api/arcontent", content);
        }

        public async Task UpdateAsync(PanoramaContent panorama)
        {
            var json = JsonSerializer.Serialize(panorama, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"api/arcontent/{panorama.Id}", content);
        }

        public async Task DeleteAsync(int panoramaId)
        {
            await _httpClient.DeleteAsync($"api/arcontent/{panoramaId}");
        }

        public async Task SubmitForApprovalAsync(string title, string description, string panoramaUrl, int uploadedBy, int heritageSiteId)
        {
            var pano = new PanoramaContent
            {
                Title = title,
                Description = description,
                PanoramaUrl = panoramaUrl,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                HeritageSiteId = heritageSiteId,
                ApprovalStatus = ApprovalStatus.Pending
            };
            var json = JsonSerializer.Serialize(pano, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("api/arcontent", content);
        }
    }
}
