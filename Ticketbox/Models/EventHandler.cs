using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
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

            if (args.Id.Contains("create_ticket"))
            {
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

                Panel? panel = await Database.GetPanelById(panelId);
                if (panel == null)
                {
                    Logger.Warn("Panel not found!");
                    return;
                }

                DiscordChannel? logChannel = null;
                if (panel.LogChannelId != null)
                {
                    try
                    {
                        logChannel = args.Guild.GetChannel((ulong)panel.LogChannelId);
                    }
                    catch (ServerErrorException)
                    {
                        Logger.Error("Server Error Exception. Please, try again or contact the developer.");
                        return;
                    }
                }

                DiscordMember member = (DiscordMember)args.User;

                (bool result, string message) = await OpenNewTicketAsync(member.Id, panel.TicketCategoryId, logChannel);
                if (result)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Green,
                        Description = message
                    }));
                }
                else
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Color = DiscordColor.Red,
                        Description = message
                    }));
                }
            }        
        }

        internal static async Task<(bool,  string)> OpenNewTicketAsync(ulong userId, ulong categoryId, DiscordChannel? logChannel)
        {
            DiscordChannel category;
            try
            {
                category = await Ticketbox.Ticketbox.Client.GetChannelAsync(categoryId);
            }
            catch (Exception) 
            {
                return (false, "Could not find the category to place the ticket in.");
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
                ticket = await category.Guild.CreateChannelAsync("ticket", ChannelType.Text, parent: category);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to manage channels! Please, check the permissions.");
            }  
            catch (NotFoundException)
            {
                return (false, "Category not found.");
            }
            catch (Exception ex)
            {
                return (false, $"{ex.Message}.");
            }

            // Making the channel private
            try
            {
                await ticket.AddOverwriteAsync(category.Guild.EveryoneRole, deny: Permissions.AccessChannels);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to manage channels or manage roles! Please, check the permissions.");
            }
            catch (Exception ex)
            {
                return (false, $"{ex.Message}.");
            }

            // Get random staff
            Staff? randomStaff = await Database.GetRandomStaffAsync();
            if (randomStaff == null)
            {
                return (false, "Staff not found!");
            }

            DiscordMember staff;
            try
            {
                staff = await category.Guild.GetMemberAsync(randomStaff.MemberId);
            }
            catch
            {
                return (false, "Could not find sstaff on the Discord server.");
            }

            // Add ticket to database
            (bool result, long ticketId) = await Database.AddTicketAsync(new Ticket() { CreatorId = member.Id, AssignedStaffId = staff.Id, ChannelId = ticket.Id, GuildId = ticket.Guild.Id });
            if (!result)
            {
                return (false, "An error occurred when adding a ticket to the database.");
            }

            // Renaming ticket
            try
            {
                await ticket.ModifyAsync(t => t.Name = "Ticket-" + ticketId);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to manage channels! Please, check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Ticket channel not found.");
            }
            catch (Exception ex)
            {
                return (false, $"{ex.Message}.");
            }

            // I give staff access to the ticket
            try
            {
                await ticket.AddOverwriteAsync(staff, allow: Permissions.AccessChannels);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to manage channels or manage roles! Please, check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Could not find you on the Discord server.");
            }
            catch (Exception ex)
            {
                return (false, $"{ex.Message}.");
            }

            //I give member access to the ticket
            try
            {
                await ticket.AddOverwriteAsync(member, allow: Permissions.AccessChannels);
            }
            catch (UnauthorizedException)
            {
                return (false, "Maybe I'm not allowed to manage channels or manage roles! Please, check the permissions.");
            }
            catch (NotFoundException)
            {
                return (false, "Could not find you on the Discord server.");
            }
            catch (Exception ex)
            {
                return (false, $"{ex.Message}.");
            }

            // Log
            if (logChannel != null)
            {
                await logChannel.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Green,
                    Description = "Ticket " + ticket.Mention + " opened by " + member.Mention + ".\n",
                    Footer = new DiscordEmbedBuilder.EmbedFooter { Text = "Ticket " + ticket.Id }
                });
            }

            return (true, "Ticket opened, " + member.Mention + "!\n" + ticket.Mention);
        }
    }
}
