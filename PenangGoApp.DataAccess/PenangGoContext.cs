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
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SiteVisitBooking> SiteVisitBookings { get; set; }

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

            // SiteVisitBooking configuration
            modelBuilder.Entity<SiteVisitBooking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
                entity.HasOne(e => e.Site).WithMany(s => s.SiteVisitBookings).HasForeignKey(e => e.HeritageSiteId);
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasIndex(e => new { e.HeritageSiteId, e.VisitDate, e.Status });
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
        }
    }
}