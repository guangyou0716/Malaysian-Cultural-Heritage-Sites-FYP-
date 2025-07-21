using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using System.Text.Json;
using PenangGoApi.DTOs;

// Now all top-level code follows
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<PenangGoApp.DataAccess.PenangGoContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Add CORS for MAUI app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMauiApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Use CORS
app.UseCors("AllowMauiApp");

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PenangGoApp.DataAccess.PenangGoContext>();
    
    // Ensure database is created
    context.Database.EnsureCreated();
    
    // Seed data using traditional EF seeder
    await SeedData.InitializeAsync(app.Services);
}

// Authentication endpoints
app.MapPost("/api/auth/login", async (LoginRequest request, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null || user.IsAccountLocked())
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Invalid credentials or account locked" 
        });
    }

    // Debug output
    Console.WriteLine($"Login attempt for email: {request.Email}");
    Console.WriteLine($"Stored hash: {user.PasswordHash}");
    Console.WriteLine($"Stored salt: {user.PasswordSalt}");
    
    var isValidPassword = PasswordUtility.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
    Console.WriteLine($"Password verification result: {isValidPassword}");
    
    if (!isValidPassword)
    {
        user.IncrementLoginAttempts();
        await db.SaveChangesAsync();
        return Results.BadRequest(new { 
            success = false,
            message = "Invalid credentials" 
        });
    }

    user.UpdateLastLogin();
    user.SessionToken = Guid.NewGuid().ToString();
    user.SessionExpiresAt = DateTime.UtcNow.AddDays(7);
    await db.SaveChangesAsync();

    return Results.Ok(new { 
        success = true,
        message = "Login successful",
        user = new { 
            id = user.Id, 
            name = user.Name, 
            email = user.Email, 
            isAdmin = user.IsAdmin
        },
        sessionToken = user.SessionToken
    });
});

app.MapPost("/api/auth/signup", async (SignupRequest request, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    // Email validation
    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Email is required" 
        });
    }

    // Email format validation using regex
    var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    if (!emailRegex.IsMatch(request.Email))
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Please enter a valid email address" 
        });
    }

    // Check if email already exists
    if (await db.Users.AnyAsync(u => u.Email == request.Email))
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Email already registered" 
        });
    }

    // Name validation
    if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Name must be at least 2 characters long" 
        });
    }

    // Password complexity validation
    if (string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Password is required" 
        });
    }

    if (request.Password.Length < 8)
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Password must be at least 8 characters long" 
        });
    }

    // Check for at least one uppercase letter
    if (!request.Password.Any(char.IsUpper))
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Password must contain at least one uppercase letter" 
        });
    }

    // Check for at least one lowercase letter
    if (!request.Password.Any(char.IsLower))
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Password must contain at least one lowercase letter" 
        });
    }

    // Check for at least one digit
    if (!request.Password.Any(char.IsDigit))
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Password must contain at least one number" 
        });
    }

    // Check for at least one special character
    var specialChars = @"!@#$%^&*()_+-=[]{}|;:,.<>?";
    if (!request.Password.Any(c => specialChars.Contains(c)))
    {
        return Results.BadRequest(new { 
            success = false,
            message = "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)" 
        });
    }

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

    return Results.Created($"/api/users/{user.Id}", new { 
        success = true,
        message = "Registration successful",
        user = new { 
            id = user.Id, 
            name = user.Name, 
            email = user.Email, 
            isAdmin = user.IsAdmin 
        }
    });
});

app.MapPost("/api/auth/logout", async (LogoutRequest request, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.SessionToken == request.SessionToken);
    if (user != null)
    {
        user.SessionToken = null;
        user.SessionExpiresAt = null;
        await db.SaveChangesAsync();
    }
    return Results.Ok(new { message = "Logged out successfully" });
});

// User endpoints
// Refactor GET /api/users endpoint
app.MapGet("/api/users", async (PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Users.Select(u => new UserDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        IsAdmin = u.IsAdmin,
        IsActive = u.IsActive,
        PhoneNumber = u.PhoneNumber,
        Bio = u.Bio,
        ProfilePicture = u.ProfilePicture,
        CreatedAt = u.CreatedAt
    }).ToListAsync());

// Refactor GET /api/users/{id} endpoint
app.MapGet("/api/users/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var u = await db.Users.FindAsync(id);
    if (u == null) return Results.NotFound();
    var dto = new UserDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        IsAdmin = u.IsAdmin,
        IsActive = u.IsActive,
        PhoneNumber = u.PhoneNumber,
        Bio = u.Bio,
        ProfilePicture = u.ProfilePicture,
        CreatedAt = u.CreatedAt
    };
    return Results.Ok(dto);
});

// Refactor GET /api/users/profile endpoint
app.MapGet("/api/users/profile", async (string sessionToken, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var u = await db.Users.FirstOrDefaultAsync(u => u.SessionToken == sessionToken && u.SessionExpiresAt > DateTime.UtcNow);
    if (u == null) return Results.Unauthorized();
    var dto = new UserDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        IsAdmin = u.IsAdmin,
        IsActive = u.IsActive,
        PhoneNumber = u.PhoneNumber,
        Bio = u.Bio,
        ProfilePicture = u.ProfilePicture,
        CreatedAt = u.CreatedAt
    };
    return Results.Ok(dto);
});

app.MapPut("/api/users/{id}", async (int id, PenangGoApp.DataAccess.User inputUser, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();
    
    user.Name = inputUser.Name;
    user.Email = inputUser.Email;
    user.IsAdmin = inputUser.IsAdmin;
    user.IsActive = inputUser.IsActive;
    user.PhoneNumber = inputUser.PhoneNumber;
    user.ProfilePicture = inputUser.ProfilePicture;
    user.Bio = inputUser.Bio;
    user.EmailNotificationsEnabled = inputUser.EmailNotificationsEnabled;
    user.PushNotificationsEnabled = inputUser.PushNotificationsEnabled;
    user.DeviceToken = inputUser.DeviceToken;
    user.PreferredLanguage = inputUser.PreferredLanguage;
    user.TimeZone = inputUser.TimeZone;

    // Allow password change if provided
    if (!string.IsNullOrEmpty(inputUser.PasswordHash) && !string.IsNullOrEmpty(inputUser.PasswordSalt))
    {
        user.PasswordHash = inputUser.PasswordHash;
        user.PasswordSalt = inputUser.PasswordSalt;
    }
    
    await db.SaveChangesAsync();
    return Results.Ok(user);
});

app.MapDelete("/api/users/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Heritage Site endpoints
// Add DTO class at the top, after usings and before any code

// Refactor GET /api/heritagesites endpoint
app.MapGet("/api/heritagesites", async (PenangGoApp.DataAccess.PenangGoContext db) =>
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
            HasPanoramaContent = h.HasPanoramaContent
        }).ToListAsync());

// Refactor GET /api/heritagesites/{id} endpoint
app.MapGet("/api/heritagesites/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var h = await db.HeritageSites.FindAsync(id);
    if (h == null) return Results.NotFound();
    var dto = new HeritageSiteDto
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
        HasPanoramaContent = h.HasPanoramaContent
    };
    return Results.Ok(dto);
});

// Refactor GET /api/heritagesites/with-ar endpoint
app.MapGet("/api/heritagesites/with-ar", async (PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.HeritageSites.Where(h => h.HasPanoramaContent)
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
            HasPanoramaContent = h.HasPanoramaContent
        }).ToListAsync());

// Refactor GET /api/heritagesites/category/{category} endpoint
app.MapGet("/api/heritagesites/category/{category}", async (string category, PenangGoApp.DataAccess.PenangGoContext db) =>
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
            HasPanoramaContent = h.HasPanoramaContent
        }).ToListAsync());

// Refactor POST /api/heritagesites endpoint
app.MapPost("/api/heritagesites", async (PenangGoApp.DataAccess.HeritageSite site, PenangGoApp.DataAccess.PenangGoContext db) =>
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
});

// Refactor PUT /api/heritagesites/{id} endpoint
app.MapPut("/api/heritagesites/{id}", async (int id, PenangGoApp.DataAccess.HeritageSite inputSite, PenangGoApp.DataAccess.PenangGoContext db) =>
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
    site.PhotoGallery = inputSite.PhotoGallery;
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
});

app.MapDelete("/api/heritagesites/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var site = await db.HeritageSites.FindAsync(id);
    if (site is null) return Results.NotFound();
    db.HeritageSites.Remove(site);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Event endpoints
// Refactor GET /api/events endpoint
app.MapGet("/api/events", async (PenangGoApp.DataAccess.PenangGoContext db) =>
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
        Price = (double)(e.Price ?? 0),
        IsActive = e.IsActive,
        IsTour = e.IsTour,
        ImageUrl = e.ImageUrl,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    }).ToListAsync());

// Refactor GET /api/events/{id} endpoint
app.MapGet("/api/events/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var e = await db.Events.FindAsync(id);
    if (e == null) return Results.NotFound();
    var dto = new EventDto
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        StartTime = e.StartTime,
        EndTime = e.EndTime,
        Location = e.Location,
        MaxCapacity = e.MaxCapacity,
        CurrentBookings = e.CurrentBookings,
        Price = (double)(e.Price ?? 0),
        IsActive = e.IsActive,
        IsTour = e.IsTour,
        ImageUrl = e.ImageUrl,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
    return Results.Ok(dto);
});

// Refactor GET /api/events/upcoming endpoint
app.MapGet("/api/events/upcoming", async (PenangGoApp.DataAccess.PenangGoContext db) =>
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
            Price = (double)(e.Price ?? 0),
            IsActive = e.IsActive,
            IsTour = e.IsTour,
            ImageUrl = e.ImageUrl,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        }).ToListAsync());

// Refactor GET /api/events/tours endpoint
app.MapGet("/api/events/tours", async (PenangGoApp.DataAccess.PenangGoContext db) =>
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
            Price = (double)(e.Price ?? 0),
            IsActive = e.IsActive,
            IsTour = e.IsTour,
            ImageUrl = e.ImageUrl,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        }).ToListAsync());

app.MapPost("/api/events", async (PenangGoApp.DataAccess.Event eventItem, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    eventItem.CreatedAt = DateTime.UtcNow;
    db.Events.Add(eventItem);
    await db.SaveChangesAsync();
    return Results.Created($"/api/events/{eventItem.Id}", eventItem);
});

app.MapPut("/api/events/{id}", async (int id, PenangGoApp.DataAccess.Event inputEvent, PenangGoApp.DataAccess.PenangGoContext db) =>
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
});

app.MapDelete("/api/events/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var eventItem = await db.Events.FindAsync(id);
    if (eventItem is null) return Results.NotFound();
    db.Events.Remove(eventItem);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Booking endpoints
// Refactor GET /api/bookings endpoint
app.MapGet("/api/bookings", async (PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Bookings.Select(b => new BookingDto
    {
        Id = b.Id,
        UserId = b.UserId,
        EventId = b.EventId,
        BookingTime = b.BookingTime,
        Status = b.Status.ToString()
    }).ToListAsync());

// Refactor GET /api/bookings/user/{userId} endpoint
app.MapGet("/api/bookings/user/{userId}", async (int userId, PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Bookings.Where(b => b.UserId == userId)
        .Select(b => new BookingDto
        {
            Id = b.Id,
            UserId = b.UserId,
            EventId = b.EventId,
            BookingTime = b.BookingTime,
            Status = b.Status.ToString()
        }).ToListAsync());

// Refactor GET /api/bookings/{id} endpoint
app.MapGet("/api/bookings/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var b = await db.Bookings.FindAsync(id);
    if (b == null) return Results.NotFound();
    var dto = new BookingDto
    {
        Id = b.Id,
        UserId = b.UserId,
        EventId = b.EventId,
        BookingTime = b.BookingTime,
        Status = b.Status.ToString()
    };
    return Results.Ok(dto);
});

app.MapPost("/api/bookings", async (PenangGoApp.DataAccess.Booking booking, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    // Check if event has capacity
    var eventItem = await db.Events.FindAsync(booking.EventId);
    if (eventItem == null) return Results.BadRequest(new { message = "Event not found" });
    
    if (eventItem.CurrentBookings >= eventItem.MaxCapacity)
    {
        return Results.BadRequest(new { message = "Event is full" });
    }
    
    booking.BookingTime = DateTime.UtcNow;
    booking.Status = BookingStatus.Confirmed;
    
    db.Bookings.Add(booking);
    
    // Update event booking count
    eventItem.CurrentBookings++;
    
    await db.SaveChangesAsync();
    return Results.Created($"/api/bookings/{booking.Id}", booking);
});

app.MapPut("/api/bookings/{id}/status", async (int id, BookingStatus status, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var booking = await db.Bookings.FindAsync(id);
    if (booking is null) return Results.NotFound();
    
    booking.Status = status;
    await db.SaveChangesAsync();
    return Results.Ok(booking);
});

app.MapDelete("/api/bookings/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var booking = await db.Bookings.FindAsync(id);
    if (booking is null) return Results.NotFound();
    
    // Update event booking count
    var eventItem = await db.Events.FindAsync(booking.EventId);
    if (eventItem != null && booking.Status == BookingStatus.Confirmed)
    {
        eventItem.CurrentBookings--;
    }
    
    db.Bookings.Remove(booking);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Bookmark endpoints
// Refactor GET /api/bookmarks endpoint
app.MapGet("/api/bookmarks", async (PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Bookmarks.Select(b => new BookmarkDto
    {
        Id = b.Id,
        UserId = b.UserId,
        SiteId = b.SiteId,
        CreatedAt = b.CreatedAt
    }).ToListAsync());

// Refactor GET /api/bookmarks/user/{userId} endpoint
app.MapGet("/api/bookmarks/user/{userId}", async (int userId, PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Bookmarks.Where(b => b.UserId == userId)
        .Select(b => new BookmarkDto
        {
            Id = b.Id,
            UserId = b.UserId,
            SiteId = b.SiteId,
            CreatedAt = b.CreatedAt
        }).ToListAsync());

app.MapPost("/api/bookmarks", async (PenangGoApp.DataAccess.Bookmark bookmark, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    // Check if bookmark already exists
    var existingBookmark = await db.Bookmarks
        .FirstOrDefaultAsync(b => b.UserId == bookmark.UserId && b.SiteId == bookmark.SiteId);
    
    if (existingBookmark != null)
    {
        return Results.BadRequest(new { message = "Bookmark already exists" });
    }
    
    bookmark.CreatedAt = DateTime.UtcNow;
    db.Bookmarks.Add(bookmark);
    
    // Update site bookmark count
    var site = await db.HeritageSites.FindAsync(bookmark.SiteId);
    if (site != null)
    {
        site.TotalBookmarks++;
    }
    
    await db.SaveChangesAsync();
    return Results.Created($"/api/bookmarks/{bookmark.Id}", bookmark);
});

app.MapDelete("/api/bookmarks/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var bookmark = await db.Bookmarks.FindAsync(id);
    if (bookmark is null) return Results.NotFound();
    
    // Update site bookmark count
    var site = await db.HeritageSites.FindAsync(bookmark.SiteId);
    if (site != null)
    {
        site.TotalBookmarks = Math.Max(0, site.TotalBookmarks - 1);
    }
    
    db.Bookmarks.Remove(bookmark);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Review endpoints
// Refactor GET /api/reviews endpoint
app.MapGet("/api/reviews", async (PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Reviews.Select(r => new ReviewDto
    {
        Id = r.Id,
        UserId = r.UserId,
        SiteId = r.SiteId,
        Rating = r.Rating,
        Comment = r.Comment,
        CreatedAt = r.CreatedAt
    }).ToListAsync());

// Refactor GET /api/reviews/site/{siteId} endpoint
app.MapGet("/api/reviews/site/{siteId}", async (int siteId, PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Reviews.Where(r => r.SiteId == siteId)
        .Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            SiteId = r.SiteId,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToListAsync());

// Refactor GET /api/reviews/user/{userId} endpoint
app.MapGet("/api/reviews/user/{userId}", async (int userId, PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Reviews.Where(r => r.UserId == userId)
        .Select(r => new ReviewDto
        {
            Id = r.Id,
            UserId = r.UserId,
            SiteId = r.SiteId,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        }).ToListAsync());

app.MapPost("/api/reviews", async (PenangGoApp.DataAccess.Review review, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    // Check if user already reviewed this site
    var existingReview = await db.Reviews
        .FirstOrDefaultAsync(r => r.UserId == review.UserId && r.SiteId == review.SiteId);
    
    if (existingReview != null)
    {
        return Results.BadRequest(new { message = "You have already reviewed this site" });
    }
    
    review.CreatedAt = DateTime.UtcNow;
    db.Reviews.Add(review);
    
    // Update site rating statistics
    var site = await db.HeritageSites.FindAsync(review.SiteId);
    if (site != null)
    {
        var allReviews = await db.Reviews.Where(r => r.SiteId == review.SiteId).ToListAsync();
        site.TotalReviews = allReviews.Count;
        site.AverageRating = allReviews.Average(r => r.Rating);
    }
    
    await db.SaveChangesAsync();
    return Results.Created($"/api/reviews/{review.Id}", review);
});

app.MapPut("/api/reviews/{id}", async (int id, PenangGoApp.DataAccess.Review inputReview, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var review = await db.Reviews.FindAsync(id);
    if (review is null) return Results.NotFound();
    
    review.Rating = inputReview.Rating;
    review.Comment = inputReview.Comment;
    
    // Update site rating statistics
    var site = await db.HeritageSites.FindAsync(review.SiteId);
    if (site != null)
    {
        var allReviews = await db.Reviews.Where(r => r.SiteId == review.SiteId).ToListAsync();
        site.AverageRating = allReviews.Average(r => r.Rating);
    }
    
    await db.SaveChangesAsync();
    return Results.Ok(review);
});

app.MapDelete("/api/reviews/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var review = await db.Reviews.FindAsync(id);
    if (review is null) return Results.NotFound();
    
    // Update site rating statistics
    var site = await db.HeritageSites.FindAsync(review.SiteId);
    if (site != null)
    {
        var allReviews = await db.Reviews.Where(r => r.SiteId == review.SiteId && r.Id != id).ToListAsync();
        site.TotalReviews = allReviews.Count;
        site.AverageRating = allReviews.Count > 0 ? allReviews.Average(r => r.Rating) : 0;
    }
    
    db.Reviews.Remove(review);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Refactor GET /api/arcontent endpoint
app.MapGet("/api/arcontent", async (PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.PanoramaContents
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

// Refactor GET /api/arcontent/site/{siteId} endpoint
app.MapGet("/api/arcontent/site/{siteId}", async (int siteId, PenangGoApp.DataAccess.PenangGoContext db) =>
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

// Refactor GET /api/arcontent/{id} endpoint
app.MapGet("/api/arcontent/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var p = await db.PanoramaContents.FindAsync(id);
    if (p == null) return Results.NotFound();
    var dto = new PanoramaContentDto
    {
        Id = p.Id,
        Title = p.Title,
        Description = p.Description,
        PanoramaUrl = p.PanoramaUrl,
        HeritageSiteId = p.HeritageSiteId ?? 0,
        UploadedBy = p.UploadedBy,
        UploadedAt = p.UploadedAt
    };
    return Results.Ok(dto);
});

// Refactor POST /api/arcontent endpoint
app.MapPost("/api/arcontent", async (PenangGoApp.DataAccess.PanoramaContent panoramaContent, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    panoramaContent.UploadedAt = DateTime.UtcNow;
    db.PanoramaContents.Add(panoramaContent);
    
    // Update heritage site AR content flag
    var site = await db.HeritageSites.FindAsync(panoramaContent.HeritageSiteId);
    if (site != null)
    {
        site.HasPanoramaContent = true;
    }
    
    await db.SaveChangesAsync();
    return Results.Created($"/api/arcontent/{panoramaContent.Id}", new PanoramaContentDto
    {
        Id = panoramaContent.Id,
        Title = panoramaContent.Title,
        Description = panoramaContent.Description,
        PanoramaUrl = panoramaContent.PanoramaUrl,
        HeritageSiteId = panoramaContent.HeritageSiteId ?? 0,
        UploadedBy = panoramaContent.UploadedBy,
        UploadedAt = panoramaContent.UploadedAt
    });
});

// Refactor PUT /api/arcontent/{id} endpoint
app.MapPut("/api/arcontent/{id}", async (int id, PenangGoApp.DataAccess.PanoramaContent inputPanoramaContent, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var panoramaContent = await db.PanoramaContents.FindAsync(id);
    if (panoramaContent is null) return Results.NotFound();
    
    panoramaContent.Title = inputPanoramaContent.Title;
    panoramaContent.Description = inputPanoramaContent.Description;
    panoramaContent.PanoramaUrl = inputPanoramaContent.PanoramaUrl;
    panoramaContent.HeritageSiteId = inputPanoramaContent.HeritageSiteId;
    
    await db.SaveChangesAsync();

    // Ensure HasPanoramaContent is true after update
    var site = await db.HeritageSites.FindAsync(panoramaContent.HeritageSiteId);
    if (site != null)
    {
        site.HasPanoramaContent = true;
        await db.SaveChangesAsync();
    }

    return Results.Ok(new PanoramaContentDto
    {
        Id = panoramaContent.Id,
        Title = panoramaContent.Title,
        Description = panoramaContent.Description,
        PanoramaUrl = panoramaContent.PanoramaUrl,
        HeritageSiteId = panoramaContent.HeritageSiteId ?? 0,
        UploadedBy = panoramaContent.UploadedBy,
        UploadedAt = panoramaContent.UploadedAt
    });
});

app.MapDelete("/api/arcontent/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var panoramaContent = await db.PanoramaContents.FindAsync(id);
    if (panoramaContent is null) return Results.NotFound();
    var siteId = panoramaContent.HeritageSiteId;
    db.PanoramaContents.Remove(panoramaContent);
    await db.SaveChangesAsync();

    // Update HasPanoramaContent on the site
    var site = await db.HeritageSites.FindAsync(siteId);
    if (site != null)
    {
        var hasAny = await db.PanoramaContents.AnyAsync(p => p.HeritageSiteId == site.Id);
        site.HasPanoramaContent = hasAny;
        await db.SaveChangesAsync();
    }

    return Results.NoContent();
});

// Analytics endpoints (Admin only)
app.MapGet("/api/analytics/overview", async (PenangGoApp.DataAccess.PenangGoContext db) =>
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

app.MapGet("/api/analytics/popular-sites", async (PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var popularSites = await db.HeritageSites
        .OrderByDescending(s => s.TotalBookmarks + s.TotalReviews)
        .Take(10)
        .Select(s => new { s.Id, s.Name, s.TotalBookmarks, s.TotalReviews, s.AverageRating })
        .ToListAsync();
    
    return Results.Ok(popularSites);
});

// Offline sync endpoints
app.MapPost("/api/sync/upload", async (SyncDataRequest request, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.SessionToken == request.SessionToken);
    if (user == null) return Results.Unauthorized();
    
    // Process offline data (bookmarks, reviews, etc.)
    if (request.OfflineData != null)
    {
        user.OfflineData = request.OfflineData;
        user.LastSyncAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
    
    return Results.Ok(new { message = "Sync completed" });
});

app.MapGet("/api/sync/download", async (string sessionToken, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.SessionToken == sessionToken);
    if (user == null) return Results.Unauthorized();
    
    // Return data for offline use
    var sites = await db.HeritageSites.Where(s => s.IsActive).ToListAsync();
    var events = await db.Events.Where(e => e.IsActive && e.StartTime > DateTime.UtcNow).ToListAsync();
    var userBookmarks = await db.Bookmarks.Where(b => b.UserId == user.Id).Include(b => b.Site).ToListAsync();
    var userReviews = await db.Reviews.Where(r => r.UserId == user.Id).Include(r => r.Site).ToListAsync();
    
    return Results.Ok(new
    {
        sites,
        events,
        userBookmarks,
        userReviews,
        lastSync = DateTime.UtcNow
    });
});

app.Run();

// Request/Response models
record LoginRequest(string Email, string Password);
record SignupRequest(string Name, string Email, string Password);
record LogoutRequest(string SessionToken);
record SyncDataRequest(string SessionToken, string? OfflineData);