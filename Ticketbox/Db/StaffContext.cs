using Microsoft.EntityFrameworkCore;
using Ticketbox.Models;

namespace Ticketbox.Db
{
    internal class StaffContext : DbContext
    {
        public DbSet<Staff> Staffs => Set<Staff>();
        public StaffContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=staffs.db");
        }
    }
}
