using Microsoft.EntityFrameworkCore;
using Ticketbox.Models;

namespace Ticketbox.Db
{
    internal class TicketsArchiveContext : DbContext
    {
        public DbSet<Ticket> TicketArchive => Set<Ticket>();
        public TicketsArchiveContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=ticketarchive.db");
        }
    }
}
