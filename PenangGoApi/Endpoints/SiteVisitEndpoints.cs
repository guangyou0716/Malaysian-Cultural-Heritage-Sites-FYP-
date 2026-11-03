using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.Services;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class SiteVisitEndpoints
{
    public static void MapSiteVisitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sitevisits").WithTags("SiteVisits");

        group.MapGet("/", async (PenangGoContext db) =>
            await db.SiteVisitBookings
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => MapToDto(b))
                .ToListAsync())
            .RequireAuthorization(Policies.RequireAdmin);

        group.MapGet("/mine", async (PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            return Results.Ok(await db.SiteVisitBookings
                .Where(b => b.UserId == userIdInt)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => MapToDto(b))
                .ToListAsync());
        }).RequireAuthorization(Policies.RequireUser);

        group.MapPost("/", async (CreateSiteVisitRequest request, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            var site = await db.HeritageSites.FindAsync(request.HeritageSiteId);
            if (site is null)
                return ApiResponses.Problem("Site not found");

            if (!site.RequiresReservation)
                return ApiResponses.Problem("This site does not require a reservation");

            if (request.VisitDate.Date < DateTime.UtcNow.Date)
                return ApiResponses.Problem("Visit date cannot be in the past", field: "visitDate");

            var slots = new[] { "Morning", "Afternoon", "Evening" };
            if (!slots.Contains(request.TimeSlot))
                return ApiResponses.Problem("Time slot must be Morning, Afternoon or Evening", field: "timeSlot");

            if (request.Quantity < 1 || request.Quantity > 10)
                return ApiResponses.Problem("Quantity must be between 1 and 10", field: "quantity");

            // Capacity check per site/day/slot
            if (site.DailyCapacity.HasValue && site.DailyCapacity.Value > 0)
            {
                var booked = await db.SiteVisitBookings
                    .Where(b => b.HeritageSiteId == site.Id
                                && b.VisitDate == request.VisitDate.Date
                                && b.TimeSlot == request.TimeSlot
                                && b.Status == SiteVisitStatus.Confirmed)
                    .SumAsync(b => (int?)b.Quantity) ?? 0;

                if (booked + request.Quantity > site.DailyCapacity.Value)
                    return ApiResponses.Problem("No remaining capacity for this date and time slot", StatusCodes.Status409Conflict);
            }

            var booking = new SiteVisitBooking
            {
                UserId = userIdInt,
                HeritageSiteId = site.Id,
                VisitDate = request.VisitDate.Date,
                TimeSlot = request.TimeSlot,
                Quantity = request.Quantity,
                TotalAmount = 0, // sites are free; pricing is out of scope for the demo
                Status = SiteVisitStatus.Confirmed,
                Reference = "SV-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow
            };

            db.SiteVisitBookings.Add(booking);
            await db.SaveChangesAsync();
            return Results.Created($"/api/sitevisits/{booking.Id}", MapToDto(booking));
        }).RequireAuthorization(Policies.RequireUser)
          .RequireRateLimiting("write");

        group.MapPut("/{id}/cancel", async (int id, PenangGoContext db, ClaimsPrincipal principal, HttpContext http) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null) return Results.Unauthorized();

            var booking = await db.SiteVisitBookings.FindAsync(id);
            if (booking is null) return Results.NotFound();
            if (booking.UserId != int.Parse(currentUserId) && !isAdmin) return Results.Forbid();

            if (booking.Status == SiteVisitStatus.Confirmed)
            {
                booking.Status = SiteVisitStatus.Cancelled;
                await db.SaveChangesAsync();
                await AuditLogger.LogAsync(db, principal, "SiteVisit.Cancelled", "SiteVisitBooking", id.ToString(), http, "Confirmed", "Cancelled");
            }

            return Results.Ok(MapToDto(booking));
        }).RequireAuthorization(Policies.RequireUser);
    }

    private static SiteVisitBookingDto MapToDto(SiteVisitBooking b) => new()
    {
        Id = b.Id,
        UserId = b.UserId,
        HeritageSiteId = b.HeritageSiteId,
        VisitDate = b.VisitDate,
        TimeSlot = b.TimeSlot,
        Quantity = b.Quantity,
        TotalAmount = b.TotalAmount,
        Status = b.Status.ToString(),
        Reference = b.Reference,
        CreatedAt = b.CreatedAt
    };
}

public record CreateSiteVisitRequest(int HeritageSiteId, DateTime VisitDate, string TimeSlot, int Quantity = 1);

public record SiteVisitBookingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HeritageSiteId { get; set; }
    public DateTime VisitDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}