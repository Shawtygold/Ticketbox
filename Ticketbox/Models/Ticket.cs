namespace Ticketbox.Models
{
    internal class Ticket
    {
        public long Id { get; set; }
        public ulong CreatorId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong? CategoryId { get; set; }
        public bool IsClosed { get; set; }
    }
}
