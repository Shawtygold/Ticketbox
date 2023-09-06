namespace Ticketbox.Models
{
    internal class Ticket
    {
        public long Id { get; set; }
        public ulong CreatorId { get; set; }
        public ulong AssignedStaffId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
    }
}
