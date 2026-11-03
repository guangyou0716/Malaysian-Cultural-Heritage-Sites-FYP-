using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.DTOs;
using PenangGoApi.Services;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class PanoramaEndpoints
{
    public static void MapPanoramaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/arcontent").WithTags("Panorama");

        group.MapGet("/", async (PenangGoContext db) =>
            await db.PanoramaContents
                .OrderByDescending(p => p.UploadedAt)
                .Select(p => new PanoramaContentDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    PanoramaUrl = p.PanoramaUrl,
                    HeritageSiteId = p.HeritageSiteId ?? 0,
                    UploadedBy = p.UploadedBy,
                    UploadedAt = p.UploadedAt
                }).ToListAsync());

        group.MapGet("/site/{siteId}", async (int siteId, PenangGoContext db) =>
            await db.PanoramaContents.Where(a => a.HeritageSiteId == siteId)
                .Select(p => new PanoramaContentDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    PanoramaUrl = p.PanoramaUrl,
                    HeritageSiteId = p.HeritageSiteId ?? 0,
                    UploadedBy = p.UploadedBy,
                    UploadedAt = p.UploadedAt
                }).ToListAsync());

        group.MapGet("/{id}", async (int id, PenangGoContext db) =>
        {
            var p = await db.PanoramaContents.FindAsync(id);
            if (p == null) return Results.NotFound();
            return Results.Ok(new PanoramaContentDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                PanoramaUrl = p.PanoramaUrl,
                HeritageSiteId = p.HeritageSiteId ?? 0,
                UploadedBy = p.UploadedBy,
                UploadedAt = p.UploadedAt
            });
        });

        group.MapPost("/", async (CreatePanoramaRequest request, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.PanoramaUrl))
                return ApiResponses.Problem("Title and panorama URL are required");
            if (!IsValidHttpUrl(request.PanoramaUrl))
                return ApiResponses.Problem("Panorama URL must be a valid http(s) URL", field: "panoramaUrl");
            if (request.HeritageSiteId <= 0)
                return ApiResponses.Problem("A heritage site must be selected", field: "heritageSiteId");

            var uploaderId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var panorama = new PanoramaContent
            {
                Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                PanoramaUrl = request.PanoramaUrl.Trim(),
                HeritageSiteId = request.HeritageSiteId,
                UploadedBy = uploaderId != null && int.TryParse(uploaderId, out var id) ? id : null,
                UploadedAt = DateTime.UtcNow
            };
            db.PanoramaContents.Add(panorama);

            var site = await db.HeritageSites.FindAsync(request.HeritageSiteId);
            if (site != null) site.HasPanoramaContent = true;

            await db.SaveChangesAsync();
            return Results.Created($"/api/arcontent/{panorama.Id}", new PanoramaContentDto
            {
                Id = panorama.Id,
                Title = panorama.Title,
                Description = panorama.Description,
                PanoramaUrl = panorama.PanoramaUrl,
                HeritageSiteId = panorama.HeritageSiteId ?? 0,
                UploadedBy = panorama.UploadedBy,
                UploadedAt = panorama.UploadedAt
            });
        }).RequireAuthorization(Policies.RequireAdmin)
          .RequireRateLimiting("write");

        group.MapPut("/{id}", async (int id, UpdatePanoramaRequest request, PenangGoContext db) =>
        {
            var pc = await db.PanoramaContents.FindAsync(id);
            if (pc is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.PanoramaUrl))
                return ApiResponses.Problem("Title and panorama URL are required");
            if (!IsValidHttpUrl(request.PanoramaUrl))
                return ApiResponses.Problem("Panorama URL must be a valid http(s) URL", field: "panoramaUrl");

            pc.Title = request.Title.Trim();
            pc.Description = request.Description?.Trim() ?? string.Empty;
            pc.PanoramaUrl = request.PanoramaUrl.Trim();
            await db.SaveChangesAsync();

            var site = await db.HeritageSites.FindAsync(pc.HeritageSiteId);
            if (site != null)
            {
                site.HasPanoramaContent = true;
                await db.SaveChangesAsync();
            }

            return Results.Ok(new PanoramaContentDto
            {
                Id = pc.Id,
                Title = pc.Title,
                Description = pc.Description,
                PanoramaUrl = pc.PanoramaUrl,
                HeritageSiteId = pc.HeritageSiteId ?? 0,
                UploadedBy = pc.UploadedBy,
                UploadedAt = pc.UploadedAt
            });
        }).RequireAuthorization(Policies.RequireAdmin)
          .RequireRateLimiting("write");

        group.MapDelete("/{id}", async (int id, PenangGoContext db) =>
        {
            var pc = await db.PanoramaContents.FindAsync(id);
            if (pc is null) return Results.NotFound();

            var siteId = pc.HeritageSiteId;
            db.PanoramaContents.Remove(pc);
            await db.SaveChangesAsync();

            if (siteId.HasValue)
            {
                var site = await db.HeritageSites.FindAsync(siteId.Value);
                if (site != null)
                {
                    var hasAny = await db.PanoramaContents.AnyAsync(p => p.HeritageSiteId == site.Id);
                    site.HasPanoramaContent = hasAny;
                    await db.SaveChangesAsync();
                }
            }

            return Results.NoContent();
        }).RequireAuthorization(Policies.RequireAdmin);
    }

    private static bool IsValidHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public record CreatePanoramaRequest(int HeritageSiteId, string Title, string? Description, string PanoramaUrl);
public record UpdatePanoramaRequest(string Title, string? Description, string PanoramaUrl);