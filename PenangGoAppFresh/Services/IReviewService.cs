using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public interface IReviewService
    {
        Task<List<Review>> GetAllReviewsAsync();
        Task<List<Review>> GetSiteReviewsAsync(int siteId);
        Task<List<Review>> GetUserReviewsAsync(int userId);
        Task<Review?> GetReviewByIdAsync(int id);
        Task<Review?> CreateReviewAsync(Review review);
        Task<Review?> UpdateReviewAsync(Review review);
        Task<bool> DeleteReviewAsync(int id);
        Task AddReviewAsync(int userId, int siteId, int rating, string comment);
        Task UpdateReviewAsync(int reviewId, int userId, int rating, string comment);
        Task DeleteReviewAsync(int reviewId, int userId);
    }
} 