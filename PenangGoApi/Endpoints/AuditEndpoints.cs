using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auditlogs").WithTags("AuditLogs");

        group.MapGet("/", async (PenangGoContext db, int? take) =>
            await db.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(take ?? 200)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    ActorId = a.ActorId,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Before = a.Before,
                    After = a.After,
                    IpAddress = a.IpAddress,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync())
            .RequireAuthorization(Policies.RequireAdmin);
    }
}

public record AuditLogDto
{
    public int Id { get; set; }
    public int? ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Before { get; set; }
    public string? After { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}