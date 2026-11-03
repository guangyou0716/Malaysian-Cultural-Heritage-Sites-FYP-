using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using PenangGoApp.DataAccess;
using PenangGoAppFresh.Auth;
using PenangGoAppFresh.Services;

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

        // Configuration: appsettings.json (optional) + environment variables
        var environmentVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry variable in Environment.GetEnvironmentVariables())
        {
            environmentVariables[((string)variable.Key).Replace("__", ":")] = variable.Value?.ToString();
        }
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddInMemoryCollection(environmentVariables)
            .Build();
        builder.Configuration.AddConfiguration(configuration);

        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
#if ANDROID
            ?? "http://10.0.2.2:7000";
#else
            ?? "http://localhost:7000";
#endif

        builder.Services.Configure<ApiOptions>(options => options.BaseUrl = apiBaseUrl);

        builder.Services.AddSingleton<ITokenStore, TokenStore>();
        builder.Services.AddTransient<AuthHeaderHandler>();

        // Local offline cache/outbox (SQLite). Not a source of truth — the API is.
        var offlineDbPath = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "penanggo_offline.db");
        builder.Services.AddDbContext<PenangGoOfflineContext>(options =>
            options.UseSqlite($"Data Source={offlineDbPath}"));

        builder.Services.AddHttpClient("PenangGo", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "PenangGoApp/1.0");
        }).AddHttpMessageHandler<AuthHeaderHandler>();

        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("PenangGo"));

        builder.Services.AddScoped<IApiService, ApiService>();
        builder.Services.AddScoped<ApiService>(sp => (ApiService)sp.GetRequiredService<IApiService>());
        builder.Services.AddScoped<PenangGoAppFresh.Auth.IAuthService, PenangGoAppFresh.Auth.AuthService>();
        builder.Services.AddScoped<PenangGoAppFresh.Auth.AuthService>(sp => (PenangGoAppFresh.Auth.AuthService)sp.GetRequiredService<PenangGoAppFresh.Auth.IAuthService>());
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<UserService>(sp => (UserService)sp.GetRequiredService<IUserService>());
        builder.Services.AddScoped<IBookingService, BookingService>();
        builder.Services.AddScoped<BookingService>(sp => (BookingService)sp.GetRequiredService<IBookingService>());
        builder.Services.AddScoped<IReviewService, ReviewService>();
        builder.Services.AddScoped<ReviewService>(sp => (ReviewService)sp.GetRequiredService<IReviewService>());
        builder.Services.AddScoped<PanoramaContentService>();
        builder.Services.AddScoped<EventService>();
        builder.Services.AddScoped<BookmarkService>();
        builder.Services.AddScoped<HeritageSiteService>();
        builder.Services.AddScoped<OfflineSyncService>();
        builder.Services.AddSingleton<NotificationService>();

        return builder.Build();
    }
}
