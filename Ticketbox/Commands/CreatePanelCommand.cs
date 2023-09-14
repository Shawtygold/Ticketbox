using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Ticketbox.Db;
using Ticketbox.Models;

namespace Ticketbox.Commands
{
    internal class CreatePanelCommand : ApplicationCommandModule
    {
        [SlashCommand("create_panel", "Creates and sends an interaction panel to be able to create a ticket.")]
        public static async Task OnExecute(InteractionContext ctx, 
            [Option("title","Panel Title")][MaximumLength(50)] string panelTitle,
            [Option("channel", "Discord channel where the panel will be sent to.")] DiscordChannel panelChannel,
            [Option("open_tickets_category", "The Discord category in which the open tickets will be located.")] DiscordChannel openTicketsCategory,
            [Option("close_tickets_category", "The Discord category where the closed tickets will be located.")] DiscordChannel? closeTicketsCategory = null,
            [Option("log_channel", "Discord channel where logs will be sent.")] DiscordChannel? logChannel = null,
            [Option("message", "The message that will be located inside the panel.")][MaximumLength(50)] string? panelDescription = null,
            [Option("button_message", "The inscription on the panel button.")][MaximumLength(30)] string? panelButtonMessage = null,
            [Option("button_emomji", "The emomji located on the panel button.")] DiscordEmoji? panelButtonEmmoji = null,
            [Choice("Blue", "Primary")]
            [Choice("Gray", "Secondary")]
            [Choice("Red", "Danger")]
            [Choice("Green", "Success")]
            [Option("button_color", "Panel Button color.")] string? panelButtonColor = null)
        {
            if(!PermissionsManager.CheckPermissionsIn(ctx.Member, ctx.Channel, new() { Permissions.Administrator }))
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

            if(!PermissionsManager.CheckPermissions(bot, new() { Permissions.SendMessages, Permissions.EmbedLinks, Permissions.AttachFiles, Permissions.ManageRoles, Permissions.ManageChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages, embed links, attach files, manage roles or manage channels. Please check the permissions."
                }));
                return;
            }

            DiscordEmoji defaultButtonEmoji = DiscordEmoji.FromName(ctx.Client, ":envelope_with_arrow:");
            panelButtonEmmoji ??= defaultButtonEmoji;
            panelDescription ??= "To create a ticket react with" + defaultButtonEmoji;

            panelButtonColor ??= "Primary";
            ButtonStyle buttonStyle = new();
            switch (panelButtonColor)
            {
                case "Primary": buttonStyle = ButtonStyle.Primary; break;
                case "Secondary": buttonStyle = ButtonStyle.Secondary; break;
                case "Danger": buttonStyle = ButtonStyle.Danger; break;
                case "Success": buttonStyle = ButtonStyle.Success; break;
            }

            panelButtonMessage ??= panelButtonEmmoji + "Create ticket";

            ulong? closeTicketsCategoryId = null;
            if (closeTicketsCategory != null)
                closeTicketsCategoryId = closeTicketsCategory.Id;

            ulong? logChannelId = null;
            if(logChannel != null)
                logChannelId = logChannel.Id;

            // Adding a panel to the database
            Panel panel = new() { Title = panelTitle, Description = panelDescription, ChannelId = panelChannel.Id, ButtonMessage = panelButtonMessage, ButtonColor = panelButtonColor, OpenTicketCategoryId = openTicketsCategory.Id, CloseTicketCategoryId = closeTicketsCategoryId, LogChannelId = logChannelId };
            (bool result, long panelId) = await Database.AddPanelAsync(panel);
            if (!result)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "The panel was not added. Please try again or contact the developer."
                }));
                return;
            }

            DiscordEmbedBuilder embed = new()
            {
                Title = panelTitle,
                Color = DiscordColor.Blurple,
                Description = panelDescription,
                Footer = new() { Text = $"Panel ID - {panelId}", IconUrl = ctx.Client.CurrentUser.AvatarUrl }
            };

            // Panel message
            DiscordMessageBuilder message = new();
            message.AddEmbed(embed);
            message.AddComponents(new DiscordButtonComponent(buttonStyle, $"{panelId}_create_ticket", panelButtonMessage));

            // Send panel message
            try
            {
                await panelChannel.SendMessageAsync(message);
            }
            catch (UnauthorizedException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to access the channel, send messages, embed links or attach files! Please check the permissions."
                }));
                return;
            }
            catch (NotFoundException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = "Something went wrong when trying to send a panel to the Discord channel. Discord channel not found!"
                }));
                return;
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "An error occurred.",
                    Color = DiscordColor.Red,
                    Description = $"Something went wrong when trying to send a panel to the Discord channel.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer."
                }));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Title = "Complete.",
                Color = DiscordColor.Green,
                Description = $"The panel was successfully created and sent to the {panelChannel.Mention}."
            }));

            if (!await Database.CheckStaffs())
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Title = "Warning.",
                    Color = DiscordColor.Yellow,
                    Description = "You have no staff. To add an staff, use the ``/addstaff`` command."
                }));
            }
        }
    }
}
