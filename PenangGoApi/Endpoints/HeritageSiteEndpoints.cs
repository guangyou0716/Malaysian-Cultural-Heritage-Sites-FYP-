using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.DTOs;
using PenangGoApi.Services;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class HeritageSiteEndpoints
{
    public static void MapHeritageSiteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/heritagesites").WithTags("HeritageSites");

        group.MapGet("/", async (PenangGoContext db) =>
            await db.HeritageSites
                .Where(h => h.ApprovalStatus == ApprovalStatus.Approved)
                .Select(h => new HeritageSiteDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Description = h.Description,
                    Location = h.Location,
                    IsActive = h.IsActive,
                    Category = h.Category,
                    HistoricalPeriod = h.HistoricalPeriod,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.AverageRating,
                    TotalReviews = h.TotalReviews,
                    HasPanoramaContent = h.HasPanoramaContent,
                    RequiresReservation = h.RequiresReservation,
                    DailyCapacity = h.DailyCapacity,
                    ApprovalStatus = h.ApprovalStatus,
                    SubmittedAt = h.SubmittedAt,
                    ApprovedAt = h.ApprovedAt,
                    ApprovalNotes = h.ApprovalNotes
                }).ToListAsync());

        group.MapGet("/{id}", async (int id, PenangGoContext db) =>
        {
            var h = await db.HeritageSites.FindAsync(id);
            if (h == null) return Results.NotFound();
            return Results.Ok(new HeritageSiteDto
            {
                Id = h.Id,
                Name = h.Name,
                Description = h.Description,
                Location = h.Location,
                IsActive = h.IsActive,
                Category = h.Category,
                HistoricalPeriod = h.HistoricalPeriod,
                ImageUrl = h.ImageUrl,
                AverageRating = h.AverageRating,
                TotalReviews = h.TotalReviews,
                HasPanoramaContent = h.HasPanoramaContent,
                RequiresReservation = h.RequiresReservation,
                DailyCapacity = h.DailyCapacity,
                ApprovalStatus = h.ApprovalStatus,
                SubmittedAt = h.SubmittedAt,
                ApprovedAt = h.ApprovedAt,
                ApprovalNotes = h.ApprovalNotes
            });
        });

        group.MapGet("/with-ar", async (PenangGoContext db) =>
            await db.HeritageSites.Where(h => h.HasPanoramaContent && h.ApprovalStatus == ApprovalStatus.Approved)
                .Select(h => new HeritageSiteDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Description = h.Description,
                    Location = h.Location,
                    IsActive = h.IsActive,
                    Category = h.Category,
                    HistoricalPeriod = h.HistoricalPeriod,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.AverageRating,
                    TotalReviews = h.TotalReviews,
                    HasPanoramaContent = h.HasPanoramaContent,
                    RequiresReservation = h.RequiresReservation,
                    DailyCapacity = h.DailyCapacity,
                    ApprovalStatus = h.ApprovalStatus,
                    SubmittedAt = h.SubmittedAt,
                    ApprovedAt = h.ApprovedAt,
                    ApprovalNotes = h.ApprovalNotes
                }).ToListAsync());

        group.MapGet("/category/{category}", async (string category, PenangGoContext db) =>
            await db.HeritageSites.Where(h => h.Category == category)
                .Select(h => new HeritageSiteDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Description = h.Description,
                    Location = h.Location,
                    IsActive = h.IsActive,
                    Category = h.Category,
                    HistoricalPeriod = h.HistoricalPeriod,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.AverageRating,
                    TotalReviews = h.TotalReviews,
                    HasPanoramaContent = h.HasPanoramaContent,
                    RequiresReservation = h.RequiresReservation,
                    DailyCapacity = h.DailyCapacity,
                    ApprovalStatus = h.ApprovalStatus,
                    SubmittedAt = h.SubmittedAt,
                    ApprovedAt = h.ApprovedAt,
                    ApprovalNotes = h.ApprovalNotes
                }).ToListAsync());

        group.MapGet("/manage", async (PenangGoContext db) =>
            await db.HeritageSites
                .Select(h => new HeritageSiteDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Description = h.Description,
                    Location = h.Location,
                    IsActive = h.IsActive,
                    Category = h.Category,
                    HistoricalPeriod = h.HistoricalPeriod,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.AverageRating,
                    TotalReviews = h.TotalReviews,
                    HasPanoramaContent = h.HasPanoramaContent,
                    RequiresReservation = h.RequiresReservation,
                    DailyCapacity = h.DailyCapacity,
                    ApprovalStatus = h.ApprovalStatus,
                    SubmittedAt = h.SubmittedAt,
                    ApprovedAt = h.ApprovedAt,
                    ApprovalNotes = h.ApprovalNotes
                }).ToListAsync())
            .RequireAuthorization(Policies.RequireAdmin);

        group.MapPost("/submissions", async (CreateSiteSubmissionRequest request, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Description) ||
                string.IsNullOrWhiteSpace(request.Location))
                return ApiResponses.Problem("Name, description and location are required");

            var site = new HeritageSite
            {
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Location = request.Location.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Category = request.Category,
                HistoricalPeriod = request.HistoricalPeriod,
                CulturalSignificance = request.CulturalSignificance,
                VisitingHours = request.VisitingHours,
                EntryFee = request.EntryFee,
                ContactInfo = request.ContactInfo,
                Website = request.Website,
                IsAccessible = request.IsAccessible,
                ImageUrl = request.ImageUrl,
                HasPanoramaContent = false,
                IsActive = false,
                ApprovalStatus = ApprovalStatus.Pending,
                SubmittedBy = userIdInt,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            db.HeritageSites.Add(site);
            await db.SaveChangesAsync();

            return Results.Created($"/api/heritagesites/{site.Id}", new HeritageSiteDto
            {
                Id = site.Id,
                Name = site.Name,
                Description = site.Description,
                Location = site.Location,
                IsActive = site.IsActive,
                Category = site.Category,
                HistoricalPeriod = site.HistoricalPeriod,
                ImageUrl = site.ImageUrl,
                AverageRating = site.AverageRating,
                TotalReviews = site.TotalReviews,
                HasPanoramaContent = site.HasPanoramaContent,
                ApprovalStatus = site.ApprovalStatus,
                SubmittedAt = site.SubmittedAt,
                ApprovedAt = site.ApprovedAt,
                ApprovalNotes = site.ApprovalNotes
            });
        }).RequireAuthorization(Policies.RequireUser)
          .RequireRateLimiting("write");

        group.MapGet("/my-submissions", async (PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            return Results.Ok(await db.HeritageSites
                .Where(h => h.SubmittedBy == userIdInt)
                .Select(h => new HeritageSiteDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Description = h.Description,
                    Location = h.Location,
                    IsActive = h.IsActive,
                    Category = h.Category,
                    HistoricalPeriod = h.HistoricalPeriod,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.AverageRating,
                    TotalReviews = h.TotalReviews,
                    HasPanoramaContent = h.HasPanoramaContent,
                    RequiresReservation = h.RequiresReservation,
                    DailyCapacity = h.DailyCapacity,
                    ApprovalStatus = h.ApprovalStatus,
                    SubmittedAt = h.SubmittedAt,
                    ApprovedAt = h.ApprovedAt,
                    ApprovalNotes = h.ApprovalNotes
                }).ToListAsync());
        }).RequireAuthorization(Policies.RequireUser);

        group.MapPost("/", async (HeritageSite site, PenangGoContext db) =>
        {
            site.CreatedAt = DateTime.UtcNow;
            db.HeritageSites.Add(site);
            await db.SaveChangesAsync();
            return Results.Created($"/api/heritagesites/{site.Id}", new HeritageSiteDto
            {
                Id = site.Id,
                Name = site.Name,
                Description = site.Description,
                Location = site.Location,
                IsActive = site.IsActive,
                Category = site.Category,
                HistoricalPeriod = site.HistoricalPeriod,
                ImageUrl = site.ImageUrl,
                AverageRating = site.AverageRating,
                TotalReviews = site.TotalReviews,
                HasPanoramaContent = site.HasPanoramaContent
            });
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}", async (int id, HeritageSite inputSite, PenangGoContext db) =>
        {
            var site = await db.HeritageSites.FindAsync(id);
            if (site is null) return Results.NotFound();

            site.Name = inputSite.Name;
            site.Description = inputSite.Description;
            site.Location = inputSite.Location;
            site.Latitude = inputSite.Latitude;
            site.Longitude = inputSite.Longitude;
            site.HasPanoramaContent = inputSite.HasPanoramaContent;
            site.IsActive = inputSite.IsActive;
            site.Category = inputSite.Category;
            site.HistoricalPeriod = inputSite.HistoricalPeriod;
            site.CulturalSignificance = inputSite.CulturalSignificance;
            site.VisitingHours = inputSite.VisitingHours;
            site.EntryFee = inputSite.EntryFee;
            site.ContactInfo = inputSite.ContactInfo;
            site.Website = inputSite.Website;
            site.IsAccessible = inputSite.IsAccessible;
            site.ImageUrl = inputSite.ImageUrl;
            site.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(new HeritageSiteDto
            {
                Id = site.Id,
                Name = site.Name,
                Description = site.Description,
                Location = site.Location,
                IsActive = site.IsActive,
                Category = site.Category,
                HistoricalPeriod = site.HistoricalPeriod,
                ImageUrl = site.ImageUrl,
                AverageRating = site.AverageRating,
                TotalReviews = site.TotalReviews,
                HasPanoramaContent = site.HasPanoramaContent
            });
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapDelete("/{id}", async (int id, PenangGoContext db) =>
        {
            var site = await db.HeritageSites
                .Include(s => s.Bookmarks)
                .Include(s => s.Reviews)
                .Include(s => s.PanoramaContents)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site is null) return Results.NotFound();

            db.Bookmarks.RemoveRange(site.Bookmarks);
            db.Reviews.RemoveRange(site.Reviews);
            db.PanoramaContents.RemoveRange(site.PanoramaContents);
            db.HeritageSites.Remove(site);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}/approve", async (int id, PenangGoContext db, IEmailService emailService, ClaimsPrincipal principal, HttpContext http) =>
        {
            var site = await db.HeritageSites
                .Include(s => s.Submitter)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (site is null) return Results.NotFound();

            var adminId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            site.ApprovalStatus = ApprovalStatus.Approved;
            site.IsActive = true;
            site.ApprovedBy = adminId != null && int.TryParse(adminId, out var adminInt) ? adminInt : null;
            site.ApprovedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "Site.Approved", "HeritageSite", id.ToString(), http, "Pending", "Approved");

            if (site.Submitter != null)
                await emailService.SendSiteApprovalEmailAsync(site.Submitter, site);

            return Results.Ok(new HeritageSiteDto
            {
                Id = site.Id,
                Name = site.Name,
                Description = site.Description,
                Location = site.Location,
                IsActive = site.IsActive,
                Category = site.Category,
                HistoricalPeriod = site.HistoricalPeriod,
                ImageUrl = site.ImageUrl,
                AverageRating = site.AverageRating,
                TotalReviews = site.TotalReviews,
                HasPanoramaContent = site.HasPanoramaContent,
                ApprovalStatus = site.ApprovalStatus,
                SubmittedAt = site.SubmittedAt,
                ApprovedAt = site.ApprovedAt
            });
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}/reject", async (int id, RejectSiteRequest request, PenangGoContext db, IEmailService emailService, ClaimsPrincipal principal, HttpContext http) =>
        {
            var site = await db.HeritageSites
                .Include(s => s.Submitter)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (site is null) return Results.NotFound();

            var adminId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            site.ApprovalStatus = ApprovalStatus.Rejected;
            site.IsActive = false;
            site.ApprovedBy = adminId != null && int.TryParse(adminId, out var adminInt) ? adminInt : null;
            site.ApprovedAt = DateTime.UtcNow;
            site.ApprovalNotes = request.Reason;
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "Site.Rejected", "HeritageSite", id.ToString(), http, "Pending", $"Rejected: {request.Reason}");

            if (site.Submitter != null)
                await emailService.SendSiteRejectionEmailAsync(site.Submitter, site, request.Reason);

            return Results.Ok(new HeritageSiteDto
            {
                Id = site.Id,
                Name = site.Name,
                Description = site.Description,
                Location = site.Location,
                IsActive = site.IsActive,
                Category = site.Category,
                HistoricalPeriod = site.HistoricalPeriod,
                ImageUrl = site.ImageUrl,
                AverageRating = site.AverageRating,
                TotalReviews = site.TotalReviews,
                HasPanoramaContent = site.HasPanoramaContent,
                ApprovalStatus = site.ApprovalStatus,
                SubmittedAt = site.SubmittedAt,
                ApprovedAt = site.ApprovedAt,
                ApprovalNotes = site.ApprovalNotes
            });
        }).RequireAuthorization(Policies.RequireAdmin);
    }
}

public record RejectSiteRequest(string Reason);

public record CreateSiteSubmissionRequest(
    string Name,
    string Description,
    string Location,
    double Latitude,
    double Longitude,
    string? Category,
    string? HistoricalPeriod,
    string? CulturalSignificance,
    string? VisitingHours,
    string? EntryFee,
    string? ContactInfo,
    string? Website,
    bool IsAccessible,
    string? ImageUrl);
