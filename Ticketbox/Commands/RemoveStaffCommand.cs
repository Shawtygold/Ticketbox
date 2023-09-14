using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Ticketbox.Db;
using Ticketbox.Models;

namespace Ticketbox.Commands
{
    internal class RemoveStaffCommand : ApplicationCommandModule
    {
        #region [Remove Staff]

        [SlashCommand("removestaff", "Removes a staff member.")]
        public static async Task RemoveStaff(InteractionContext ctx,
            [Option("User", "User to remove from staff.")] DiscordUser user)
        {
            if (!PermissionsManager.CheckPermissionsIn(ctx.Member, ctx.Channel, new() { Permissions.Administrator }))
            {
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                {
                    Title = "Insufficient permissions.",
                    Color = DiscordColor.Red,
                    Description = "You need **Administrator** permission for this command."
                }, true);
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordMember bot;
            try
            {
                bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "Could not find itself on the server. Please try again or contact the developer."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.AccessChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please check the permissions."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissions(bot, new() { Permissions.SendMessages }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages. Please check the permissions."
                }));
                return;
            }

            DiscordMember member;
            try
            {
                member = await ctx.Guild.GetMemberAsync(user.Id);
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "Could not find user on the Discord server. Please try again or contact the developer."
                }));
                return;
            }

            if(!await Database.RemoveStaff(member.Id))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "The staff was not removed from the database! Please check the user id and try again."
                }));
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "Complete.",
                Color = DiscordColor.Green,
                Description = $"Staff {member.Mention} has been successfully removed!"
            }));
        }

        #endregion
    }
}
