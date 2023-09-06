using Microsoft.EntityFrameworkCore;
using Ticketbox.Models;

namespace Ticketbox.Db
{
    internal class ApplicationContext : DbContext
    {
        public DbSet<Panel> Panels => Set<Panel>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<Ticket> TicketArchive => Set<Ticket>();
        //public DbSet<Category> Categories=> Set<Category>();
        public DbSet<Staff> Staffs => Set<Staff>();
        public ApplicationContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=databases.db");
        
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<List<ulong>>().HasNoKey();
        //}
    }
}
