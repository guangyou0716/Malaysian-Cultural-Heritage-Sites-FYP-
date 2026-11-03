using System.Text;
using System.Text.Json;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class ReviewService : IReviewService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ReviewService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<List<Review>> GetAllReviewsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/reviews");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var dtos = JsonSerializer.Deserialize<List<ApiReviewDto>>(content, _jsonOptions) ?? new();
                return dtos.Select(d => new Review
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    SiteId = d.SiteId,
                    Rating = d.Rating,
                    Comment = d.Comment ?? string.Empty,
                    CreatedAt = d.CreatedAt,
                    ApprovalStatus = d.ApprovalStatus,
                    User = d.UserName == null ? null! : new User { Id = d.UserId, Name = d.UserName },
                    Site = d.SiteName == null ? null! : new HeritageSite { Id = d.SiteId, Name = d.SiteName }
                }).ToList();
            }
            catch { return new List<Review>(); }
        }

        public async Task<List<Review>> GetSiteReviewsAsync(int siteId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/reviews/site/{siteId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Review>>(content, _jsonOptions) ?? new();
            }
            catch { return new List<Review>(); }
        }

        public async Task<List<Review>> GetUserReviewsAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/reviews/user/{userId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Review>>(content, _jsonOptions) ?? new();
            }
            catch { return new List<Review>(); }
        }

        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/reviews/{id}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Review>(content, _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<Review?> CreateReviewAsync(Review review)
        {
            try
            {
                var json = JsonSerializer.Serialize(review, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/reviews", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Review>(responseContent, _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<Review?> UpdateReviewAsync(Review review)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { rating = review.Rating, comment = review.Comment }, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/reviews/{review.Id}", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Review>(responseContent, _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/reviews/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task AddReviewAsync(int userId, int siteId, int rating, string comment)
        {
            var review = new Review
            {
                UserId = userId,
                SiteId = siteId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
            await CreateReviewAsync(review);
        }

        public async Task UpdateReviewAsync(int reviewId, int userId, int rating, string comment)
        {
            var review = new Review { Id = reviewId, Rating = rating, Comment = comment };
            await UpdateReviewAsync(review);
        }

        public async Task DeleteReviewAsync(int reviewId, int userId)
        {
            await DeleteReviewAsync(reviewId);
        }

        public async Task<bool> ApproveReviewAsync(int reviewId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/reviews/{reviewId}/approve", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> RejectReviewAsync(int reviewId, string reason)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { reason }, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/reviews/{reviewId}/reject", content);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }

    internal class ApiReviewDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SiteId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public string? SiteName { get; set; }
        public string? UserName { get; set; }
    }
}
