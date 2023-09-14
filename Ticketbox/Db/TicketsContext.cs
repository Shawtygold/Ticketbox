using Microsoft.EntityFrameworkCore;
using Ticketbox.Models;

namespace Ticketbox.Db
{
    internal class TicketsContext : DbContext
    {
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public TicketsContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=tickets.db");
        }
    }
}
