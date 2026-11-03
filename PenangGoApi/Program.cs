using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PenangGoApi.Authorization;
using PenangGoApi.Endpoints;
using PenangGoApi.Services;
using PenangGoApp.DataAccess;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "MySQL connection string 'ConnectionStrings:DefaultConnection' is not configured. Set it via environment variable or user secrets.");

builder.Services.AddDbContext<PenangGoContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.MaxDepth = 32;
});

// JWT Authentication
var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "JWT secret key 'JwtSettings:SecretKey' is not configured. Set it via environment variable or user secrets.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "PenangGoApi",
            ValidAudience = "PenangGoApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.RequireUser, policy => policy.RequireAuthenticatedUser())
    .AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole("Admin"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMauiApp", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Rate limiting (login/signup brute-force protection)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("write", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

// Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7000);
});

var app = builder.Build();

// Middleware
app.UseCors("AllowMauiApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PenangGoContext>();
    context.Database.EnsureCreated();
    await SeedData.InitializeAsync(app.Services);
}

// Map endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapHeritageSiteEndpoints();
app.MapEventEndpoints();
app.MapBookingEndpoints();
app.MapBookmarkEndpoints();
app.MapReviewEndpoints();
app.MapPanoramaEndpoints();
app.MapAdminEndpoints();
app.MapSyncEndpoints();
app.MapAuditEndpoints();
app.MapSiteVisitEndpoints();

app.Run();
