using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using PenangGoApp.DataAccess;
using System;

namespace PenangGoAppFresh.Services
{
    public class HeritageSiteService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private static string BaseUrl
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    return DeviceInfo.DeviceType == DeviceType.Physical
                        ? "http://192.168.0.20:7000" // Physical Android device
                        : "http://10.0.2.2:7000"; // Android emulator
                }
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    return "http://localhost:7000"; // iOS simulator
                }
                else if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    return "http://localhost:7000"; // Windows
                }
                else if (DeviceInfo.Platform == DevicePlatform.macOS)
                {
                    return "http://localhost:7000"; // macOS
                }
                else
                {
                    return "http://localhost:7000"; // Default fallback
                }
            }
        }

        // DTO for deserialization
        private class HeritageSiteDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string? Description { get; set; }
            public string? Location { get; set; }
            public bool IsActive { get; set; }
            public string? Category { get; set; }
            public string? HistoricalPeriod { get; set; }
            public string? ImageUrl { get; set; }
            public double AverageRating { get; set; }
            public int TotalReviews { get; set; }
            public bool HasPanoramaContent { get; set; }
            public ApprovalStatus ApprovalStatus { get; set; }
            public DateTime? SubmittedAt { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public string? ApprovalNotes { get; set; }
        }

        private HeritageSite MapDtoToModel(HeritageSiteDto dto)
        {
            return new HeritageSite
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                Location = dto.Location ?? string.Empty,
                IsActive = dto.IsActive,
                Category = dto.Category,
                HistoricalPeriod = dto.HistoricalPeriod,
                ImageUrl = dto.ImageUrl,
                AverageRating = dto.AverageRating,
                TotalReviews = dto.TotalReviews,
                HasPanoramaContent = dto.HasPanoramaContent,
                ApprovalStatus = (PenangGoApp.DataAccess.ApprovalStatus)dto.ApprovalStatus,
                SubmittedAt = dto.SubmittedAt,
                ApprovedAt = dto.ApprovedAt,
                ApprovalNotes = dto.ApprovalNotes
            };
        }

        public async Task<List<HeritageSite>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/heritagesites");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var dtos = JsonSerializer.Deserialize<List<HeritageSiteDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = new List<HeritageSite>();
            if (dtos != null)
            {
                foreach (var dto in dtos)
                {
                    result.Add(MapDtoToModel(dto));
                }
            }
            return result;
        }

        public async Task<HeritageSite> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/heritagesites/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<HeritageSiteDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return dto != null ? MapDtoToModel(dto) : null;
        }

        public async Task AddAsync(HeritageSite site)
        {
            var json = JsonSerializer.Serialize(site);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/api/heritagesites", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/api/heritagesites/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateAsync(HeritageSite site)
        {
            var json = JsonSerializer.Serialize(site);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/api/heritagesites/{site.Id}", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task ApproveSiteAsync(int siteId)
        {
            var response = await _httpClient.PutAsync($"{BaseUrl}/api/heritagesites/{siteId}/approve", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task RejectSiteAsync(int siteId, string reason)
        {
            var request = new { Reason = reason };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/api/heritagesites/{siteId}/reject", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task SubmitForApprovalAsync(HeritageSite site)
        {
            // Set submission details
            site.ApprovalStatus = (PenangGoApp.DataAccess.ApprovalStatus)ApprovalStatus.Pending;
            site.SubmittedAt = DateTime.UtcNow;
            site.IsActive = false; // Start as inactive until approved
            var json = JsonSerializer.Serialize(site);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/api/heritagesites", content);
            response.EnsureSuccessStatusCode();
        }
    }
} 