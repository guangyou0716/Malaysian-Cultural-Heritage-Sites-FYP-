using Microsoft.Extensions.Logging;
using PenangGoAppFresh.Services;
using Microsoft.EntityFrameworkCore;
using PenangGoApp.DataAccess;

namespace PenangGoAppFresh;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        string connectionString;

#if ANDROID
        connectionString = "server=10.0.2.2;database=penang_go;user=root;password=1234;";
#else
        connectionString = "server=localhost;database=penang_go;user=root;password=1234;";
#endif

        builder.Services.AddDbContext<PenangGoContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            ));


        // HTTP Client for API communication
        builder.Services.AddHttpClient<IApiService, ApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "PenangGoApp/1.0");
        });

        // Register services that will use the API
        builder.Services.AddScoped<PenangGoAppFresh.Auth.IAuthService, PenangGoAppFresh.Auth.AuthService>();
        builder.Services.AddScoped<PenangGoAppFresh.Auth.AuthService>(sp => (PenangGoAppFresh.Auth.AuthService)sp.GetRequiredService<PenangGoAppFresh.Auth.IAuthService>());
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IBookingService, BookingService>();
        builder.Services.AddScoped<IReviewService, ReviewService>();
        builder.Services.AddScoped<PenangGoAppFresh.Services.BookingService>();
        builder.Services.AddScoped<PenangGoAppFresh.Services.ReviewService>();
        builder.Services.AddScoped<PenangGoAppFresh.Services.PanoramaContentService>();
        builder.Services.AddScoped<PenangGoAppFresh.Services.EventService>();
        builder.Services.AddScoped<PenangGoAppFresh.Services.BookmarkService>();
        builder.Services.AddScoped<PenangGoAppFresh.Services.HeritageSiteService>();
        builder.Services.AddScoped<PenangGoAppFresh.Services.OfflineSyncService>();
        builder.Services.AddSingleton<PenangGoAppFresh.Services.NotificationService>();

        // Register API-based booking service for email notifications
        builder.Services.AddScoped<IApiBookingService, ApiBookingService>();
        
        // Register API-based bookmark service
        builder.Services.AddScoped<IApiBookmarkService, ApiBookmarkService>();

        return builder.Build();
    }
}
