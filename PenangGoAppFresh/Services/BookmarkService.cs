using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class BookmarkService
    {
        private readonly HttpClient _httpClient;
        private readonly OfflineSyncService _offlineSync;
        private readonly JsonSerializerOptions _jsonOptions;

        public BookmarkService(HttpClient httpClient, OfflineSyncService offlineSync)
        {
            _httpClient = httpClient;
            _offlineSync = offlineSync;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<Bookmark>> GetUserBookmarksAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/bookmarks/user/{userId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var dtos = JsonSerializer.Deserialize<List<BookmarkApiDto>>(content, _jsonOptions) ?? new();
                return dtos.Select(MapDto).ToList();
            }
            catch { return new List<Bookmark>(); }
        }

        public async Task<bool> AddBookmarkAsync(int userId, int siteId)
        {
            // Offline: queue into the outbox; it is flushed when connectivity returns
            if (!_offlineSync.IsOnline)
            {
                _offlineSync.QueueBookmarkAdd(siteId);
                return true;
            }

            // userId is derived from the access token by the server
            var json = JsonSerializer.Serialize(new { siteId }, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/bookmarks", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveBookmarkAsync(int userId, int siteId)
        {
            // Offline: queue removal using the mirrored server bookmark id
            if (!_offlineSync.IsOnline)
            {
                var cachedId = _offlineSync.TryResolveCachedBookmarkId(siteId);
                if (cachedId.HasValue)
                    return _offlineSync.QueueBookmarkRemove(cachedId.Value);
                return false;
            }

            var bookmarks = await GetUserBookmarksAsync(userId);
            var bookmark = bookmarks.FirstOrDefault(b => b.SiteId == siteId);
            if (bookmark == null) return false;
            var response = await _httpClient.DeleteAsync($"api/bookmarks/{bookmark.Id}");
            return response.IsSuccessStatusCode;
        }

        private static Bookmark MapDto(BookmarkApiDto dto) => new()
        {
            Id = dto.Id,
            UserId = dto.UserId,
            SiteId = dto.SiteId,
            CreatedAt = dto.CreatedAt,
            Site = dto.Site == null ? null! : new HeritageSite
            {
                Id = dto.Site.Id,
                Name = dto.Site.Name,
                Description = dto.Site.Description,
                Location = dto.Site.Location,
                ImageUrl = dto.Site.ImageUrl,
                AverageRating = dto.Site.AverageRating,
                TotalReviews = dto.Site.TotalReviews,
                IsActive = dto.Site.IsActive,
                Category = dto.Site.Category,
                HistoricalPeriod = dto.Site.HistoricalPeriod,
                HasPanoramaContent = dto.Site.HasPanoramaContent,
                ApprovalStatus = dto.Site.ApprovalStatus,
                SubmittedAt = dto.Site.SubmittedAt,
                ApprovedAt = dto.Site.ApprovedAt,
                ApprovalNotes = dto.Site.ApprovalNotes
            }
        };
    }

    internal class BookmarkApiDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SiteId { get; set; }
        public DateTime CreatedAt { get; set; }
        public BookmarkSiteDto? Site { get; set; }
    }

    internal class BookmarkSiteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public string? HistoricalPeriod { get; set; }
        public bool HasPanoramaContent { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
    }
}