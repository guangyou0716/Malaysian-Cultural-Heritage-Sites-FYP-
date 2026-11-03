using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PenangGoApp.DataAccess
{
    public class PenangGoContextFactory : IDesignTimeDbContextFactory<PenangGoContext>
    {
        public PenangGoContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PenangGoContext>();
            var connectionString = Environment.GetEnvironmentVariable("PENANGGO_DB_CONNECTION")
                ?? throw new InvalidOperationException(
                    "Set the PENANGGO_DB_CONNECTION environment variable to your MySQL connection string before running EF tools.");
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            return new PenangGoContext(optionsBuilder.Options);
        }
    }
}