using Microsoft.EntityFrameworkCore;
using Ticketbox.Models;

namespace Ticketbox.Db
{
    internal class PanelsContext : DbContext
    {
        public DbSet<Panel> Panels => Set<Panel>();
        public PanelsContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=panels.db");
        }
    }
}
