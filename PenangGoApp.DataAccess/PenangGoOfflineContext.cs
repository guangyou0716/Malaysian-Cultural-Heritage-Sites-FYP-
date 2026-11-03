using Microsoft.EntityFrameworkCore;

namespace PenangGoApp.DataAccess
{
    /// <summary>
    /// Local SQLite cache/outbox. Not a source of truth — the server is. Every cached
    /// entity carries shadow columns ServerUpdatedAt / CachedAt / SyncState / SchemaVersion.
    /// </summary>
    public class PenangGoOfflineContext : DbContext
    {
        public PenangGoOfflineContext(DbContextOptions<PenangGoOfflineContext> options) : base(options) { }

        public DbSet<HeritageSite> HeritageSites { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<OfflineOutboxItem> OutboxItems { get; set; }
        public DbSet<OfflineSyncMeta> SyncMeta { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            void WithCacheMeta<TEntity>(ModelBuilder mb) where TEntity : class
            {
                mb.Entity<TEntity>().Property<DateTime?>("ServerUpdatedAt");
                mb.Entity<TEntity>().Property<DateTime>("CachedAt");
                mb.Entity<TEntity>().Property<string>("SyncState").HasMaxLength(16);
                mb.Entity<TEntity>().Property<int>("SchemaVersion");
            }

            // Site snapshots: navigation state is not persisted locally
            modelBuilder.Entity<HeritageSite>(e =>
            {
                e.Ignore(x => x.Bookmarks);
                e.Ignore(x => x.Reviews);
                e.Ignore(x => x.PanoramaContents);
                e.Ignore(x => x.SiteVisitBookings);
                e.Ignore(x => x.Submitter);
                e.Ignore(x => x.Approver);
            });
            WithCacheMeta<HeritageSite>(modelBuilder);

            modelBuilder.Entity<Event>(e => e.Ignore(x => x.Bookings));
            WithCacheMeta<Event>(modelBuilder);

            // Bookmark mirror carries the SERVER bookmark id, so removals can be
            // flushed against /api/sync/upload without further lookups.
            modelBuilder.Entity<Bookmark>(e => e.Ignore(x => x.Site));
            WithCacheMeta<Bookmark>(modelBuilder);

            modelBuilder.Entity<Booking>(e =>
            {
                e.Ignore(x => x.User);
                e.Ignore(x => x.Event);
            });
            WithCacheMeta<Booking>(modelBuilder);

            modelBuilder.Entity<OfflineOutboxItem>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Operation);
            });

            modelBuilder.Entity<OfflineSyncMeta>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Key).IsUnique();
            });
        }
    }
}