using Microsoft.EntityFrameworkCore;

namespace PenangGoApp.DataAccess
{
    public class PenangGoOfflineContext : DbContext
    {
        public PenangGoOfflineContext(DbContextOptions<PenangGoOfflineContext> options) : base(options) { }
        public DbSet<HeritageSite> HeritageSites { get; set; }
        public DbSet<Event> Events { get; set; }
        // Add other entities as needed
    }
} 