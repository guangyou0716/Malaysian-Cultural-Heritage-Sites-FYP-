using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;
using System.Text.Json;
using PenangGoApi.DTOs;
using PenangGoApi.Services;

// Now all top-level code follows
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<PenangGoApp.DataAccess.PenangGoContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Configure JSON serialization to handle circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.MaxDepth = 32;
});

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

// Register email service
builder.Services.AddScoped<IEmailService, EmailService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7000); // HTTP
    // Or for HTTPS:
    // options.ListenAnyIP(7001, listenOptions => listenOptions.UseHttps());
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
    user.Bio = inputUser.Bio;
    user.EmailNotificationsEnabled = inputUser.EmailNotificationsEnabled;
    user.PushNotificationsEnabled = inputUser.PushNotificationsEnabled;

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
            HasPanoramaContent = h.HasPanoramaContent,
            ApprovalStatus = h.ApprovalStatus,
            SubmittedAt = h.SubmittedAt,
            ApprovedAt = h.ApprovedAt,
            ApprovalNotes = h.ApprovalNotes
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
        HasPanoramaContent = h.HasPanoramaContent,
        ApprovalStatus = h.ApprovalStatus,
        SubmittedAt = h.SubmittedAt,
        ApprovedAt = h.ApprovedAt,
        ApprovalNotes = h.ApprovalNotes
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
            HasPanoramaContent = h.HasPanoramaContent,
            ApprovalStatus = h.ApprovalStatus,
            SubmittedAt = h.SubmittedAt,
            ApprovedAt = h.ApprovedAt,
            ApprovalNotes = h.ApprovalNotes
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
            HasPanoramaContent = h.HasPanoramaContent,
            ApprovalStatus = h.ApprovalStatus,
            SubmittedAt = h.SubmittedAt,
            ApprovedAt = h.ApprovedAt,
            ApprovalNotes = h.ApprovalNotes
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
    var site = await db.HeritageSites
        .Include(s => s.Bookmarks)
        .Include(s => s.Reviews)
        .Include(s => s.PanoramaContents)
        .FirstOrDefaultAsync(s => s.Id == id);
    
    if (site is null) return Results.NotFound();
    
    // Remove related records first
    db.Bookmarks.RemoveRange(site.Bookmarks);
    db.Reviews.RemoveRange(site.Reviews);
    db.PanoramaContents.RemoveRange(site.PanoramaContents);
    
    // Then remove the site
    db.HeritageSites.Remove(site);
    
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Heritage site approval endpoints
app.MapPut("/api/heritagesites/{id}/approve", async (int id, PenangGoApp.DataAccess.PenangGoContext db, IEmailService emailService) =>
{
    var site = await db.HeritageSites
        .Include(s => s.Submitter)
        .FirstOrDefaultAsync(s => s.Id == id);
    if (site is null) return Results.NotFound();
    
    site.ApprovalStatus = ApprovalStatus.Approved;
    site.IsActive = true;
    site.ApprovedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    
    // Send approval email to the submitter
    if (site.Submitter != null)
    {
        await emailService.SendSiteApprovalEmailAsync(site.Submitter, site);
    }
    
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
});

app.MapPut("/api/heritagesites/{id}/reject", async (int id, RejectSiteRequest request, PenangGoApp.DataAccess.PenangGoContext db, IEmailService emailService) =>
{
    var site = await db.HeritageSites
        .Include(s => s.Submitter)
        .FirstOrDefaultAsync(s => s.Id == id);
    if (site is null) return Results.NotFound();
    
    site.ApprovalStatus = ApprovalStatus.Rejected;
    site.IsActive = false;
    site.ApprovedAt = DateTime.UtcNow;
    site.ApprovalNotes = request.Reason;
    
    await db.SaveChangesAsync();
    
    // Send rejection email to the submitter
    if (site.Submitter != null)
    {
        await emailService.SendSiteRejectionEmailAsync(site.Submitter, site, request.Reason);
    }
    
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
        Price = e.Price,
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
        Price = e.Price,
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
            Price = e.Price,
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
            Price = e.Price,
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
    await db.Bookings.Include(b => b.User).Include(b => b.Event)
        .Select(b => new BookingDto
        {
            Id = b.Id,
            UserId = b.UserId,
            EventId = b.EventId,
            BookingTime = b.BookingTime,
            Status = b.Status.ToString(),
            User = new PenangGoApi.DTOs.UserDto
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
            Event = new PenangGoApi.DTOs.EventDto
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
        }).ToListAsync());

// Refactor GET /api/bookings/user/{userId} endpoint
app.MapGet("/api/bookings/user/{userId}", async (int userId, PenangGoApp.DataAccess.PenangGoContext db) =>
    await db.Bookings.Where(b => b.UserId == userId)
        .Include(b => b.User).Include(b => b.Event)
        .Select(b => new BookingDto
        {
            Id = b.Id,
            UserId = b.UserId,
            EventId = b.EventId,
            BookingTime = b.BookingTime,
            Status = b.Status.ToString(),
            User = new PenangGoApi.DTOs.UserDto
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
            Event = new PenangGoApi.DTOs.EventDto
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
        }).ToListAsync());

// Refactor GET /api/bookings/{id} endpoint
app.MapGet("/api/bookings/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var b = await db.Bookings.Include(b => b.User).Include(b => b.Event).FirstOrDefaultAsync(b => b.Id == id);
    if (b == null) return Results.NotFound();
    var dto = new BookingDto
    {
        Id = b.Id,
        UserId = b.UserId,
        EventId = b.EventId,
        BookingTime = b.BookingTime,
        Status = b.Status.ToString(),
        User = new PenangGoApi.DTOs.UserDto
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
        Event = new PenangGoApi.DTOs.EventDto
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
    return Results.Ok(dto);
});

app.MapPost("/api/bookings", async (PenangGoApp.DataAccess.Booking booking, PenangGoApp.DataAccess.PenangGoContext db, IEmailService emailService) =>
{
    Console.WriteLine($"API: Received booking request - UserId: {booking.UserId}, EventId: {booking.EventId}");
    
    // Check if event has capacity
    var eventItem = await db.Events.FindAsync(booking.EventId);
    if (eventItem == null) 
    {
        Console.WriteLine($"API: Event not found - EventId: {booking.EventId}");
        return Results.BadRequest(new { message = "Event not found" });
    }
    
    Console.WriteLine($"API: Event found - Title: {eventItem.Title}, CurrentBookings: {eventItem.CurrentBookings}, MaxCapacity: {eventItem.MaxCapacity}");
    
    if (eventItem.CurrentBookings >= eventItem.MaxCapacity)
    {
        Console.WriteLine($"API: Event is full - CurrentBookings: {eventItem.CurrentBookings}, MaxCapacity: {eventItem.MaxCapacity}");
        return Results.BadRequest(new { message = "Event is full" });
    }
    
    booking.BookingTime = DateTime.UtcNow;
    booking.Status = BookingStatus.Confirmed;
    
    Console.WriteLine($"API: Adding booking to database");
    db.Bookings.Add(booking);
    
    // Update event booking count
    eventItem.CurrentBookings++;
    Console.WriteLine($"API: Updated event booking count to {eventItem.CurrentBookings}");
    
    await db.SaveChangesAsync();
    Console.WriteLine($"API: Database changes saved");
    
    // Reload the booking with its generated ID
    await db.Entry(booking).ReloadAsync();
    Console.WriteLine($"API: Booking reloaded - ID: {booking.Id}");
    
    // Send confirmation email
    try
    {
        var user = await db.Users.FindAsync(booking.UserId);
        if (user != null)
        {
            Console.WriteLine($"API: Sending confirmation email to user: {user.Email}");
            await emailService.SendBookingConfirmationEmailAsync(booking, user, eventItem);
        }
        else
        {
            Console.WriteLine($"API: User not found for ID: {booking.UserId}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"API: Failed to send confirmation email: {ex.Message}");
        // Don't fail the booking if email fails
    }
    
    // Return the created booking with DTO
    Console.WriteLine($"API: Retrieving created booking with ID: {booking.Id}");
    var createdBooking = await db.Bookings.Include(b => b.User).Include(b => b.Event)
        .FirstOrDefaultAsync(b => b.Id == booking.Id);
    if (createdBooking == null) 
    {
        Console.WriteLine($"API: Created booking not found in database");
        return Results.NotFound();
    }
    
    Console.WriteLine($"API: Successfully retrieved booking - ID: {createdBooking.Id}");
    
    Console.WriteLine($"API: Creating DTO for booking ID: {createdBooking.Id}");
    var dto = new BookingDto
    {
        Id = createdBooking.Id,
        UserId = createdBooking.UserId,
        EventId = createdBooking.EventId,
        BookingTime = createdBooking.BookingTime,
        Status = createdBooking.Status.ToString(),
        User = new PenangGoApi.DTOs.UserDto
        {
            Id = createdBooking.User.Id,
            Name = createdBooking.User.Name,
            Email = createdBooking.User.Email,
            IsAdmin = createdBooking.User.IsAdmin,
            IsActive = createdBooking.User.IsActive,
            PhoneNumber = createdBooking.User.PhoneNumber,
            Bio = createdBooking.User.Bio,
            CreatedAt = createdBooking.User.CreatedAt
        },
        Event = new PenangGoApi.DTOs.EventDto
        {
            Id = createdBooking.Event.Id,
            Title = createdBooking.Event.Title,
            Description = createdBooking.Event.Description,
            StartTime = createdBooking.Event.StartTime,
            EndTime = createdBooking.Event.EndTime,
            Location = createdBooking.Event.Location,
            Price = createdBooking.Event.Price,
            MaxCapacity = createdBooking.Event.MaxCapacity,
            CurrentBookings = createdBooking.Event.CurrentBookings,
            IsActive = createdBooking.Event.IsActive,
            IsTour = createdBooking.Event.IsTour,
            ImageUrl = createdBooking.Event.ImageUrl,
            CreatedAt = createdBooking.Event.CreatedAt,
            UpdatedAt = createdBooking.Event.UpdatedAt
        }
    };
    Console.WriteLine($"API: DTO created successfully - returning Created response");
    return Results.Created($"/api/bookings/{booking.Id}", dto);
});

app.MapPut("/api/bookings/{id}", async (int id, PenangGoApp.DataAccess.Booking booking, PenangGoApp.DataAccess.PenangGoContext db) =>
{
    var existingBooking = await db.Bookings.FindAsync(id);
    if (existingBooking == null) return Results.NotFound();
    
    existingBooking.Status = booking.Status;
    existingBooking.BookingTime = booking.BookingTime;
    
    await db.SaveChangesAsync();
    
    // Return the updated booking with DTO
    var updatedBooking = await db.Bookings.Include(b => b.User).Include(b => b.Event)
        .FirstOrDefaultAsync(b => b.Id == id);
    if (updatedBooking == null) return Results.NotFound();
    
    var dto = new BookingDto
    {
        Id = updatedBooking.Id,
        UserId = updatedBooking.UserId,
        EventId = updatedBooking.EventId,
        BookingTime = updatedBooking.BookingTime,
        Status = updatedBooking.Status.ToString(),
        User = new PenangGoApi.DTOs.UserDto
        {
            Id = updatedBooking.User.Id,
            Name = updatedBooking.User.Name,
            Email = updatedBooking.User.Email,
            IsAdmin = updatedBooking.User.IsAdmin,
            IsActive = updatedBooking.User.IsActive,
            PhoneNumber = updatedBooking.User.PhoneNumber,
            Bio = updatedBooking.User.Bio,
            CreatedAt = updatedBooking.User.CreatedAt
        },
        Event = new PenangGoApi.DTOs.EventDto
        {
            Id = updatedBooking.Event.Id,
            Title = updatedBooking.Event.Title,
            Description = updatedBooking.Event.Description,
            StartTime = updatedBooking.Event.StartTime,
            EndTime = updatedBooking.Event.EndTime,
            Location = updatedBooking.Event.Location,
            Price = updatedBooking.Event.Price,
            MaxCapacity = updatedBooking.Event.MaxCapacity,
            CurrentBookings = updatedBooking.Event.CurrentBookings,
            IsActive = updatedBooking.Event.IsActive,
            IsTour = updatedBooking.Event.IsTour,
            ImageUrl = updatedBooking.Event.ImageUrl,
            CreatedAt = updatedBooking.Event.CreatedAt,
            UpdatedAt = updatedBooking.Event.UpdatedAt
        }
    };
    return Results.Ok(dto);
});

app.MapPut("/api/bookings/{id}/status", async (int id, UpdateBookingStatusRequest req, PenangGoApp.DataAccess.PenangGoContext db, IEmailService emailService) =>
{
    var booking = await db.Bookings.FindAsync(id);
    if (booking is null) return Results.NotFound();
    
    var originalStatus = booking.Status;
    booking.Status = req.Status;
    await db.SaveChangesAsync();
    
    // Send status update email
    try
    {
        var user = await db.Users.FindAsync(booking.UserId);
        var eventItem = await db.Events.FindAsync(booking.EventId);
        if (user != null && eventItem != null)
        {
            await emailService.SendBookingStatusUpdateEmailAsync(booking, user, eventItem, req.Status);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send status update email: {ex.Message}");
        // Don't fail the status update if email fails
    }
    
    // Return the updated booking with DTO
    var updatedBooking = await db.Bookings.Include(b => b.User).Include(b => b.Event)
        .FirstOrDefaultAsync(b => b.Id == id);
    if (updatedBooking == null) return Results.NotFound();
    
    var dto = new BookingDto
    {
        Id = updatedBooking.Id,
        UserId = updatedBooking.UserId,
        EventId = updatedBooking.EventId,
        BookingTime = updatedBooking.BookingTime,
        Status = updatedBooking.Status.ToString(),
        User = new PenangGoApi.DTOs.UserDto
        {
            Id = updatedBooking.User.Id,
            Name = updatedBooking.User.Name,
            Email = updatedBooking.User.Email,
            IsAdmin = updatedBooking.User.IsAdmin,
            IsActive = updatedBooking.User.IsActive,
            PhoneNumber = updatedBooking.User.PhoneNumber,
            Bio = updatedBooking.User.Bio,
            CreatedAt = updatedBooking.User.CreatedAt
        },
        Event = new PenangGoApi.DTOs.EventDto
        {
            Id = updatedBooking.Event.Id,
            Title = updatedBooking.Event.Title,
            Description = updatedBooking.Event.Description,
            StartTime = updatedBooking.Event.StartTime,
            EndTime = updatedBooking.Event.EndTime,
            Location = updatedBooking.Event.Location,
            Price = updatedBooking.Event.Price,
            MaxCapacity = updatedBooking.Event.MaxCapacity,
            CurrentBookings = updatedBooking.Event.CurrentBookings,
            IsActive = updatedBooking.Event.IsActive,
            IsTour = updatedBooking.Event.IsTour,
            ImageUrl = updatedBooking.Event.ImageUrl,
            CreatedAt = updatedBooking.Event.CreatedAt,
            UpdatedAt = updatedBooking.Event.UpdatedAt
        }
    };
    return Results.Ok(dto);
});

app.MapDelete("/api/bookings/{id}", async (int id, PenangGoApp.DataAccess.PenangGoContext db, IEmailService emailService) =>
{
    var booking = await db.Bookings.FindAsync(id);
    if (booking is null) return Results.NotFound();
    
    // Store booking info before deletion for email
    var user = await db.Users.FindAsync(booking.UserId);
    var eventItem = await db.Events.FindAsync(booking.EventId);
    
    // Update event booking count
    if (eventItem != null && booking.Status == BookingStatus.Confirmed)
    {
        eventItem.CurrentBookings--;
    }
    
    db.Bookings.Remove(booking);
    await db.SaveChangesAsync();
    
    // Send cancellation email
    try
    {
        if (user != null && eventItem != null)
        {
            await emailService.SendBookingCancellationEmailAsync(booking, user, eventItem);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send cancellation email: {ex.Message}");
        // Don't fail the deletion if email fails
    }
    
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
{
    var bookmarks = await db.Bookmarks
        .Where(b => b.UserId == userId)
        .Select(b => new { b.Id, b.UserId, b.SiteId, b.CreatedAt })
        .ToListAsync();
    
    var siteIds = bookmarks.Select(b => b.SiteId).Distinct().ToList();
    var sites = await db.HeritageSites
        .Where(s => siteIds.Contains(s.Id))
        .Select(s => new { s.Id, s.Name, s.Description, s.Location, s.ImageUrl, s.AverageRating, s.TotalReviews, s.IsActive, s.Category, s.HistoricalPeriod, s.HasPanoramaContent, s.ApprovalStatus, s.SubmittedAt, s.ApprovedAt, s.ApprovalNotes })
        .ToListAsync();
    
    var siteDict = sites.ToDictionary(s => s.Id);
    
    return bookmarks.Select(b => new BookmarkDto
    {
        Id = b.Id,
        UserId = b.UserId,
        SiteId = b.SiteId,
        CreatedAt = b.CreatedAt,
        Site = siteDict.ContainsKey(b.SiteId) ? new PenangGoApi.DTOs.HeritageSiteDto
        {
            Id = siteDict[b.SiteId].Id,
            Name = siteDict[b.SiteId].Name,
            Description = siteDict[b.SiteId].Description,
            Location = siteDict[b.SiteId].Location,
            ImageUrl = siteDict[b.SiteId].ImageUrl,
            AverageRating = siteDict[b.SiteId].AverageRating,
            TotalReviews = siteDict[b.SiteId].TotalReviews,
            IsActive = siteDict[b.SiteId].IsActive,
            Category = siteDict[b.SiteId].Category,
            HistoricalPeriod = siteDict[b.SiteId].HistoricalPeriod,
            HasPanoramaContent = siteDict[b.SiteId].HasPanoramaContent,
            ApprovalStatus = siteDict[b.SiteId].ApprovalStatus,
            SubmittedAt = siteDict[b.SiteId].SubmittedAt,
            ApprovedAt = siteDict[b.SiteId].ApprovedAt,
            ApprovalNotes = siteDict[b.SiteId].ApprovalNotes
        } : null
    }).ToList();
});

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
public record UpdateBookingStatusRequest(BookingStatus Status);
public record RejectSiteRequest(string Reason);