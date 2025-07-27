using Microsoft.EntityFrameworkCore;

namespace PenangGoApp.DataAccess
{
    public class PenangGoContext : DbContext
    {
        public PenangGoContext(DbContextOptions<PenangGoContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<HeritageSite> HeritageSites { get; set; }
        public DbSet<PanoramaContent> PanoramaContents { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.PasswordSalt).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Bio).HasMaxLength(500);
            });

            // Bookmark configuration
            modelBuilder.Entity<Bookmark>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User).WithMany(u => u.Bookmarks).HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Site).WithMany(s => s.Bookmarks).HasForeignKey(e => e.SiteId);
                entity.HasIndex(e => new { e.UserId, e.SiteId }).IsUnique();
            });

            // Booking configuration
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User).WithMany(u => u.Bookings).HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Event).WithMany(ev => ev.Bookings).HasForeignKey(e => e.EventId);
                entity.Property(e => e.Status).HasConversion<string>();
            });

            // Review configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User).WithMany(u => u.Reviews).HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Site).WithMany(s => s.Reviews).HasForeignKey(e => e.SiteId);
                entity.Property(e => e.Rating).IsRequired();
                entity.Property(e => e.Comment).IsRequired();
            });

            // HeritageSite configuration
            modelBuilder.Entity<HeritageSite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Location).IsRequired();
                entity.Property(e => e.Latitude).IsRequired();
                entity.Property(e => e.Longitude).IsRequired();
            });

            // Event configuration
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
                entity.Property(e => e.Location).IsRequired();
                entity.Property(e => e.MaxCapacity).IsRequired();
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            // Seed data for testing
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Create a default admin user
            var (adminHash, adminSalt) = CreatePasswordHash("Admin123!");
            var adminUser = new User
            {
                Id = 1,
                Name = "Admin User",
                Email = "admin@penanggo.com",
                PasswordHash = adminHash,
                PasswordSalt = adminSalt,
                IsAdmin = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = true
            };

            modelBuilder.Entity<User>().HasData(adminUser);

            // Create some sample heritage sites
            var heritageSites = new List<HeritageSite>
            {
                new HeritageSite
                {
                    Id = 1,
                    Name = "Khoo Kongsi",
                    Description = "A large Chinese clanhouse with elaborate and highly ornamented architecture.",
                    Location = "Cannon Square, George Town, Penang",
                    Latitude = 5.4164,
                    Longitude = 100.3327,
                    HasPanoramaContent = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new HeritageSite
                {
                    Id = 2,
                    Name = "Cheong Fatt Tze Mansion",
                    Description = "A distinctive blue-colored mansion built in the 1880s.",
                    Location = "14 Leith Street, George Town, Penang",
                    Latitude = 5.4214,
                    Longitude = 100.3412,
                    HasPanoramaContent = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            modelBuilder.Entity<HeritageSite>().HasData(heritageSites);

            // Create some sample events
            var events = new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Title = "Heritage Walking Tour",
                    Description = "Explore the historic streets of George Town with our expert guide.",
                    StartTime = DateTime.UtcNow.AddDays(7),
                    EndTime = DateTime.UtcNow.AddDays(7).AddHours(3),
                    Location = "George Town Heritage Zone",
                    MaxCapacity = 20,
                    CurrentBookings = 0,
                    Price = 50.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            modelBuilder.Entity<Event>().HasData(events);
        }

        private (string hash, string salt) CreatePasswordHash(string password)
        {
            return PasswordUtility.CreatePasswordHash(password);
        }
    }
} 