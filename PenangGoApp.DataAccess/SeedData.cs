using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PenangGoApp.DataAccess;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PenangGoContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PenangGoContext>>();

        try
        {
            logger.LogInformation("Starting database seeding...");

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed data in order to maintain referential integrity
            await SeedUsersAsync(context, logger);
            await SeedHeritageSitesAsync(context, logger);
            await SeedEventsAsync(context, logger);
            await SeedARContentAsync(context, logger);
            await SeedBookmarksAsync(context, logger);
            await SeedReviewsAsync(context, logger);
            await SeedBookingsAsync(context, logger);

            logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private static async Task SeedUsersAsync(PenangGoContext context, ILogger logger)
    {
        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Users already exist, skipping user seeding");
            return;
        }

        // Dev-only seed accounts (known test credentials); the bootstrap admin password can be
        // overridden via PENANGGO_ADMIN_PASSWORD for any shared/staging deployment.
        var adminPassword = Environment.GetEnvironmentVariable("PENANGGO_ADMIN_PASSWORD") ?? "Admin123!";
        var adminHash = PasswordUtility.CreatePasswordHash(adminPassword);
        var user1Hash = PasswordUtility.CreatePasswordHash("Password123!");
        var user2Hash = PasswordUtility.CreatePasswordHash("Password123!");
        var user3Hash = PasswordUtility.CreatePasswordHash("Password123!");
        var user4Hash = PasswordUtility.CreatePasswordHash("Password123!");

        var users = new List<User>
        {
            new User
            {
                Name = "Admin User",
                Email = "admin@penanggo.com",
                PasswordHash = adminHash.hash,
                PasswordSalt = adminHash.salt,
                IsAdmin = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = true,
                Bio = "System administrator for PenangGo cultural heritage platform"
            },
            new User
            {
                Name = "Ahmad bin Ismail",
                Email = "ahmad.ismail@email.com",
                PasswordHash = user1Hash.hash,
                PasswordSalt = user1Hash.salt,
                IsAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                PhoneNumber = "+60123456789",
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = true,
                Bio = "Local Penang resident passionate about cultural heritage"
            },
            new User
            {
                Name = "Sarah Chen",
                Email = "sarah.chen@email.com",
                PasswordHash = user2Hash.hash,
                PasswordSalt = user2Hash.salt,
                IsAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                PhoneNumber = "+60187654321",
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = false,
                Bio = "Tourism student interested in Malaysian cultural sites"
            },
            new User
            {
                Name = "Rajesh Kumar",
                Email = "rajesh.kumar@email.com",
                PasswordHash = user3Hash.hash,
                PasswordSalt = user3Hash.salt,
                IsAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                PhoneNumber = "+60111222333",
                EmailNotificationsEnabled = false,
                PushNotificationsEnabled = true,
                Bio = "Photography enthusiast documenting cultural heritage"
            },
            new User
            {
                Name = "Li Wei",
                Email = "li.wei@email.com",
                PasswordHash = user4Hash.hash,
                PasswordSalt = user4Hash.salt,
                IsAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                PhoneNumber = "+60144555666",
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = true,
                Bio = "Cultural researcher studying Malaysian heritage"
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} users", users.Count);
    }

    private static async Task SeedHeritageSitesAsync(PenangGoContext context, ILogger logger)
    {
        if (await context.HeritageSites.AnyAsync())
        {
            logger.LogInformation("Heritage sites already exist, skipping heritage site seeding");
            return;
        }

        // Get admin user for submissions
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.IsAdmin);

        var heritageSites = new List<HeritageSite>
        {
            new HeritageSite
            {
                Name = "Khoo Kongsi",
                Description = "A magnificent Chinese clanhouse built in 1906, featuring elaborate architecture with intricate carvings, murals, and ornate decorations. It represents the wealth and influence of the Khoo clan in Penang.",
                Location = "Cannon Square, George Town, Penang",
                Latitude = 5.4164,
                Longitude = 100.3327,
                HasPanoramaContent = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-100),
                Category = "Clanhouse",
                HistoricalPeriod = "19th Century",
                CulturalSignificance = "Represents the wealth and influence of Chinese clans in colonial Penang",
                VisitingHours = "9:00 AM - 5:00 PM (Daily)",
                EntryFee = "RM 10 for adults, RM 5 for children",
                ContactInfo = "+604-261 4609",
                Website = "https://www.khookongsi.com.my",
                IsAccessible = true,
                ImageUrl = "[\"/images/khoo-kongsi-1.jpg\",\"/images/khoo-kongsi-2.jpg\",\"/images/khoo-kongsi-3.jpg\"]",
                AverageRating = 4.5,
                TotalReviews = 12,
                TotalBookmarks = 8,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedBy = adminUser?.Id,
                SubmittedAt = DateTime.UtcNow.AddDays(-100),
                ApprovedBy = adminUser?.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new HeritageSite
            {
                Name = "Cheong Fatt Tze Mansion",
                Description = "Also known as the Blue Mansion, this distinctive indigo-blue colored mansion was built in the 1880s. It showcases the fusion of Chinese and European architectural styles and was the home of the famous merchant Cheong Fatt Tze.",
                Location = "14 Leith Street, George Town, Penang",
                Latitude = 5.4214,
                Longitude = 100.3412,
                HasPanoramaContent = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-95),
                Category = "Mansion",
                HistoricalPeriod = "19th Century",
                CulturalSignificance = "Exemplifies the fusion of Chinese and European architectural styles",
                VisitingHours = "11:00 AM - 2:00 PM (Guided tours only)",
                EntryFee = "RM 25 for guided tour",
                ContactInfo = "+604-262 0006",
                Website = "https://www.cheongfatttzemansion.com",
                IsAccessible = false,
                ImageUrl = "[\"/images/blue-mansion-1.jpg\",\"/images/blue-mansion-2.jpg\"]",
                AverageRating = 4.8,
                TotalReviews = 15,
                TotalBookmarks = 12,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedBy = adminUser?.Id,
                SubmittedAt = DateTime.UtcNow.AddDays(-95),
                ApprovedBy = adminUser?.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new HeritageSite
            {
                Name = "Kapitan Keling Mosque",
                Description = "The oldest mosque in Penang, built in 1801 by Indian Muslim traders. It features a distinctive yellow dome and minarets, representing the Moorish architectural style.",
                Location = "Jalan Masjid Kapitan Keling, George Town, Penang",
                Latitude = 5.4197,
                Longitude = 100.3394,
                HasPanoramaContent = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                Category = "Mosque",
                HistoricalPeriod = "19th Century",
                CulturalSignificance = "Oldest mosque in Penang, represents Islamic heritage",
                VisitingHours = "Open for prayers (Non-Muslims: 1:00 PM - 5:00 PM)",
                EntryFee = "Free",
                ContactInfo = "+604-261 4217",
                Website = "",
                IsAccessible = true,
                ImageUrl = "[\"/images/kapitan-keling-1.jpg\",\"/images/kapitan-keling-2.jpg\"]",
                AverageRating = 4.2,
                TotalReviews = 8,
                TotalBookmarks = 5,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedBy = adminUser?.Id,
                SubmittedAt = DateTime.UtcNow.AddDays(-90),
                ApprovedBy = adminUser?.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new HeritageSite
            {
                Name = "Sri Mahamariamman Temple",
                Description = "The oldest Hindu temple in Penang, built in 1833. It features intricate sculptures of Hindu deities and is known for its annual Thaipusam festival procession.",
                Location = "Jalan Masjid Kapitan Keling, George Town, Penang",
                Latitude = 5.4195,
                Longitude = 100.3392,
                HasPanoramaContent = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-85),
                Category = "Temple",
                HistoricalPeriod = "19th Century",
                CulturalSignificance = "Oldest Hindu temple in Penang, center of Hindu religious activities",
                VisitingHours = "6:00 AM - 9:00 PM (Daily)",
                EntryFee = "Free",
                ContactInfo = "+604-261 4628",
                Website = "",
                IsAccessible = true,
                ImageUrl = "[\"/images/sri-mahamariamman-1.jpg\",\"/images/sri-mahamariamman-2.jpg\"]",
                AverageRating = 4.0,
                TotalReviews = 6,
                TotalBookmarks = 3,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedBy = adminUser?.Id,
                SubmittedAt = DateTime.UtcNow.AddDays(-85),
                ApprovedBy = adminUser?.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new HeritageSite
            {
                Name = "Pinang Peranakan Mansion",
                Description = "A restored mansion showcasing the lifestyle of the wealthy Peranakan (Straits Chinese) community. It features antique furniture, traditional costumes, and elaborate decorations.",
                Location = "29 Church Street, George Town, Penang",
                Latitude = 5.4208,
                Longitude = 100.3418,
                HasPanoramaContent = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-80),
                Category = "Museum",
                HistoricalPeriod = "19th Century",
                CulturalSignificance = "Showcases Peranakan culture and lifestyle",
                VisitingHours = "9:30 AM - 5:00 PM (Daily)",
                EntryFee = "RM 20 for adults, RM 10 for children",
                ContactInfo = "+604-264 2929",
                Website = "https://www.pinangperanakanmansion.com.my",
                IsAccessible = true,
                ImageUrl = "[\"/images/peranakan-mansion-1.jpg\",\"/images/peranakan-mansion-2.jpg\",\"/images/peranakan-mansion-3.jpg\"]",
                AverageRating = 4.6,
                TotalReviews = 18,
                TotalBookmarks = 14,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedBy = adminUser?.Id,
                SubmittedAt = DateTime.UtcNow.AddDays(-80),
                ApprovedBy = adminUser?.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new HeritageSite
            {
                Name = "Fort Cornwallis",
                Description = "A star-shaped fort built by the British East India Company in the late 18th century. It was named after Charles Cornwallis and served as a defensive structure for the settlement.",
                Location = "Jalan Tun Syed Sheh Barakbah, George Town, Penang",
                Latitude = 5.4219,
                Longitude = 100.3447,
                HasPanoramaContent = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-75),
                Category = "Fort",
                HistoricalPeriod = "18th Century",
                CulturalSignificance = "First British fort in Malaysia, represents colonial history",
                VisitingHours = "9:00 AM - 6:30 PM (Daily)",
                EntryFee = "RM 20 for adults, RM 10 for children",
                ContactInfo = "+604-261 0262",
                Website = "",
                IsAccessible = true,
                ImageUrl = "[\"/images/fort-cornwallis-1.jpg\",\"/images/fort-cornwallis-2.jpg\"]",
                AverageRating = 3.8,
                TotalReviews = 10,
                TotalBookmarks = 7,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedBy = adminUser?.Id,
                SubmittedAt = DateTime.UtcNow.AddDays(-75),
                ApprovedBy = adminUser?.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new HeritageSite
            {
                Name = "Gurney Drive",
                Description = "A famous seafront promenade known for its street food and beautiful sunset views. It's a popular spot for locals and tourists to enjoy Penang's famous hawker food.",
                Location = "Gurney Drive, George Town, Penang",
                Latitude = 5.4702,
                Longitude = 100.3089,
                HasPanoramaContent = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-70),
                Category = "Promenade",
                HistoricalPeriod = "20th Century",
                CulturalSignificance = "Famous food destination and sunset viewing spot",
                VisitingHours = "24 hours (Food stalls: 6:00 PM - 12:00 AM)",
                EntryFee = "Free",
                ContactInfo = "",
                Website = "",
                IsAccessible = true,
                ImageUrl = "[\"/images/gurney-drive-1.jpg\",\"/images/gurney-drive-2.jpg\"]",
                AverageRating = 4.3,
                TotalReviews = 22,
                TotalBookmarks = 16,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedBy = adminUser?.Id,
                SubmittedAt = DateTime.UtcNow.AddDays(-70),
                ApprovedBy = adminUser?.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new HeritageSite
            {
                Name = "Penang Hill",
                Description = "A hill resort located 6km from George Town. It offers cool weather, colonial bungalows, and panoramic views of the island. The funicular railway to the top is a popular attraction.",
                Location = "Penang Hill, Air Itam, Penang",
                Latitude = 5.4211,
                Longitude = 100.2756,
                HasPanoramaContent = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                Category = "Hill Resort",
                HistoricalPeriod = "19th Century",
                CulturalSignificance = "Colonial hill station with panoramic views",
                VisitingHours = "6:30 AM - 11:00 PM (Daily)",
                EntryFee = "RM 30 for adults, RM 15 for children (Funicular)",
                ContactInfo = "+604-828 8880",
                Website = "https://www.penanghill.gov.my",
                IsAccessible = true,
                ImageUrl = "[\"/images/penang-hill-1.jpg\",\"/images/penang-hill-2.jpg\",\"/images/penang-hill-3.jpg\"]",
                AverageRating = 4.7,
                TotalReviews = 25,
                TotalBookmarks = 20,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedBy = adminUser?.Id,
                SubmittedAt = DateTime.UtcNow.AddDays(-65),
                ApprovedBy = adminUser?.Id,
                ApprovedAt = DateTime.UtcNow
            }
        };

        context.HeritageSites.AddRange(heritageSites);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} heritage sites", heritageSites.Count);
    }

    private static async Task SeedEventsAsync(PenangGoContext context, ILogger logger)
    {
        if (await context.Events.AnyAsync())
        {
            logger.LogInformation("Events already exist, skipping event seeding");
            return;
        }

        var events = new List<Event>
        {
            new Event
            {
                Title = "Heritage Walking Tour - George Town",
                Description = "Explore the historic streets of George Town with our expert guide. Visit UNESCO World Heritage sites and learn about the rich cultural history of Penang.",
                StartTime = DateTime.UtcNow.AddDays(7).AddHours(9),
                EndTime = DateTime.UtcNow.AddDays(7).AddHours(12),
                Location = "George Town Heritage Zone",
                MaxCapacity = 20,
                CurrentBookings = 8,
                Price = 50.00m,
                IsActive = true,
                IsTour = true,
                ImageUrl = "/images/events/heritage-walking-tour.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Type = "Tour",
                Icon = "🏛️",
                Gradient = "linear-gradient(135deg, #D4AF37, #8B0000)",
                IsPopular = true,
                IsNew = false
            },
            new Event
            {
                Title = "Traditional Peranakan Cooking Class",
                Description = "Learn to cook authentic Peranakan dishes from a master chef. Experience the unique fusion of Chinese and Malay culinary traditions.",
                StartTime = DateTime.UtcNow.AddDays(14).AddHours(14),
                EndTime = DateTime.UtcNow.AddDays(14).AddHours(17),
                Location = "Pinang Peranakan Mansion",
                MaxCapacity = 12,
                CurrentBookings = 5,
                Price = 120.00m,
                IsActive = true,
                IsTour = false,
                ImageUrl = "/images/events/peranakan-cooking.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                Type = "Workshop",
                Icon = "👨‍🍳",
                Gradient = "linear-gradient(135deg, #FF6B35, #F7931E)",
                IsPopular = false,
                IsNew = true
            },
            new Event
            {
                Title = "Sunset Photography at Gurney Drive",
                Description = "Capture the beautiful sunset at Gurney Drive with professional photography guidance. Learn techniques for landscape and street photography.",
                StartTime = DateTime.UtcNow.AddDays(5).AddHours(17),
                EndTime = DateTime.UtcNow.AddDays(5).AddHours(19),
                Location = "Gurney Drive Promenade",
                MaxCapacity = 15,
                CurrentBookings = 12,
                Price = 35.00m,
                IsActive = true,
                IsTour = false,
                ImageUrl = "/images/events/sunset-photography.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                Type = "Workshop",
                Icon = "📸",
                Gradient = "linear-gradient(135deg, #E74C3C, #C0392B)",
                IsPopular = false,
                IsNew = true
            },
            new Event
            {
                Title = "Cultural Festival - Thaipusam",
                Description = "Experience the vibrant Thaipusam festival at Batu Caves. Witness the spectacular procession and traditional ceremonies.",
                StartTime = DateTime.UtcNow.AddDays(21).AddHours(6),
                EndTime = DateTime.UtcNow.AddDays(21).AddHours(18),
                Location = "Batu Caves, Kuala Lumpur",
                MaxCapacity = 50,
                CurrentBookings = 25,
                Price = 80.00m,
                IsActive = true,
                IsTour = true,
                ImageUrl = "/images/events/thaipusam-festival.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                Type = "Festival",
                Icon = "🕉️",
                Gradient = "linear-gradient(135deg, #9B59B6, #8E44AD)",
                IsPopular = true,
                IsNew = false
            },
            new Event
            {
                Title = "AR Heritage Experience - Khoo Kongsi",
                Description = "Experience Khoo Kongsi through augmented reality. Learn about the history and architecture through interactive digital content.",
                StartTime = DateTime.UtcNow.AddDays(10).AddHours(10),
                EndTime = DateTime.UtcNow.AddDays(10).AddHours(11),
                Location = "Khoo Kongsi",
                MaxCapacity = 8,
                CurrentBookings = 3,
                Price = 25.00m,
                IsActive = true,
                IsTour = false,
                ImageUrl = "/images/events/ar-heritage.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Type = "Exhibition",
                Icon = "🎨",
                Gradient = "linear-gradient(135deg, #3498DB, #2980B9)",
                IsPopular = false,
                IsNew = true
            }
        };

        context.Events.AddRange(events);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} events", events.Count);
    }

    private static async Task SeedARContentAsync(PenangGoContext context, ILogger logger)
    {
        if (await context.PanoramaContents.AnyAsync())
        {
            logger.LogInformation("AR content already exists, skipping AR content seeding");
            return;
        }

        // Get admin user for uploader
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.IsAdmin);
        if (adminUser == null)
        {
            logger.LogWarning("No admin user found, skipping AR content seeding");
            return;
        }

        // Fetch heritage sites by name to get their actual IDs
        var khooKongsi = await context.HeritageSites.FirstOrDefaultAsync(h => h.Name == "Khoo Kongsi");
        var blueMansion = await context.HeritageSites.FirstOrDefaultAsync(h => h.Name == "Cheong Fatt Tze Mansion");
        var peranakanMansion = await context.HeritageSites.FirstOrDefaultAsync(h => h.Name == "Pinang Peranakan Mansion");
        var fortCornwallis = await context.HeritageSites.FirstOrDefaultAsync(h => h.Name == "Fort Cornwallis");
        var penangHill = await context.HeritageSites.FirstOrDefaultAsync(h => h.Name == "Penang Hill");

        var panoramaContents = new List<PanoramaContent>
        {
            new PanoramaContent {
                Title = "Komtar Tower Street View",
                Description = "Panorama of Komtar Tower, Penang.",
                PanoramaUrl = "https://www.google.com/maps/embed?pb=!4v1718199999999!6m8!1m7!1sCAoSLEFGMVFpcE1wZ2ZyQ2d6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6!2m2!1d5.4141!2d100.3288!3f0!4f0!5f0.7820865974627469",
                HeritageSiteId = khooKongsi?.Id ?? 0,
                UploadedBy = adminUser.Id,
                UploadedAt = DateTime.UtcNow,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
                ApprovedBy = adminUser.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new PanoramaContent {
                Title = "Cheong Fatt Tze Mansion Street View",
                Description = "Panorama of Cheong Fatt Tze Mansion, Penang.",
                PanoramaUrl = "https://www.google.com/maps/embed?pb=!4v1718199999999!6m8!1m7!1sCAoSLEFGMVFpcE1wZ2ZyQ2d6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6!2m2!1d5.4214!2d100.3412!3f0!4f0!5f0.7820865974627469",
                HeritageSiteId = blueMansion?.Id ?? 0,
                UploadedBy = adminUser.Id,
                UploadedAt = DateTime.UtcNow,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
                ApprovedBy = adminUser.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new PanoramaContent {
                Title = "Pinang Peranakan Mansion Street View",
                Description = "Panorama of Pinang Peranakan Mansion, Penang.",
                PanoramaUrl = "https://www.google.com/maps/embed?pb=!4v1718199999999!6m8!1m7!1sCAoSLEFGMVFpcE1wZ2ZyQ2d6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6!2m2!1d5.4208!2d100.3418!3f0!4f0!5f0.7820865974627469",
                HeritageSiteId = peranakanMansion?.Id ?? 0,
                UploadedBy = adminUser.Id,
                UploadedAt = DateTime.UtcNow,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
                ApprovedBy = adminUser.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new PanoramaContent {
                Title = "Fort Cornwallis Street View",
                Description = "Panorama of Fort Cornwallis, Penang.",
                PanoramaUrl = "https://www.google.com/maps/embed?pb=!4v1718199999999!6m8!1m7!1sCAoSLEFGMVFpcE1wZ2ZyQ2d6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6!2m2!1d5.4219!2d100.3447!3f0!4f0!5f0.7820865974627469",
                HeritageSiteId = fortCornwallis?.Id ?? 0,
                UploadedBy = adminUser.Id,
                UploadedAt = DateTime.UtcNow,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
                ApprovedBy = adminUser.Id,
                ApprovedAt = DateTime.UtcNow
            },
            new PanoramaContent {
                Title = "Penang Hill Railway Journey Street View",
                Description = "Panorama of Penang Hill funicular railway journey, Penang.",
                PanoramaUrl = "https://www.google.com/maps/embed?pb=!4v1718199999999!6m8!1m7!1sCAoSLEFGMVFpcE1wZ2ZyQ2d6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6b2Z6b3h6!2m2!1d5.4211!2d100.2756!3f0!4f0!5f0.7820865974627469",
                HeritageSiteId = penangHill?.Id ?? 0,
                UploadedBy = adminUser.Id,
                UploadedAt = DateTime.UtcNow,
                // Approval system fields for admin-created content
                ApprovalStatus = ApprovalStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
                ApprovedBy = adminUser.Id,
                ApprovedAt = DateTime.UtcNow
            }
        };
        context.PanoramaContents.AddRange(panoramaContents);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} panorama content items", panoramaContents.Count);
    }

    private static async Task SeedBookmarksAsync(PenangGoContext context, ILogger logger)
    {
        if (await context.Bookmarks.AnyAsync())
        {
            logger.LogInformation("Bookmarks already exist, skipping bookmark seeding");
            return;
        }

        var users = await context.Users.ToListAsync();
        var sites = await context.HeritageSites.ToListAsync();

        var bookmarks = new List<Bookmark>();

        // Create some sample bookmarks
        foreach (var user in users.Where(u => !u.IsAdmin))
        {
            var randomSites = sites.OrderBy(x => Guid.NewGuid()).Take(Random.Shared.Next(1, 4));
            foreach (var site in randomSites)
            {
                bookmarks.Add(new Bookmark
                {
                    UserId = user.Id,
                    SiteId = site.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30))
                });
            }
        }

        context.Bookmarks.AddRange(bookmarks);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} bookmarks", bookmarks.Count);
    }

    private static async Task SeedReviewsAsync(PenangGoContext context, ILogger logger)
    {
        if (await context.Reviews.AnyAsync())
        {
            logger.LogInformation("Reviews already exist, skipping review seeding");
            return;
        }

        var users = await context.Users.Where(u => !u.IsAdmin).ToListAsync();
        var sites = await context.HeritageSites.ToListAsync();

        var reviews = new List<Review>();

        foreach (var site in sites)
        {
            var numReviews = Random.Shared.Next(2, 6);
            var randomUsers = users.OrderBy(x => Guid.NewGuid()).Take(numReviews);

            foreach (var user in randomUsers)
            {
                reviews.Add(new Review
                {
                    UserId = user.Id,
                    SiteId = site.Id,
                    Rating = Random.Shared.Next(3, 6),
                    Comment = GetRandomReviewComment(site.Name),
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 60))
                });
            }
        }

        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} reviews", reviews.Count);
    }

    private static async Task SeedBookingsAsync(PenangGoContext context, ILogger logger)
    {
        if (await context.Bookings.AnyAsync())
        {
            logger.LogInformation("Bookings already exist, skipping booking seeding");
            return;
        }

        var users = await context.Users.Where(u => !u.IsAdmin).ToListAsync();
        var events = await context.Events.ToListAsync();

        var bookings = new List<Booking>();

        foreach (var evt in events)
        {
            var numBookings = Random.Shared.Next(1, Math.Min(evt.MaxCapacity, 8));
            var randomUsers = users.OrderBy(x => Guid.NewGuid()).Take(numBookings);

            foreach (var user in randomUsers)
            {
                bookings.Add(new Booking
                {
                    UserId = user.Id,
                    EventId = evt.Id,
                    BookingTime = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    Status = BookingStatus.Confirmed
                });
            }
        }

        context.Bookings.AddRange(bookings);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} bookings", bookings.Count);
    }

    private static string GetRandomReviewComment(string siteName)
    {
        var comments = new List<string>
        {
            $"Amazing historical site! The architecture is absolutely stunning and the guided tour was very informative.",
            $"Beautiful place with rich cultural heritage. Highly recommend visiting this site.",
            $"Great experience learning about the history and culture. The staff were very friendly and knowledgeable.",
            $"A must-visit destination in Penang. The preservation work is impressive.",
            $"Fascinating place with so much history. Perfect for photography and cultural exploration.",
            $"Wonderful heritage site with excellent restoration work. The stories behind it are captivating.",
            $"Absolutely loved this place! The cultural significance and architectural beauty are remarkable.",
            $"A gem of Malaysian heritage. The attention to detail in preservation is outstanding.",
            $"Incredible historical value and beautiful surroundings. Well worth the visit.",
            $"Excellent cultural experience. The site tells such an important story of our heritage."
        };

        return comments[Random.Shared.Next(comments.Count)];
    }
}