using Microsoft.Extensions.Logging;
using PenangGoApp;
using PenangGoApp.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.Maui.Storage;

namespace PenangGoApp;

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
		builder.Logging.AddDebug();
#endif

#if ANDROID
		builder.Services.AddSingleton<IARService, Platforms.Android.ARService>();
#elif IOS
		builder.Services.AddSingleton<IARService, Platforms.iOS.ARService>();
#else
		builder.Services.AddSingleton<IARService, Platforms.DefaultARService>();
#endif

		builder.Services.AddScoped<PenangGoApp.Services.AuthService>();
		builder.Services.AddScoped<PenangGoApp.Services.ARContentService>();
		builder.Services.AddScoped<PenangGoApp.Services.EventService>();
		builder.Services.AddScoped<PenangGoApp.Services.BookingService>();
		builder.Services.AddScoped<PenangGoApp.Services.BookmarkService>();
		builder.Services.AddScoped<PenangGoApp.Services.ReviewService>();
		builder.Services.AddScoped<PenangGoApp.Services.HeritageSiteService>();
		builder.Services.AddScoped<PenangGoApp.Services.OfflineSyncService>();
		builder.Services.AddSingleton<PenangGoApp.Services.NotificationService>();

		var connectionString = "server=localhost;database=penang_go;user=root;password=1234;";
		builder.Services.AddDbContext<PenangGoContext>(options =>
			options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

		var offlineDbPath = Path.Combine(FileSystem.AppDataDirectory, "penanggo_offline.db");
		builder.Services.AddDbContext<PenangGoApp.Data.PenangGoOfflineContext>(options =>
			options.UseSqlite($"Filename={offlineDbPath}"));

		return builder.Build();
	}
}
