using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace PenangGoAppFresh.Services
{
    public class HeritageSiteService
    {
        private readonly PenangGoContext _context;
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
        public HeritageSiteService(PenangGoContext context)
        {
            _context = context;
        }

        public async Task<List<HeritageSite>> GetAllAsync()
        {
            return await _context.HeritageSites.OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<HeritageSite> GetByIdAsync(int id)
        {
            return await _context.HeritageSites.FindAsync(id);
        }

        public async Task AddAsync(HeritageSite site)
        {
            _context.HeritageSites.Add(site);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var site = await _context.HeritageSites.FindAsync(id);
            if (site != null)
            {
                _context.HeritageSites.Remove(site);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(HeritageSite site)
        {
            var json = JsonSerializer.Serialize(site);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/api/heritagesites/{site.Id}", content);
            response.EnsureSuccessStatusCode();
        }
    }
} 