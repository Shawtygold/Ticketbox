using System.ComponentModel.DataAnnotations.Schema;

namespace Ticketbox.Models
{
    internal class Staff
    {
        public long Id { get; set; }
        public ulong MemberId { get; set; }
        public string Name { get; set; } = null!;
        public ulong GuildId { get; set; }
        [NotMapped] public bool Active { get; set; }

        //public Staff(ulong memberId, string name, ulong guildId)
        //{
        //    GuildId = guildId;
        //    MemberId = memberId;
        //    Name = name;
        //}
    }
}
