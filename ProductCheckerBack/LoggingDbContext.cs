using ProductCheckerBack.Models.Logging;
using Microsoft.EntityFrameworkCore;

namespace ProductCheckerBack
{
    internal class LoggingDbContext : DbContext
    {
        public DbSet<ErrorLog> Logs { get; set; }
        public DbSet<Tool> Tools { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(Configuration.GetConnectionString("LoggingDbContext"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ErrorLogConfiguration());
        }
    }
}
