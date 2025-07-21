using PenangGoApp.DataAccess;

namespace DatabaseSeeder;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -- seed    - Seed the database");
            Console.WriteLine("  dotnet run -- clear   - Clear all data");
            return;
        }

        var connectionString = "server=localhost;database=penang_go;user=root;password=1234;";

        try
        {
            switch (args[0].ToLower())
            {
                case "seed":
                    Console.WriteLine("🌱 Seeding database...");
                    await PenangGoApp.DataAccess.DatabaseSeeder.SeedAsync(connectionString);
                    break;

                case "clear":
                    Console.WriteLine("🗑️ Clearing database...");
                    await PenangGoApp.DataAccess.DatabaseSeeder.ClearAsync(connectionString);
                    break;

                default:
                    Console.WriteLine($"Unknown command: {args[0]}");
                    Console.WriteLine("Available commands: seed, clear");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
} 