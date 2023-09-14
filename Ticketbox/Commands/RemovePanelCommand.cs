using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using System.Diagnostics.Metrics;
using Ticketbox.Db;
using Ticketbox.Models;

namespace Ticketbox.Commands
{
    internal class RemovePanelCommand : ApplicationCommandModule
    {
        #region [Remove Panel]

        [SlashCommand("removepanel", "Removes the panel.")]
        public static async Task RemovePanel(InteractionContext ctx,
            [Option("panelID", "Panel ID.")] long panelId)
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
            catch (ServerErrorException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "Server Error Exception. Please try again or contact the developer."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.AccessChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "Insufficient permissions.",
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please check the permissions."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissions(bot, new() { Permissions.SendMessages }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "Insufficient permissions.",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages. Please check the permissions."
                }));
                return;
            }

            Panel? panel = await Database.GetPanelAsync(panelId);
            if(panel == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "The panel could not be found. Сheck the panel Id."
                }));
                return;
            }

            if(!await Database.RemovePanel(panel))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "Сould not remove the panel from the database. Please try again or contact the developer."
                }));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "Complete.",
                Color = DiscordColor.Green,
                Description = $"The panel has been successfully removed!"
            }));
        }

        #endregion
    }
}
