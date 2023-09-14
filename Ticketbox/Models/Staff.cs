namespace Ticketbox.Models
{
    internal class Staff
    {
        public long Id { get; set; }
        public ulong MemberId { get; set; }
        public string Name { get; set; } = null!;
    }
}
