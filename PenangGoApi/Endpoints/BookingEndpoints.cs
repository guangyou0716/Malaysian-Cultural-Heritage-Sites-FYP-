using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.DTOs;
using PenangGoApi.Services;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");

        group.MapGet("/", async (PenangGoContext db) =>
            await db.Bookings.Include(b => b.User).Include(b => b.Event)
                .Select(b => MapToDto(b)).ToListAsync()).RequireAuthorization(Policies.RequireAdmin);

        group.MapGet("/user/{userId}", async (int userId, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null || (int.Parse(currentUserId) != userId && !isAdmin))
                return Results.Forbid();

            var bookings = await db.Bookings.Where(b => b.UserId == userId)
                .Include(b => b.User).Include(b => b.Event)
                .Select(b => MapToDto(b)).ToListAsync();
            return Results.Ok(bookings);
        }).RequireAuthorization(Policies.RequireUser);

        group.MapGet("/{id}", async (int id, PenangGoContext db, ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null) return Results.Unauthorized();

            var b = await db.Bookings.Include(b => b.User).Include(b => b.Event).FirstOrDefaultAsync(b => b.Id == id);
            if (b == null) return Results.NotFound();
            if (b.UserId != int.Parse(currentUserId) && !isAdmin) return Results.Forbid();
            return Results.Ok(MapToDto(b));
        }).RequireAuthorization(Policies.RequireUser);

        group.MapPost("/", async (CreateBookingRequest request, HttpRequest httpRequest, PenangGoContext db, IEmailService emailService, ClaimsPrincipal principal) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out var userIdInt))
                return Results.Unauthorized();

            var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await db.Bookings
                    .Include(b => b.User).Include(b => b.Event)
                    .FirstOrDefaultAsync(b => b.UserId == userIdInt && b.EventId == request.EventId && b.IdempotencyKey == idempotencyKey);
                if (existing != null)
                    return Results.Ok(MapToDto(existing));
            }

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var eventItem = await db.Events.FindAsync(request.EventId);
            if (eventItem == null)
            {
                await transaction.RollbackAsync();
                return ApiResponses.Problem("Event not found");
            }

            if (eventItem.CurrentBookings >= eventItem.MaxCapacity)
            {
                await transaction.RollbackAsync();
                return ApiResponses.Problem("Event is full", StatusCodes.Status409Conflict);
            }

            var booking = new Booking
            {
                UserId = userIdInt,
                EventId = request.EventId,
                IdempotencyKey = idempotencyKey,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed
            };

            db.Bookings.Add(booking);
            eventItem.CurrentBookings++;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                var user = await db.Users.FindAsync(booking.UserId);
                if (user != null)
                    await emailService.SendBookingConfirmationEmailAsync(booking, user, eventItem);
            }
            catch (Exception) { }

            var createdBooking = await db.Bookings.Include(b => b.User).Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == booking.Id);
            if (createdBooking == null) return Results.NotFound();

            return Results.Created($"/api/bookings/{booking.Id}", MapToDto(createdBooking));
        }).RequireAuthorization(Policies.RequireUser)
          .RequireRateLimiting("write");

        group.MapPut("/{id}", async (int id, UpdateBookingStatusRequest req, PenangGoContext db) =>
        {
            var existingBooking = await db.Bookings.FindAsync(id);
            if (existingBooking == null) return Results.NotFound();

            existingBooking.Status = req.Status;
            await db.SaveChangesAsync();

            var updatedBooking = await db.Bookings.Include(b => b.User).Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (updatedBooking == null) return Results.NotFound();
            return Results.Ok(MapToDto(updatedBooking));
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}/status", async (int id, UpdateBookingStatusRequest req, PenangGoContext db, IEmailService emailService, ClaimsPrincipal principal, HttpContext http) =>
        {
            var booking = await db.Bookings.FindAsync(id);
            if (booking is null) return Results.NotFound();

            var before = booking.Status.ToString();
            booking.Status = req.Status;
            await db.SaveChangesAsync();
            await AuditLogger.LogAsync(db, principal, "Booking.StatusChanged", "Booking", id.ToString(), http, before, req.Status.ToString());

            try
            {
                var user = await db.Users.FindAsync(booking.UserId);
                var eventItem = await db.Events.FindAsync(booking.EventId);
                if (user != null && eventItem != null)
                    await emailService.SendBookingStatusUpdateEmailAsync(booking, user, eventItem, req.Status);
            }
            catch (Exception) { }

            var updatedBooking = await db.Bookings.Include(b => b.User).Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (updatedBooking == null) return Results.NotFound();
            return Results.Ok(MapToDto(updatedBooking));
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}/cancel", async (int id, PenangGoContext db, IEmailService emailService, ClaimsPrincipal principal, HttpContext http) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = principal.IsInRole("Admin");
            if (currentUserId == null) return Results.Unauthorized();

            var booking = await db.Bookings.FindAsync(id);
            if (booking is null) return Results.NotFound();
            if (booking.UserId != int.Parse(currentUserId) && !isAdmin) return Results.Forbid();

            if (booking.Status == BookingStatus.Confirmed)
            {
                var before = booking.Status.ToString();
                booking.Status = BookingStatus.Cancelled;
                var eventItem = await db.Events.FindAsync(booking.EventId);
                if (eventItem != null)
                    eventItem.CurrentBookings = Math.Max(0, eventItem.CurrentBookings - 1);
                await db.SaveChangesAsync();
                await AuditLogger.LogAsync(db, principal, "Booking.Cancelled", "Booking", id.ToString(), http, before, "Cancelled");

                try
                {
                    var user = await db.Users.FindAsync(booking.UserId);
                    if (user != null && eventItem != null)
                        await emailService.SendBookingCancellationEmailAsync(booking, user, eventItem);
                }
                catch (Exception) { }
            }

            var updatedBooking = await db.Bookings.Include(b => b.User).Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (updatedBooking == null) return Results.NotFound();
            return Results.Ok(MapToDto(updatedBooking));
        }).RequireAuthorization(Policies.RequireUser);

        group.MapDelete("/{id}", async (int id, PenangGoContext db, IEmailService emailService) =>
        {
            var booking = await db.Bookings.FindAsync(id);
            if (booking is null) return Results.NotFound();

            var user = await db.Users.FindAsync(booking.UserId);
            var eventItem = await db.Events.FindAsync(booking.EventId);

            if (eventItem != null && booking.Status == BookingStatus.Confirmed)
                eventItem.CurrentBookings--;

            db.Bookings.Remove(booking);
            await db.SaveChangesAsync();

            try
            {
                if (user != null && eventItem != null)
                    await emailService.SendBookingCancellationEmailAsync(booking, user, eventItem);
            }
            catch (Exception) { }

            return Results.NoContent();
        }).RequireAuthorization(Policies.RequireAdmin);
    }

    private static BookingDto MapToDto(Booking b) => new()
    {
        Id = b.Id,
        UserId = b.UserId,
        EventId = b.EventId,
        BookingTime = b.BookingTime,
        Status = b.Status.ToString(),
        User = new UserDto
        {
            Id = b.User.Id,
            Name = b.User.Name,
            Email = b.User.Email,
            IsAdmin = b.User.IsAdmin,
            IsActive = b.User.IsActive,
            PhoneNumber = b.User.PhoneNumber,
            Bio = b.User.Bio,
            CreatedAt = b.User.CreatedAt
        },
        Event = new EventDto
        {
            Id = b.Event.Id,
            Title = b.Event.Title,
            Description = b.Event.Description,
            StartTime = b.Event.StartTime,
            EndTime = b.Event.EndTime,
            Location = b.Event.Location,
            Price = b.Event.Price,
            MaxCapacity = b.Event.MaxCapacity,
            CurrentBookings = b.Event.CurrentBookings,
            IsActive = b.Event.IsActive,
            IsTour = b.Event.IsTour,
            ImageUrl = b.Event.ImageUrl,
            CreatedAt = b.Event.CreatedAt,
            UpdatedAt = b.Event.UpdatedAt
        }
    };
}

public record CreateBookingRequest(int EventId);
public record UpdateBookingStatusRequest(BookingStatus Status);