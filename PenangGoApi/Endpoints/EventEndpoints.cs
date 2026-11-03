using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using PenangGoApi.DTOs;
using PenangGoApi.Authorization;

namespace PenangGoApi.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events").WithTags("Events");

        group.MapGet("/", async (PenangGoContext db) =>
            await db.Events.Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Location = e.Location,
                MaxCapacity = e.MaxCapacity,
                CurrentBookings = e.CurrentBookings,
                Price = e.Price,
                IsActive = e.IsActive,
                IsTour = e.IsTour,
                ImageUrl = e.ImageUrl,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToListAsync());

        group.MapGet("/{id}", async (int id, PenangGoContext db) =>
        {
            var e = await db.Events.FindAsync(id);
            if (e == null) return Results.NotFound();
            return Results.Ok(new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Location = e.Location,
                MaxCapacity = e.MaxCapacity,
                CurrentBookings = e.CurrentBookings,
                Price = e.Price,
                IsActive = e.IsActive,
                IsTour = e.IsTour,
                ImageUrl = e.ImageUrl,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            });
        });

        group.MapGet("/upcoming", async (PenangGoContext db) =>
            await db.Events.Where(e => e.StartTime > DateTime.UtcNow && e.IsActive)
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Location = e.Location,
                    MaxCapacity = e.MaxCapacity,
                    CurrentBookings = e.CurrentBookings,
                    Price = e.Price,
                    IsActive = e.IsActive,
                    IsTour = e.IsTour,
                    ImageUrl = e.ImageUrl,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                }).ToListAsync());

        group.MapGet("/tours", async (PenangGoContext db) =>
            await db.Events.Where(e => e.IsTour && e.IsActive)
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Location = e.Location,
                    MaxCapacity = e.MaxCapacity,
                    CurrentBookings = e.CurrentBookings,
                    Price = e.Price,
                    IsActive = e.IsActive,
                    IsTour = e.IsTour,
                    ImageUrl = e.ImageUrl,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                }).ToListAsync());

        group.MapPost("/", async (Event eventItem, PenangGoContext db) =>
        {
            eventItem.CreatedAt = DateTime.UtcNow;
            db.Events.Add(eventItem);
            await db.SaveChangesAsync();
            return Results.Created($"/api/events/{eventItem.Id}", eventItem);
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapPut("/{id}", async (int id, Event inputEvent, PenangGoContext db) =>
        {
            var eventItem = await db.Events.FindAsync(id);
            if (eventItem is null) return Results.NotFound();

            eventItem.Title = inputEvent.Title;
            eventItem.Description = inputEvent.Description;
            eventItem.StartTime = inputEvent.StartTime;
            eventItem.EndTime = inputEvent.EndTime;
            eventItem.Location = inputEvent.Location;
            eventItem.MaxCapacity = inputEvent.MaxCapacity;
            eventItem.CurrentBookings = inputEvent.CurrentBookings;
            eventItem.Price = inputEvent.Price;
            eventItem.IsActive = inputEvent.IsActive;
            eventItem.IsTour = inputEvent.IsTour;
            eventItem.ImageUrl = inputEvent.ImageUrl;
            eventItem.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(eventItem);
        }).RequireAuthorization(Policies.RequireAdmin);

        group.MapDelete("/{id}", async (int id, PenangGoContext db) =>
        {
            var eventItem = await db.Events.FindAsync(id);
            if (eventItem is null) return Results.NotFound();
            db.Events.Remove(eventItem);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization(Policies.RequireAdmin);
    }
}
