using DSharpPlus;
using DSharpPlus.Entities;

namespace Ticketbox.Models
{
    internal class PermissionsManager
    {
        public static bool CheckPermissions(DiscordMember member, List<Permissions> permissions)
        {
            if (member == null || permissions == null)
                return false;

            for (int i = 0; i < permissions.Count; i++)
            {
                if (!member.Permissions.HasPermission(permissions[i]))
                    return false;
            }

            return true;
        }

        public static bool CheckPermissionsIn(DiscordMember member, DiscordChannel channel, List<Permissions> permissions)
        {
            if (member == null || channel == null || permissions == null)
                return false;

            for(int i = 0; i < permissions.Count; i++)
            {
                if (!member.PermissionsIn(channel).HasPermission(permissions[i]))
                    return false;
            }

            return true;
        }
    }
}
