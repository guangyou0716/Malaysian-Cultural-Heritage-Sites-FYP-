using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh.Services
{
    public class ReviewService : IReviewService
    {
        private readonly PenangGoContext _context;
        
        public ReviewService(PenangGoContext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetAllReviewsAsync()
        {
            return await _context.Reviews
                .Include(r => r.Site)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Review>> GetSiteReviewsAsync(int siteId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.SiteId == siteId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Review>> GetUserReviewsAsync(int userId)
        {
            return await _context.Reviews
                .Include(r => r.Site)
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            return await _context.Reviews
                .Include(r => r.Site)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Review?> CreateReviewAsync(Review review)
        {
            review.CreatedAt = DateTime.Now;
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<Review?> UpdateReviewAsync(Review review)
        {
            var existingReview = await _context.Reviews.FindAsync(review.Id);
            if (existingReview == null)
                return null;

            existingReview.Rating = review.Rating;
            existingReview.Comment = review.Comment;
            
            await _context.SaveChangesAsync();
            return existingReview;
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddReviewAsync(int userId, int siteId, int rating, string comment)
        {
            var review = new Review
            {
                UserId = userId,
                SiteId = siteId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateReviewAsync(int reviewId, int userId, int rating, string comment)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);
            if (review != null)
            {
                review.Rating = rating;
                review.Comment = comment;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteReviewAsync(int reviewId, int userId)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
        }
    }
} 