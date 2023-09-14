using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Ticketbox.Db;

namespace Ticketbox.Models
{
    internal class EventHandler
    {
        internal static Task OnReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Logger.Info("Client is ready!");
            return Task.CompletedTask;
        }

        internal async static Task OnComponentInteractionCreated(DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs args)
        {
            await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            DiscordMember member = (DiscordMember)args.User;

            // I get the id of the panel from which the button was pressed (the ID is located in the button ID)
            long panelId;
            try
            {
                int index = args.Id.IndexOf('_');
                panelId = Convert.ToUInt32(args.Id.Substring(0, index));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return;
            }

            Panel? panel = await Database.GetPanelAsync(panelId);
            if (panel == null)
            {
                Logger.Error("Panel not found!");
                return;
            }

            if (args.Id.Contains("create_ticket"))
            {

                (bool result, string message) = await OpenNewTicketAsync(member.Id, panel, args.Interaction);
                if (!result)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred.",
                        Color = DiscordColor.Red,
                        Description = message
                    }));
                }
            }
            else if (args.Id.Contains("close_ticket"))
            {
                // Getting a ticket channel
                DiscordChannel ticketChannel = args.Channel;

                (bool result, string message) = await CloseTicketAsync(ticketChannel, member, panel);
                if (!result)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred.",
                        Color = DiscordColor.Red,
                        Description = message
                    }));
                    return;
                }

                (bool result1, string message1) = await SendModeratorMessage(ticketChannel, panel.Id);
                if (!result1)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred.",
                        Color = DiscordColor.Red,
                        Description = message1
                    }));
                    return;
                }
            }
            else if (args.Id.Contains("open_ticket"))
            {
                // Getting a ticket channel
                DiscordChannel ticketChannel = args.Channel;

                (bool result, string message) = await ReOpenTicketAsync(ticketChannel, member, panel);
                if (!result)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred.",
                        Color = DiscordColor.Red,
                        Description = message
                    }));
                    return;
                }
            }
            else if (args.Id.Contains("delete_ticket"))
            {
                // Getting a ticket channel
                DiscordChannel ticketChannel = args.Channel;

                (bool result, string message) = await DeleteTicketAsync(ticketChannel, member, panel);
                if (!result)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "An error occurred.",
                        Color = DiscordColor.Red,
                        Description = message
                    }));
                    return;
                }
            }
        }

        internal static async Task<(bool, string)> OpenNewTicketAsync(ulong userId, Panel panel, DiscordInteraction ctx)
        {
            DiscordChannel category;
            try
            {
                category = await Ticketbox.Ticketbox.Client.GetChannelAsync(panel.OpenTicketCategoryId);
            }
            catch (Exception)
            {
                return (false, "Could not find a category in which to place the ticket.");
            }

            DiscordMember member;
            try
            {
                member = await category.Guild.GetMemberAsync(userId);
            }
            catch
            {
                return (false, "Could not find you on the Discord server.");
            }

            DiscordChannel ticket;
            try
            {
                ticket = await category.Guild.CreateChannelAsync($"ticket-{panel.TicketCount:0000}", ChannelType.Text, parent: category);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to manage channels! Please, check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Category for open tickets not found.");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong trying to get a category for open tickets.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }  

            // Making the channel private
            try
            {
                await ticket.AddOverwriteAsync(category.Guild.EveryoneRole, deny: Permissions.AccessChannels);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket, manage roles or manage channels! Please check the permissions.");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when I tried to make the ticket private.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            if(!await Database.AddTicketAsync(new Ticket() { CreatorId = member.Id, ChannelId = ticket.Id, CategoryId = ticket.Parent.Id }))
            {
                return (false, "Ticket was not added to the database! Please try again or contact the developer.");
            }              

            if(!await Database.UpdateTicketCount(panel.Id))
            {
                return (false, "An error occurred while updating the number of tickets in the database! Please try again or contact the developer.");
            }

            // Welcome message
            DiscordMessageBuilder message = new();
            DiscordEmoji emoji = DiscordEmoji.FromName(Ticketbox.Ticketbox.Client, ":lock:");
            message.AddEmbed(new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Blurple,
                Description = $"Support will be with you shortly",
                Footer = new() { Text = "Ticketbox", IconUrl = Ticketbox.Ticketbox.Client.CurrentUser.AvatarUrl }
            });
            message.AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, $"{panel.Id}_close_ticket", $"{emoji}Close"));

            // Send message to the ticket
            try
            {
                await ticket.SendMessageAsync(message.WithContent($"{member.Mention} Welcome"));
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket or send messages! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Something went wrong when trying to send a message to the ticket. Discord channel not found!");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to send a message to the ticket channel.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }           

            // Get staffs
            List<Staff>? staffs = await Database.GetAllStaffAsync();
            if (staffs == null)
            {
                return (false, "Could not find staff due to a database error! Please try again or contact the developer.");
            }               

            if (staffs.Count > 0)
            {
                // I give staff access to the ticket
                for (int i = 0; i < staffs.Count; i++)
                {
                    DiscordMember staffMember;
                    try
                    {
                        staffMember = await category.Guild.GetMemberAsync(staffs[i].MemberId);
                    }
                    catch
                    {
                        return (false, "Could not find staff on the Discord server.");
                    }

                    try
                    {
                        await ticket.AddOverwriteAsync(staffMember, allow: Permissions.AccessChannels);
                    }
                    catch (UnauthorizedException)
                    {
                        return (false, "Maybe I'm not allowed to access the ticket, manage roles or manage channels! Please check the permissions.");
                    }
                    catch (NotFoundException)
                    {
                        return (false, "The channel or employee who needs to be granted access to the ticket is not found.");
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Something went wrong when trying to give the staff access to the ticket.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
                    }
                }
            }

            //I give member access to the ticket
            try
            {
                await ticket.AddOverwriteAsync(member, allow: Permissions.AccessChannels);
            }
            catch (UnauthorizedException)
            {              
                return (false, "Maybe I'm not allowed to access the ticket, manage roles or manage channels! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "The channel or member who needs to be granted access to the ticket was not found.");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to give a member access to the ticket.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            // Create follow up message
            await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"✔ Ticket Created {ticket.Mention}").AsEphemeral());

            // Log
            if (panel.LogChannelId != null)
            {
                DiscordChannel logChannel;
                try
                {
                    logChannel = category.Guild.GetChannel((ulong)panel.LogChannelId);
                }
                catch
                {
                    return (false, "Could not find the log channel.");
                }

                try
                {
                    await logChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Green,
                        Description = "Ticket " + ticket.Mention + " opened by " + member.Mention + ".\n",
                        Footer = new() { Text = "Ticket " + ticket.Id }
                    });
                }
                catch (UnauthorizedException)
                {
                    return (false, "Maybe I'm not allowed to access the logs channel or send messages! Please check the permissions.");
                }
                catch (NotFoundException)
                {
                    return (false, "Something went wrong when trying to send a message to the log channel. Discord channel not found!");
                }
                catch (Exception ex)
                {
                    return (false, $"Something went wrong when trying to send a message to the log channel.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
                }
            }

            return (true, "");
        }

        internal static async Task<(bool, string)> CloseTicketAsync(DiscordChannel ticket, DiscordMember member, Panel panel)
        {
            Ticket? ticket1 = await Database.GetTicketAsync(channelId: ticket.Id);

            if (ticket1 == null)
            {
                return (false, "Could not find a ticket due to a database error! Please try again or contact the developer.");
            }

            if(ticket1.IsClosed == true)
            {
                return (false, "The ticket's already closed.");
            }

            string ticketName = ticket.Name.Replace("ticket", "closed");

            try
            {
                await ticket.ModifyAsync(a => a.Name = ticketName);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket or manage channels! Please check the permissions.");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when I tried to change the ticket name.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            try
            {
                await ticket.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Yellow,
                    Description = $"Ticket Closed by {member.Mention}"
                });
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket or send messages! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Something went wrong when trying to send a message to the ticket. Discord channel not found!");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to send message to the ticket channel.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            //Deny access to the channel to the user
            try
            {
                await ticket.AddOverwriteAsync(member, deny: Permissions.AccessChannels);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket, manage roles or manage channels! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "The channel or member who needs to be denied access to the ticket was not found.");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to remove ticket access from a member.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            // Moving the channel to another category if it exists
            if (panel.CloseTicketCategoryId != null)
            {
                DiscordChannel closeTicketCategory;
                try
                {
                    closeTicketCategory = await Ticketbox.Ticketbox.Client.GetChannelAsync((ulong)panel.CloseTicketCategoryId);
                }
                catch
                {
                    return (false, $"Could not find a category in which to place a closed ticket.");
                }

                try
                {
                    await ticket.ModifyPositionAsync(closeTicketCategory.Position, parentId: closeTicketCategory.Id);
                }
                catch (UnauthorizedException)
                {
                    return (false, "Maybe I'm not allowed to access the ticket or manage channels! Please check the permissions.");
                }
                catch (Exception ex)
                {
                    return (false, $"Something went wrong when trying to move the channel to another category.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
                }

                ticket1.CategoryId = closeTicketCategory.Id;
            }

            ticket1.IsClosed = true;

            if (!await Database.ArchiveTicket(ticket1))
            {
                return (false, "Failed to archive a ticket due to a database error. Try again or contact the developer.");
            }                

            // Log          
            if (panel.LogChannelId != null)
            {
                DiscordChannel? logChannel;
                try
                {
                    logChannel = ticket.Guild.GetChannel((ulong)panel.LogChannelId);
                }
                catch
                {
                    return (false, "Could not find the log channel.");
                }

                try
                {
                    await logChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Green,
                        Description = $"Ticket {ticket.Mention} closed by {member.Mention}",
                        Footer = new() { Text = "Ticket " + ticket.Id }
                    });
                }
                catch (UnauthorizedException)
                {
                    return (false, "Maybe I'm not allowed to access the logs channel or send messages! Please check the permissions.");
                }
                catch (NotFoundException)
                {
                    return (false, "Something went wrong when trying to send a message to the log channel. Discord channel not found!");
                }
                catch (Exception ex)
                {
                    return (false, $"Something went wrong when trying to send a message to the log channel.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
                }
            }
            return (true, "");
        }

        internal static async Task<(bool, string)> ReOpenTicketAsync(DiscordChannel ticket, DiscordMember member, Panel panel)
        {
            Ticket? archiveTicket = await Database.GetArchiveTicketAsync(ticket.Id);

            if (archiveTicket == null)
            {
                return (false, "Something went wrong when I tried to reopen the ticket. Ticket not found.");
            }  
            
            if(archiveTicket.IsClosed == false)
            {
                return (false, "The ticket's already open.");
            }

            IReadOnlyList<DiscordMessage> list = await ticket.GetMessagesAsync();
            List<DiscordMessage> messages = list.ToList();
            DiscordMessage? moderateMessage = messages.Find(m => m.Author.Id == Ticketbox.Ticketbox.Client.CurrentUser.Id && m.Embeds.Any(e => e.Description.Contains("Support")));
            
            if (moderateMessage == null)
            {
                return (false, "Could not find the moderator's message.");
            }

            try
            {
                await moderateMessage.DeleteAsync();
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket or manage messages! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Could not find the moderator's message.");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to delete a moderator's message.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            DiscordChannel ticket11 = ticket.Guild.GetChannel(ticket.Id);

            string ticketName = ticket.Name.Replace("closed", "ticket");
            try
            {
                await ticket11.ModifyAsync(a => a.Name = ticketName);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket or manage channels! Please check the permissions.");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when I tried to change the ticket name.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            // If the ticket is in the category for closed tickets
            if (archiveTicket.CategoryId == panel.CloseTicketCategoryId)
            {
                DiscordChannel openTicketCategory;
                try
                {
                    openTicketCategory = await Ticketbox.Ticketbox.Client.GetChannelAsync(panel.OpenTicketCategoryId);
                }
                catch
                {
                    return (false, $"Could not find a category to place the reopened ticket in.");
                }

                try
                {
                    await ticket.ModifyPositionAsync(openTicketCategory.Position, parentId: openTicketCategory.Id);
                }
                catch (UnauthorizedException)
                {
                    return (false, "Maybe I'm not allowed to access the ticket or manage channels! Please check the permissions.");
                }
                catch (Exception ex)
                {
                    return (false, $"Something went wrong when trying to move the channel to the category for open tickets.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
                }
            }

            archiveTicket.IsClosed = false;

            if (!await Database.UnarchiveTicket(archiveTicket))
            {
                return (false, "Failed to reopen ticket due to database error. Please try again or contact the developer.");
            }

            try
            {
                await ticket.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Green,
                    Description = $"Ticket Opened by {member.Mention}"
                });
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket or send messages! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Something went wrong when trying to send message to the ticket. Discord channel not found!");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to send message to the ticket.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            try
            {
                await ticket.AddOverwriteAsync(member, allow: Permissions.AccessChannels);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket, manage roles or manage channels! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "The channel or member who needs to be granted access to the ticket was not found.");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to give a member access to the ticket.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            // Log
            if (panel.LogChannelId != null)
            {
                DiscordChannel logChannel;
                try
                {
                    logChannel = ticket.Guild.GetChannel((ulong)panel.LogChannelId);
                }
                catch
                {
                    return (false, "Could not find the log channel.");
                }

                try
                {
                    await logChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Green,
                        Description = "Ticket " + ticket.Mention + " opened by " + member.Mention + ".\n",
                        Footer = new() { Text = "Ticket " + ticket.Id }
                    });
                }
                catch (UnauthorizedException)
                {
                    return (false, "Maybe I'm not allowed to access the logs channel or send messages! Please check the permissions.");
                }
                catch (NotFoundException)
                {
                    return (false, "Something went wrong when trying to send a message to the log channel. Discord channel not found!");
                }
                catch (Exception ex)
                {
                    return (false, $"Something went wrong when trying to send a message to the log channel.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
                }
            }

            return (true, "");
        }

        internal static async Task<(bool, string)> DeleteTicketAsync(DiscordChannel ticket, DiscordMember member, Panel panel)
        {
            try
            {
                await ticket.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Description = $"Ticket will be deleted in a few seconds"
                });
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to send messages! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Something went wrong when trying to send message to the ticket. Discord channel not found!");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to send message to the ticket.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            try
            {
                await ticket.DeleteAsync();
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket or manage channels! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Something went wrong when I tried to delete the ticket. Discord channel not found!");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when I tried to delete the ticket.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            // Log
            if (panel.LogChannelId != null)
            {
                DiscordChannel? logChannel;
                try
                {
                    logChannel = ticket.Guild.GetChannel((ulong)panel.LogChannelId);
                }
                catch
                {
                    return (false, "Could not find the log channel.");
                }

                try
                {
                    await logChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Green,
                        Description = $"Ticket {ticket.Mention} deleted by {member.Mention}",
                        Footer = new() { Text = "Ticket " + ticket.Id }
                    });
                }
                catch (UnauthorizedException)
                {
                    return (false, "Maybe I'm not allowed to access the logs channel or send messages! Please check the permissions.");
                }
                catch (NotFoundException)
                {
                    return (false, "Something went wrong when trying to send a message to the log channel. Discord channel not found!");
                }
                catch (Exception ex)
                {
                    return (false, $"Something went wrong when trying to send a message to the log channel.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
                }
            }

            return (true, "");
        }

        internal static async Task<(bool, string)> SendModeratorMessage(DiscordChannel ticket, long panelId)
        {
            DiscordEmbedBuilder embed = new()
            {
                Color = DiscordColor.Gray,
                Description = "```Support team ticket controls```"
            };

            var openButton = new DiscordButtonComponent(ButtonStyle.Secondary, $"{panelId}_open_ticket", $"{DiscordEmoji.FromName(Ticketbox.Ticketbox.Client, ":unlock:")}Open");
            var deleteButton = new DiscordButtonComponent(ButtonStyle.Secondary, $"{panelId}_delete_ticket", $"{DiscordEmoji.FromName(Ticketbox.Ticketbox.Client, ":no_entry:")}Delete");

            DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbed(embed);
            messageBuilder.AddComponents(openButton, deleteButton);

            try
            {
                await ticket.SendMessageAsync(messageBuilder);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to access the ticket or send messages! Please check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Something went wrong when trying to send a moderator message to the ticket. Discord channel not found!");
            }
            catch (Exception ex)
            {
                return (false, $"Something went wrong when trying to send a moderator message to the ticket.\n\nThis was Discord's response:\n```{ex.Message}```\nPlease try again or contact the developer.");
            }

            return (true, "");
        }
    }
}
