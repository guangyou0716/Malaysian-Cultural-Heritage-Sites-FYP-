using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.DTOs;
using PenangGoApi.Services;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reviews").WithTags("Reviews");

        // Full list incl. pending — management use only
        group.MapGet("/", async (PenangGoContext db) =>
            await db.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Include(r => r.User)
                .Include(r => r.Site)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    SiteId = r.SiteId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    ApprovalStatus = r.ApprovalStatus,
                    SiteName = r.Site != null ? r.Site.Name : null,
                    UserName = r.User != null ? r.User.Name : null
                }).ToListAsync())
            .RequireAuthorization(Policies.RequireAdmin);

        // Public site reviews: approved only
        group.MapGet("/site/{siteId}", async (int siteId, PenangGoContext db) =>
            await db.Reviews.Where(r => r.SiteId == siteId && r.ApprovalStatus == ApprovalStatus.Approved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    SiteId = r.SiteId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    ApprovalStatus = r.ApprovalStatus
                }).ToListAsync());

        group.MapGet("/user/{userId}", async (int userId, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null || (int.Parse(currentUserId) != userId && !isAdmin))
                return Results.Forbid();

            return Results.Ok(await db.Reviews.Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    SiteId = r.SiteId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    ApprovalStatus = r.ApprovalStatus
                }).ToListAsync());
        }).RequireAuthorization(Policies.RequireUser);

        group.MapPost("/", async (UpsertReviewRequest request, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            if (request.Rating < 1 || request.Rating > 5)
                return ApiResponses.Problem("Rating must be between 1 and 5", field: "rating");
            if (string.IsNullOrWhiteSpace(request.Comment))
                return ApiResponses.Problem("Comment is required", field: "comment");

            var existing = await db.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userIdInt && r.SiteId == request.SiteId);
            if (existing != null)
                return ApiResponses.Problem("You have already reviewed this site", StatusCodes.Status409Conflict);

            var review = new Review
            {
                UserId = userIdInt,
                SiteId = request.SiteId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow,
                ApprovalStatus = ApprovalStatus.Pending
            };
            db.Reviews.Add(review);
            await db.SaveChangesAsync();

            return Results.Created($"/api/reviews/{review.Id}", new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                SiteId = review.SiteId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                ApprovalStatus = review.ApprovalStatus
            });
        }).RequireAuthorization(Policies.RequireUser)
          .RequireRateLimiting("write");

        group.MapPut("/{id}", async (int id, UpsertReviewRequest request, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null) return Results.Unauthorized();

            var review = await db.Reviews.FindAsync(id);
            if (review is null) return Results.NotFound();
            if (review.UserId != int.Parse(currentUserId) && !isAdmin) return Results.Forbid();

            if (request.Rating < 1 || request.Rating > 5)
                return ApiResponses.Problem("Rating must be between 1 and 5", field: "rating");
            if (string.IsNullOrWhiteSpace(request.Comment))
                return ApiResponses.Problem("Comment is required", field: "comment");

            review.Rating = request.Rating;
            review.Comment = request.Comment;
            await db.SaveChangesAsync();
            await RecomputeSiteRatingAsync(db, review.SiteId);

            return Results.Ok(new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                SiteId = review.SiteId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                ApprovalStatus = review.ApprovalStatus
            });
        }).RequireAuthorization(Policies.RequireUser)
          .RequireRateLimiting("write");

        group.MapPut("/{id}/approve", async (int id, PenangGoContext db, ClaimsPrincipal principal, HttpContext http) =>
        {
            var review = await db.Reviews.FindAsync(id);
            if (review is null) return Results.NotFound();

            review.ApprovalStatus = ApprovalStatus.Approved;
            await RecomputeSiteRatingAsync(db, review.SiteId);
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "Review.Approved", "Review", id.ToString(), http, "Pending", "Approved");

            return Results.Ok(new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                SiteId = review.SiteId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                ApprovalStatus = review.ApprovalStatus
            });
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}/reject", async (int id, RejectReviewRequest request, PenangGoContext db, ClaimsPrincipal principal, HttpContext http) =>
        {
            var review = await db.Reviews.FindAsync(id);
            if (review is null) return Results.NotFound();

            var reason = string.IsNullOrWhiteSpace(request.Reason) ? "No reason provided" : request.Reason;
            review.ApprovalStatus = ApprovalStatus.Rejected;
            await RecomputeSiteRatingAsync(db, review.SiteId);
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "Review.Rejected", "Review", id.ToString(), http, "Pending", $"Rejected: {reason}");

            return Results.Ok(new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                SiteId = review.SiteId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                ApprovalStatus = review.ApprovalStatus
            });
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapDelete("/{id}", async (int id, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null) return Results.Unauthorized();

            var review = await db.Reviews.FindAsync(id);
            if (review is null) return Results.NotFound();
            if (review.UserId != int.Parse(currentUserId) && !isAdmin) return Results.Forbid();

            var siteId = review.SiteId;
            db.Reviews.Remove(review);
            await db.SaveChangesAsync();
            await RecomputeSiteRatingAsync(db, siteId);

            return Results.NoContent();
        }).RequireAuthorization(Policies.RequireUser);
    }

    private static async Task RecomputeSiteRatingAsync(PenangGoContext db, int siteId)
    {
        var site = await db.HeritageSites.FindAsync(siteId);
        if (site == null) return;

        var approved = await db.Reviews
            .Where(r => r.SiteId == siteId && r.ApprovalStatus == ApprovalStatus.Approved)
            .ToListAsync();
        site.TotalReviews = approved.Count;
        site.AverageRating = approved.Count > 0 ? approved.Average(r => r.Rating) : 0;
    }
}

public record UpsertReviewRequest(int SiteId, int Rating, string Comment);
public record RejectReviewRequest(string Reason);