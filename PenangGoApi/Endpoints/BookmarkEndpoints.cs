using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.DTOs;
using PenangGoApi.Services;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class BookmarkEndpoints
{
    public static void MapBookmarkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookmarks").WithTags("Bookmarks");

        group.MapGet("/", async (PenangGoContext db) =>
            await db.Bookmarks.Select(b => new BookmarkDto
            {
                Id = b.Id,
                UserId = b.UserId,
                SiteId = b.SiteId,
                CreatedAt = b.CreatedAt
            }).ToListAsync()).RequireAuthorization(Policies.RequireAdmin);

        group.MapGet("/user/{userId}", async (int userId, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null || (int.Parse(currentUserId) != userId && !isAdmin))
                return Results.Forbid();

            var bookmarks = await db.Bookmarks
                .Where(b => b.UserId == userId)
                .Select(b => new { b.Id, b.UserId, b.SiteId, b.CreatedAt })
                .ToListAsync();

            var siteIds = bookmarks.Select(b => b.SiteId).Distinct().ToList();
            var sites = await db.HeritageSites
                .Where(s => siteIds.Contains(s.Id))
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.Location,
                    s.ImageUrl,
                    s.AverageRating,
                    s.TotalReviews,
                    s.IsActive,
                    s.Category,
                    s.HistoricalPeriod,
                    s.HasPanoramaContent,
                    s.ApprovalStatus,
                    s.SubmittedAt,
                    s.ApprovedAt,
                    s.ApprovalNotes
                })
                .ToListAsync();

            var siteDict = sites.ToDictionary(s => s.Id);

            return Results.Ok(bookmarks.Select(b => new BookmarkDto
            {
                Id = b.Id,
                UserId = b.UserId,
                SiteId = b.SiteId,
                CreatedAt = b.CreatedAt,
                Site = siteDict.ContainsKey(b.SiteId) ? new HeritageSiteDto
                {
                    Id = siteDict[b.SiteId].Id,
                    Name = siteDict[b.SiteId].Name,
                    Description = siteDict[b.SiteId].Description,
                    Location = siteDict[b.SiteId].Location,
                    ImageUrl = siteDict[b.SiteId].ImageUrl,
                    AverageRating = siteDict[b.SiteId].AverageRating,
                    TotalReviews = siteDict[b.SiteId].TotalReviews,
                    IsActive = siteDict[b.SiteId].IsActive,
                    Category = siteDict[b.SiteId].Category,
                    HistoricalPeriod = siteDict[b.SiteId].HistoricalPeriod,
                    HasPanoramaContent = siteDict[b.SiteId].HasPanoramaContent,
                    ApprovalStatus = siteDict[b.SiteId].ApprovalStatus,
                    SubmittedAt = siteDict[b.SiteId].SubmittedAt,
                    ApprovedAt = siteDict[b.SiteId].ApprovedAt,
                    ApprovalNotes = siteDict[b.SiteId].ApprovalNotes
                } : null
            }).ToList());
        }).RequireAuthorization(Policies.RequireUser);

        group.MapPost("/", async (CreateBookmarkRequest request, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            var existing = await db.Bookmarks
                .FirstOrDefaultAsync(b => b.UserId == userIdInt && b.SiteId == request.SiteId);
            if (existing != null)
                return ApiResponses.Problem("Bookmark already exists", StatusCodes.Status409Conflict);

            var bookmark = new Bookmark
            {
                UserId = userIdInt,
                SiteId = request.SiteId,
                CreatedAt = DateTime.UtcNow
            };
            db.Bookmarks.Add(bookmark);

            var site = await db.HeritageSites.FindAsync(bookmark.SiteId);
            if (site != null) site.TotalBookmarks++;

            await db.SaveChangesAsync();
            return Results.Created($"/api/bookmarks/{bookmark.Id}", new BookmarkDto
            {
                Id = bookmark.Id,
                UserId = bookmark.UserId,
                SiteId = bookmark.SiteId,
                CreatedAt = bookmark.CreatedAt
            });
        }).RequireAuthorization(Policies.RequireUser)
          .RequireRateLimiting("write");

        group.MapDelete("/{id}", async (int id, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null) return Results.Unauthorized();

            var bookmark = await db.Bookmarks.FindAsync(id);
            if (bookmark is null) return Results.NotFound();
            if (bookmark.UserId != int.Parse(currentUserId) && !isAdmin) return Results.Forbid();

            var site = await db.HeritageSites.FindAsync(bookmark.SiteId);
            if (site != null) site.TotalBookmarks = Math.Max(0, site.TotalBookmarks - 1);

            db.Bookmarks.Remove(bookmark);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization(Policies.RequireUser);
    }
}

public record CreateBookmarkRequest(int SiteId);