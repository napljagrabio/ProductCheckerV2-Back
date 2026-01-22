using ProductCheckerBack.Models.ProductChecker;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ProductCheckerBack
{
    internal class ProductCheckerDbContext : DbContext
    {
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<ApiEndpoint> ApiEndpoints { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(Configuration.GetConnectionString("ProductCheckerDbContext"),
                options => options.EnableRetryOnFailure(5, TimeSpan.FromMinutes(2), null));
        }
    }
}