using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.DTOs;
using PenangGoApi.Services;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", async (PenangGoContext db) =>
            await db.Users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                IsAdmin = u.IsAdmin,
                IsActive = u.IsActive,
                PhoneNumber = u.PhoneNumber,
                Bio = u.Bio,
                AccessibilityPreference = u.AccessibilityPreference,
                CreatedAt = u.CreatedAt
            }).ToListAsync())
            .RequireAuthorization(Policies.RequireAdmin);

        group.MapGet("/{id}", async (int id, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null || (int.Parse(currentUserId) != id && !isAdmin))
                return Results.Forbid();

            var u = await db.Users.FindAsync(id);
            if (u == null) return Results.NotFound();
            return Results.Ok(new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                IsAdmin = u.IsAdmin,
                IsActive = u.IsActive,
                PhoneNumber = u.PhoneNumber,
                Bio = u.Bio,
                AccessibilityPreference = u.AccessibilityPreference,
                CreatedAt = u.CreatedAt
            });
        }).RequireAuthorization(Policies.RequireUser);

        group.MapGet("/profile", async (string? sessionToken, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            User? u = null;

            // Prefer the authenticated JWT identity when present
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id != null && int.TryParse(id, out var userId))
                u = await db.Users.FindAsync(userId);

            // Fall back to session token for legacy callers
            u ??= await db.Users.FirstOrDefaultAsync(x =>
                x.SessionToken == sessionToken && x.SessionExpiresAt > DateTime.UtcNow);

            if (u == null) return Results.Unauthorized();
            return Results.Ok(new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                IsAdmin = u.IsAdmin,
                IsActive = u.IsActive,
                PhoneNumber = u.PhoneNumber,
                Bio = u.Bio,
                AccessibilityPreference = u.AccessibilityPreference,
                CreatedAt = u.CreatedAt
            });
        }).RequireAuthorization(Policies.RequireUser);

        group.MapPost("/", async (CreateUserRequest request, PenangGoContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { message = "Email and password are required" });

            if (await db.Users.AnyAsync(u => u.Email == request.Email))
                return Results.BadRequest(new { message = "Email already registered" });

            var passwordHash = PasswordUtility.CreatePasswordHash(request.Password);
            var user = new User
            {
                Name = string.IsNullOrWhiteSpace(request.Name) ? request.Email : request.Name,
                Email = request.Email,
                PasswordHash = passwordHash.hash,
                PasswordSalt = passwordHash.salt,
                IsAdmin = request.IsAdmin,
                IsActive = request.IsActive,
                PhoneNumber = request.PhoneNumber,
                Bio = request.Bio,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Created($"/api/users/{user.Id}", new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                AccessibilityPreference = user.AccessibilityPreference,
                CreatedAt = user.CreatedAt
            });
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}", async (int id, UpdateUserRequest request, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");

            if (currentUserId == null || (int.Parse(currentUserId) != id && !isAdmin))
                return Results.Forbid();

            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            if (request.Name != null) user.Name = request.Name;
            if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
            if (request.Bio != null) user.Bio = request.Bio;
            if (request.AccessibilityPreference != null) user.AccessibilityPreference = request.AccessibilityPreference;
            if (request.EmailNotificationsEnabled.HasValue) user.EmailNotificationsEnabled = request.EmailNotificationsEnabled.Value;
            if (request.PushNotificationsEnabled.HasValue) user.PushNotificationsEnabled = request.PushNotificationsEnabled.Value;

            await db.SaveChangesAsync();
            return Results.Ok(new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                AccessibilityPreference = user.AccessibilityPreference,
                CreatedAt = user.CreatedAt
            });
        }).RequireAuthorization(Policies.RequireUser);

        group.MapPut("/{id}/role", async (int id, SetUserRoleRequest request, PenangGoContext db, ClaimsPrincipal principal, HttpContext http) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            var before = user.IsAdmin.ToString();
            user.IsAdmin = request.IsAdmin;
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "User.RoleChanged", "User", id.ToString(), http, before, request.IsAdmin.ToString());

            return Results.Ok(new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                AccessibilityPreference = user.AccessibilityPreference,
                CreatedAt = user.CreatedAt
            });
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}/status", async (int id, SetUserStatusRequest request, PenangGoContext db, ClaimsPrincipal principal, HttpContext http) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            var before = user.IsActive.ToString();
            user.IsActive = request.IsActive;
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "User.StatusChanged", "User", id.ToString(), http, before, request.IsActive.ToString());

            return Results.Ok(new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                AccessibilityPreference = user.AccessibilityPreference,
                CreatedAt = user.CreatedAt
            });
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapDelete("/{id}", async (int id, PenangGoContext db, ClaimsPrincipal principal, HttpContext http) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");

            if (currentUserId == null || (int.Parse(currentUserId) != id && !isAdmin))
                return Results.Forbid();

            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "User.Deleted", "User", id.ToString(), http);
            return Results.NoContent();
        }).RequireAuthorization(Policies.RequireUser);
    }
}

record CreateUserRequest(string Name, string Email, string Password, bool IsAdmin = false, bool IsActive = true, string? PhoneNumber = null, string? Bio = null);
record SetUserRoleRequest(bool IsAdmin);
record SetUserStatusRequest(bool IsActive);