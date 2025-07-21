using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PenangGoApp.DataAccess;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(string connectionString)
    {
        var services = new ServiceCollection();
        
        // Configure services
        services.AddDbContext<PenangGoContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            await SeedData.InitializeAsync(serviceProvider);
            Console.WriteLine("✅ Database seeded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding database: {ex.Message}");
            throw;
        }
    }

    public static async Task ClearAsync(string connectionString)
    {
        var services = new ServiceCollection();
        
        // Configure services
        services.AddDbContext<PenangGoContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PenangGoContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<PenangGoContext>>();

            logger.LogInformation("Clearing all data...");
            
            context.Bookings.RemoveRange(context.Bookings);
            context.Reviews.RemoveRange(context.Reviews);
            context.Bookmarks.RemoveRange(context.Bookmarks);
            context.PanoramaContents.RemoveRange(context.PanoramaContents);
            context.Events.RemoveRange(context.Events);
            context.HeritageSites.RemoveRange(context.HeritageSites);
            context.Users.RemoveRange(context.Users);
            
            await context.SaveChangesAsync();
            logger.LogInformation("All data cleared successfully!");
            Console.WriteLine("✅ Database cleared successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error clearing database: {ex.Message}");
            throw;
        }
    }
} 