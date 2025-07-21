using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

namespace PenangGoAppFresh.Services
{
    public class PanoramaContentService
    {
        private readonly PenangGoContext _context;
        private readonly HttpClient _httpClient;
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

        public PanoramaContentService(PenangGoContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        public async Task<List<PanoramaContent>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/arcontent");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PanoramaContent>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PanoramaContent>();
        }

        public async Task<List<PanoramaContent>> GetByHeritageSiteIdAsync(int heritageSiteId)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/arcontent/site/{heritageSiteId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PanoramaContent>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PanoramaContent>();
        }

        public async Task AddAsync(string title, string description, string panoramaUrl, int uploadedBy, int heritageSiteId)
        {
            var pano = new PanoramaContent
            {
                Title = title,
                Description = description,
                PanoramaUrl = panoramaUrl,
                UploadedBy = uploadedBy,
                UploadedAt = System.DateTime.Now,
                HeritageSiteId = heritageSiteId
            };
            _context.PanoramaContents.Add(pano);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PenangGoApp.DataAccess.PanoramaContent panorama)
        {
            var json = JsonSerializer.Serialize(panorama);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/api/arcontent/{panorama.Id}", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(int panoramaId)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/api/arcontent/{panoramaId}");
            response.EnsureSuccessStatusCode();
        }
    }
}