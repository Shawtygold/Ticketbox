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
        [SlashCommand("send_panel", "Sends a panel for interaction to be able to create a ticket.")]
        public static async Task OnExecute(InteractionContext ctx, 
            [Option("title","Panel Title")][MaximumLength(50)] string panelTitle,
            [Option("channel", "Discord channel where the panel will be sent to.")] DiscordChannel panelChannel,
            [Option("tickets_support_role", "Role that, by default, will have access to tickets")] DiscordRole supportRole,
            [Option("open_tickets_category", "Discord category where tickets will be located.")] DiscordChannel ticketsCategory,
            [Option("log_channel", "Discord channel where logs will be sent.")] DiscordChannel? logChannel = null,
            [Option("transcript_channel", "Discord channel where the transcripts will be sent.")] DiscordChannel? transcriptChannel = null,
            [Option("tickets_support_role2", "Role that, by default, will have access to tickets")] DiscordRole? supportRole2 = null,
            [Option("tickets_support_role3", "Role that, by default, will have access to tickets")] DiscordRole? supportRole3 = null,
            [Option("tickets_support_role4", "Role that, by default, will have access to tickets")] DiscordRole? supportRole4 = null,
            [Option("tickets_support_role5", "Role that, by default, will have access to tickets")] DiscordRole? supportRole5 = null,
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
                    Color = DiscordColor.Red,
                    Description = "Insufficient permissions. You need **Administrator** permission for this command."
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
                    Color = DiscordColor.Red,
                    Description = "Server Error Exception. Please, try again or contact the developer."
                }));
                return;
            }

            if (!PermissionsManager.CheckPermissionsIn(bot, ctx.Channel, new() { Permissions.AccessChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "I don't have access to this channel! Please, check the permissions."
                }));
                return;
            }

            if(!PermissionsManager.CheckPermissions(bot, new() { Permissions.SendMessages, Permissions.EmbedLinks, Permissions.AttachFiles, Permissions.ManageRoles, Permissions.ManageChannels }))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Maybe I'm not allowed to send messages, embed links, attach files, manage roles or manage channels. Please check the permissions."
                }));
                return;
            }

            DiscordEmoji defaultButtonEmoji = DiscordEmoji.FromName(ctx.Client, ":envelope_with_arrow:");
            panelButtonEmmoji ??= defaultButtonEmoji;
            panelDescription ??= "To create a ticket react with" + defaultButtonEmoji;

            DiscordEmbedBuilder embed = new()
            {
                Title = panelTitle,
                Color = DiscordColor.Blurple,
                Description = panelDescription,
                Footer = new() { Text = "Ticketbox", IconUrl = ctx.Client.CurrentUser.AvatarUrl }
            };

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

            ulong? logChannelId = null;
            if(logChannel != null)
                logChannelId = logChannel.Id;

            ulong? transcriptChannelId = null;
            if(transcriptChannel != null) 
                transcriptChannelId = transcriptChannel.Id;

            // Adding a panel to the database
            Panel panel = new() { Title = panelTitle, Description= panelDescription, ChannelId= panelChannel.Id,  ButtonMessage =panelButtonMessage, ButtonColor= panelButtonColor,  TicketCategoryId = ticketsCategory.Id, LogChannelId = logChannelId,  TranscriptChannelId =transcriptChannelId };
            (bool result, long panelId) = await Database.AddPanelAsync(panel);
            if (!result)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "An error occurred.",
                    Description = "Something went wrong. The panel was not added. Please try again or contact the developer."
                }));
            }

            // Getting support roles
            List<DiscordRole?> supportRoles = new() { supportRole, supportRole2, supportRole3, supportRole4, supportRole5 };
            List<DiscordRole> supportRolesNotNull = new();
            for (int i = 0; i < supportRoles.Count; i++)
            {
                if (supportRoles[i] != null)
                {
                    supportRolesNotNull.Add(supportRoles[i]);
                }
            }

            // Get all members
            IReadOnlyCollection<DiscordMember> allMembers;
            try
            {
                allMembers = await ctx.Guild.GetAllMembersAsync();
            }
            catch (ServerErrorException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = "Server Error Exception. Please, try again or contact the developer."
                }));
                return;
            }

            // Get staffs
            List<DiscordMember> staffs = new();

            foreach (var member in allMembers)
            {
                for (int j = 0; j < supportRolesNotNull.Count; j++)
                {
                    if (member.Roles.Any(r => r.Id == supportRolesNotNull[j].Id))
                        staffs.Add(member);
                }
            }

            // Adding staffs to the database
            for (int i = 0; i < staffs.Count; i++)
            {
                if (!await Database.AddStaff(new Staff() { MemberId = staffs[i].Id, Name = staffs[i].DisplayName, GuildId = ctx.Guild.Id }))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Red,
                        Title = "An error occurred.",
                        Description = "Something went wrong. The staff was not added. Please try again or contact the developer."
                    }));
                    return;
                }
            }

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
                    Color = DiscordColor.Red,
                    Title = "An error occurred.",
                    Description = "Hmm, something went wrong. Maybe I'm not allowed to access the channel, send messages, embed links, attach files, manage roles or manage channels! Please, check the permissions."
                }));
                return;
            }
            catch (NotFoundException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "An error occurred.",
                    Description = "Hmm, something went wrong when trying to send a panel to the Discord channel. Discord channel not found!"
                }));
                return;
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "An error occurred.",
                    Description = $"Hmm, something went wrong when trying to send a panel to the Discord channel.\n\nThis was Discord's response:\n> {ex.Message}\n\nIf you would like to contact the bot owner about this, please include the following debugging information in the message:\n```{ex}\n```"
                }));
                Logger.Error(ex.ToString());
                return;
            }

            // Log
            if(logChannel != null)
            {
                await logChannel.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Green,
                    Description = $"Panel **{panelTitle}** was successfully created and sent to the {panelChannel.Mention}!"
                });
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Green,
                Description = $"The panel was successfully created and sent to the {panelChannel.Mention}."
            }));
        }
    }
}
