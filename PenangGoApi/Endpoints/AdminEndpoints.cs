using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics")
            .WithTags("Admin")
            .RequireAuthorization(Policies.RequireAdmin);

        group.MapGet("/overview", async (PenangGoContext db) =>
        {
            var totalUsers = await db.Users.CountAsync();
            var totalSites = await db.HeritageSites.CountAsync();
            var totalEvents = await db.Events.CountAsync();
            var totalBookings = await db.Bookings.CountAsync();
            var totalReviews = await db.Reviews.CountAsync();
            var totalBookmarks = await db.Bookmarks.CountAsync();

            return Results.Ok(new
            {
                totalUsers,
                totalSites,
                totalEvents,
                totalBookings,
                totalReviews,
                totalBookmarks,
                generatedAt = DateTime.UtcNow
            });
        });

        group.MapGet("/popular-sites", async (PenangGoContext db) =>
        {
            var popularSites = await db.HeritageSites
                .OrderByDescending(s => s.TotalBookmarks + s.TotalReviews)
                .Take(10)
                .Select(s => new { s.Id, s.Name, s.TotalBookmarks, s.TotalReviews, s.AverageRating })
                .ToListAsync();

            return Results.Ok(popularSites);
        });
    }
}
