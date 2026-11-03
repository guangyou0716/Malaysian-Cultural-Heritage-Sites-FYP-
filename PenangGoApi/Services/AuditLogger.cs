using System.Security.Claims;
using PenangGoApp.DataAccess;

namespace PenangGoApi.Services;

public static class AuditLogger
{
    public static Task LogAsync(
        PenangGoContext db,
        ClaimsPrincipal? principal,
        string action,
        string entityType,
        string entityId,
        HttpContext? httpContext = null,
        string? before = null,
        string? after = null)
    {
        var actorId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId != null && int.TryParse(actorId, out var id) ? id : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = before,
            After = after,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        return db.SaveChangesAsync();
    }
}