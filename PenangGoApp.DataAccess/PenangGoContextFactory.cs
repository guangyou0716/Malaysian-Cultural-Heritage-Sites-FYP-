using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PenangGoApp.DataAccess
{
    public class PenangGoContextFactory : IDesignTimeDbContextFactory<PenangGoContext>
    {
        public PenangGoContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PenangGoContext>();
            // Use your actual connection string here
            var connectionString = "server=localhost;database=penang_go;user=root;password=1234;";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            return new PenangGoContext(optionsBuilder.Options);
        }
    }
} 