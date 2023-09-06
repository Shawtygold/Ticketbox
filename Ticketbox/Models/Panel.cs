namespace Ticketbox.Models
{
    internal class Panel
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public ulong ChannelId { get; set; }
        public string ButtonMessage { get; set; } = null!;
        public string ButtonColor { get; set; } = null!;
        public ulong TicketCategoryId { get; set; }
        public ulong? LogChannelId { get; set; }
        public ulong? TranscriptChannelId { get; set; }
    }
}
