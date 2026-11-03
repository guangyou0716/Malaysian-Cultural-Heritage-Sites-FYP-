using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.Services;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class AuthEndpoints
{
    private static (bool ok, string? error) ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return (false, "Password is required");
        if (password.Length < 8) return (false, "Password must be at least 8 characters long");
        if (!password.Any(char.IsUpper)) return (false, "Password must contain at least one uppercase letter");
        if (!password.Any(char.IsLower)) return (false, "Password must contain at least one lowercase letter");
        if (!password.Any(char.IsDigit)) return (false, "Password must contain at least one number");

        var specialChars = @"!@#$%^&*()_+-=[]{}|;:,.<>?";
        if (!password.Any(c => specialChars.Contains(c)))
            return (false, "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)");

        return (true, null);
    }

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, PenangGoContext db, ITokenService tokenService) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || user.IsAccountLocked())
            {
                return ApiResponses.Problem("Invalid credentials or account locked",
                    extensions: new Dictionary<string, object?> { ["success"] = false });
            }

            var isValidPassword = PasswordUtility.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
            if (!isValidPassword)
            {
                user.IncrementLoginAttempts();
                await db.SaveChangesAsync();
                return ApiResponses.Problem("Invalid credentials",
                    extensions: new Dictionary<string, object?> { ["success"] = false });
            }

            user.UpdateLastLogin();
            user.SessionToken = Guid.NewGuid().ToString();
            user.SessionExpiresAt = DateTime.UtcNow.AddDays(7);

            var accessToken = tokenService.GenerateAccessToken(user);
            var refreshToken = tokenService.GenerateRefreshToken();
            user.RefreshTokenHash = tokenService.HashRefreshToken(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                success = true,
                message = "Login successful",
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    isAdmin = user.IsAdmin
                },
                accessToken,
                refreshToken,
                sessionToken = user.SessionToken
            });
        }).RequireRateLimiting("auth");

        group.MapPost("/signup", async (SignupRequest request, PenangGoContext db) =>
        {
            Dictionary<string, object?> ClientError() => new() { ["success"] = false };

            if (string.IsNullOrWhiteSpace(request.Email))
                return ApiResponses.Problem("Email is required", field: "email", extensions: ClientError());

            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(request.Email))
                return ApiResponses.Problem("Please enter a valid email address", field: "email", extensions: ClientError());

            if (await db.Users.AnyAsync(u => u.Email == request.Email))
                return ApiResponses.Problem("Email already registered", StatusCodes.Status409Conflict, "email", ClientError());

            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
                return ApiResponses.Problem("Name must be at least 2 characters long", field: "name", extensions: ClientError());

            var (valid, error) = ValidateNewPassword(request.Password);
            if (!valid)
                return ApiResponses.Problem(error!, field: "password", extensions: ClientError());

            var passwordHash = PasswordUtility.CreatePasswordHash(request.Password);
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash.hash,
                PasswordSalt = passwordHash.salt,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsAdmin = false
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Created($"/api/users/{user.Id}", new
            {
                success = true,
                message = "Registration successful",
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    isAdmin = user.IsAdmin
                }
            });
        }).RequireRateLimiting("auth");

        group.MapPost("/logout", async (LogoutRequest request, PenangGoContext db, ITokenService tokenService, ClaimsPrincipal principal) =>
        {
            User? user = null;

            // Prefer the authenticated JWT identity when present
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id != null && int.TryParse(id, out var userId))
                user = await db.Users.FindAsync(userId);

            // Fall back to session token / refresh token for legacy callers
            user ??= await db.Users.FirstOrDefaultAsync(u =>
                (request.SessionToken != null && u.SessionToken == request.SessionToken) ||
                (request.RefreshToken != null && u.RefreshTokenHash == tokenService.HashRefreshToken(request.RefreshToken)));

            if (user != null)
            {
                user.SessionToken = null;
                user.SessionExpiresAt = null;
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                await db.SaveChangesAsync();
            }
            return Results.Ok(new { message = "Logged out successfully" });
        });

        group.MapPost("/refresh", async (RefreshTokenRequest request, PenangGoContext db, ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Results.Unauthorized();

            var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
            var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == tokenHash);
            if (user == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
                return Results.Unauthorized();

            var accessToken = tokenService.GenerateAccessToken(user);
            var newRefreshToken = tokenService.GenerateRefreshToken();
            user.RefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                accessToken,
                refreshToken = newRefreshToken
            });
        });

        group.MapPost("/change-password", async (ChangePasswordRequest request, PenangGoContext db, ClaimsPrincipal principal, HttpContext http) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            var user = await db.Users.FindAsync(userIdInt);
            if (user is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                !PasswordUtility.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                return ApiResponses.Problem("Current password is incorrect", field: "currentPassword");

            var (valid, error) = ValidateNewPassword(request.NewPassword);
            if (!valid)
                return ApiResponses.Problem(error!, field: "newPassword");

            var hash = PasswordUtility.CreatePasswordHash(request.NewPassword);
            user.PasswordHash = hash.hash;
            user.PasswordSalt = hash.salt;
            // Revoke refresh tokens so other sessions must re-authenticate
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "User.PasswordChanged", "User", userIdInt.ToString(), http);
            return Results.Ok(new { message = "Password updated successfully" });
        }).RequireAuthorization(Policies.RequireUser)
          .RequireRateLimiting("write");

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, PenangGoContext db, ITokenService tokenService, IEmailService emailService) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user != null)
            {
                var resetToken = tokenService.GenerateRefreshToken();
                user.PasswordResetTokenHash = tokenService.HashRefreshToken(resetToken);
                user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
                await db.SaveChangesAsync();
                try { await emailService.SendPasswordResetEmailAsync(user, resetToken); } catch { }
            }
            // Always return the same response to avoid user enumeration
            return Results.Ok(new { message = "If that email is registered, a password reset code has been sent." });
        }).RequireRateLimiting("auth");

        group.MapPost("/reset-password", async (ResetPasswordRequest request, PenangGoContext db, ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return ApiResponses.Problem("Reset code is required", field: "token");

            var tokenHash = tokenService.HashRefreshToken(request.Token);
            var user = await db.Users.FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash);
            if (user == null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
                return ApiResponses.Problem("Reset code is invalid or has expired", field: "token");

            var (valid, error) = ValidateNewPassword(request.NewPassword);
            if (!valid)
                return ApiResponses.Problem(error!, field: "newPassword");

            var hash = PasswordUtility.CreatePasswordHash(request.NewPassword);
            user.PasswordHash = hash.hash;
            user.PasswordSalt = hash.salt;
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.SessionToken = null;
            user.SessionExpiresAt = null;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Password has been reset. You can now log in." });
        }).RequireRateLimiting("auth");
    }
}

record LoginRequest(string Email, string Password);
record SignupRequest(string Name, string Email, string Password);
record LogoutRequest(string? SessionToken = null, string? RefreshToken = null);
record RefreshTokenRequest(string RefreshToken);
record ChangePasswordRequest(string CurrentPassword, string NewPassword);
record ForgotPasswordRequest(string Email);
record ResetPasswordRequest(string Token, string NewPassword);