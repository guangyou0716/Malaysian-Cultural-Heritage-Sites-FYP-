using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PenangGoApp.DataAccess;
using Microsoft.Maui.Devices;

namespace PenangGoAppFresh.Services
{
    public interface IApiBookmarkService
    {
        Task<List<Bookmark>> GetUserBookmarksAsync(int userId);
        Task<bool> AddBookmarkAsync(int userId, int siteId);
        Task<bool> RemoveBookmarkAsync(int userId, int siteId);
    }

    public class ApiBookmarkService : IApiBookmarkService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiBookmarkService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                MaxDepth = 32
            };
        }

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

        public async Task<List<Bookmark>> GetUserBookmarksAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/bookmarks/user/{userId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var bookmarkDtos = JsonSerializer.Deserialize<List<BookmarkDto>>(content, _jsonOptions) ?? new List<BookmarkDto>();
                
                // Convert DTOs to Bookmark objects
                var bookmarks = new List<Bookmark>();
                foreach (var dto in bookmarkDtos)
                {
                    bookmarks.Add(ConvertDtoToBookmark(dto));
                }
                
                return bookmarks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user bookmarks: {ex.Message}");
                return new List<Bookmark>();
            }
        }

        public async Task<bool> AddBookmarkAsync(int userId, int siteId)
        {
            try
            {
                var bookmark = new Bookmark
                {
                    UserId = userId,
                    SiteId = siteId,
                    CreatedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(bookmark, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/bookmarks", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding bookmark: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveBookmarkAsync(int userId, int siteId)
        {
            try
            {
                // First, get the bookmark to find its ID
                var userBookmarks = await GetUserBookmarksAsync(userId);
                var bookmark = userBookmarks.FirstOrDefault(b => b.SiteId == siteId);
                
                if (bookmark == null)
                {
                    return false;
                }

                var response = await _httpClient.DeleteAsync($"{BaseUrl}/bookmarks/{bookmark.Id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing bookmark: {ex.Message}");
                return false;
            }
        }

        private Bookmark ConvertDtoToBookmark(BookmarkDto dto)
        {
            return new Bookmark
            {
                Id = dto.Id,
                UserId = dto.UserId,
                SiteId = dto.SiteId,
                CreatedAt = dto.CreatedAt,
                Site = dto.Site != null ? new PenangGoApp.DataAccess.HeritageSite
                {
                    Id = dto.Site.Id,
                    Name = dto.Site.Name,
                    Description = dto.Site.Description,
                    Location = dto.Site.Location,
                    ImageUrl = dto.Site.ImageUrl,
                    IsActive = dto.Site.IsActive,
                    Category = dto.Site.Category,
                    HistoricalPeriod = dto.Site.HistoricalPeriod,
                    AverageRating = dto.Site.AverageRating,
                    TotalReviews = dto.Site.TotalReviews,
                    HasPanoramaContent = dto.Site.HasPanoramaContent,
                    ApprovalStatus = (PenangGoApp.DataAccess.ApprovalStatus)dto.Site.ApprovalStatus,
                    SubmittedAt = dto.Site.SubmittedAt,
                    ApprovedAt = dto.Site.ApprovedAt,
                    ApprovalNotes = dto.Site.ApprovalNotes
                } : null
            };
        }
    }

    // DTO class for API responses
    public class BookmarkDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SiteId { get; set; }
        public DateTime CreatedAt { get; set; }
        public HeritageSiteDto? Site { get; set; }
    }

    public class HeritageSiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public string? HistoricalPeriod { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool HasPanoramaContent { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
    }
    
    public enum ApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
} 