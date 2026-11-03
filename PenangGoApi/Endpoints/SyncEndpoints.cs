using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync")
            .WithTags("Sync")
            .RequireAuthorization(Policies.RequireUser);

        // Applies the offline outbox: bookmarks queued while offline.
        group.MapPost("/upload", async (SyncUploadRequest request, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            var added = 0;
            if (request.BookmarkAddSiteIds != null)
            {
                foreach (var siteId in request.BookmarkAddSiteIds.Distinct())
                {
                    if (siteId <= 0) continue;
                    var exists = await db.Bookmarks
                        .AnyAsync(b => b.UserId == userIdInt && b.SiteId == siteId);
                    if (exists) continue;

                    var site = await db.HeritageSites.FindAsync(siteId);
                    if (site == null) continue;

                    db.Bookmarks.Add(new Bookmark
                    {
                        UserId = userIdInt,
                        SiteId = siteId,
                        CreatedAt = DateTime.UtcNow
                    });
                    site.TotalBookmarks++;
                    added++;
                }
            }

            var removed = 0;
            if (request.BookmarkRemoveIds != null)
            {
                foreach (var bookmarkId in request.BookmarkRemoveIds.Distinct())
                {
                    var bookmark = await db.Bookmarks.FirstOrDefaultAsync(
                        b => b.Id == bookmarkId && b.UserId == userIdInt);
                    if (bookmark == null) continue;

                    var site = await db.HeritageSites.FindAsync(bookmark.SiteId);
                    if (site != null)
                        site.TotalBookmarks = Math.Max(0, site.TotalBookmarks - 1);

                    db.Bookmarks.Remove(bookmark);
                    removed++;
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { added, removed });
        }).RequireRateLimiting("write");

        group.MapGet("/download", async (PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            var sites = await db.HeritageSites
                .Where(s => s.IsActive && s.ApprovalStatus == ApprovalStatus.Approved)
                .ToListAsync();
            var events = await db.Events
                .Where(e => e.IsActive && e.StartTime > DateTime.UtcNow)
                .ToListAsync();
            var userBookmarks = await db.Bookmarks
                .Where(b => b.UserId == userIdInt)
                .Include(b => b.Site)
                .ToListAsync();
            var userBookings = await db.Bookings
                .Where(b => b.UserId == userIdInt)
                .Include(b => b.Event)
                .ToListAsync();

            return Results.Ok(new
            {
                sites,
                events,
                userBookmarks,
                userBookings,
                lastSync = DateTime.UtcNow
            });
        });
    }
}

public record SyncUploadRequest(ICollection<int>? BookmarkAddSiteIds, ICollection<int>? BookmarkRemoveIds);