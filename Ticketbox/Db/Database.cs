using Ticketbox.Models;

namespace Ticketbox.Db
{
    internal class Database
    {
        #region [Panel]

        public static async Task<(bool, long)> AddPanelAsync(Panel panel)
        {          
            try
            {
                using PanelsContext db = new();
                await db.Panels.AddAsync(panel);
                await db.SaveChangesAsync();
                return (true, db.Panels.ToList()[^1].Id);
            }
            catch (Exception ex) 
            {
                Logger.Error(ex.Message);
                return (false, 0);
            }
        }

        public static async Task<bool> UpdateTicketCount(long panelId)
        {
            try
            {
                using PanelsContext db = new();
                Panel? panel = await GetPanelAsync(panelId);

                if (panel == null)
                    return false;

                panel.TicketCount++;

                db.Panels.Update(panel);
                await db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return false;
            }
        }

        public static async Task<bool> RemovePanel(Panel panel)
        {
            try
            {
                using PanelsContext db = new();
                await Task.Run(() => db.Panels.Remove(panel));
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }

        public static async Task<Panel?> GetPanelAsync(long panelId)
        {
            try
            {
                using PanelsContext db = new();
                List<Panel> panels = new();
                await Task.Run(() => panels = db.Panels.ToList());
                for(int i = 0; i < panels.Count; i++)
                {
                    if (panels[i].Id == panelId)
                    {
                        return panels[i];
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return null;
            }
        }

        #endregion

        #region [Staff]

        public static async Task<bool> AddStaff(Staff staff)
        {
            try
            {
                using StaffContext db = new();
                await db.Staffs.AddAsync(staff);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return false;
            }
        }

        public static async Task<bool> RemoveStaff(ulong memberId)
        {
            try
            {
                using StaffContext db = new();
                Staff? staff = null;
                await Task.Run(() => staff = db.Staffs.ToList().Find(m => m.MemberId == memberId));

                if (staff == null)
                    return false;

                db.Staffs.Remove(staff);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return false;
            }
        }

        public static async Task<List<Staff>?> GetAllStaffAsync()
        {
            List<Staff>? staffs = null;
            try
            {
                using StaffContext db = new();
                await Task.Run(() => staffs = db.Staffs.ToList());
                return staffs;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return null;
            }
        }

        public static async Task<bool> CheckStaffs()
        {
            List<Staff>? staffs = await GetAllStaffAsync();
            if (staffs == null || staffs.Count == 0)
                return false;

            return true;
        }


        #endregion

        #region [Tickets]

        public static async Task<bool> AddTicketAsync(Ticket ticket)
        {
            try
            {
                using TicketsContext db = new();
                await db.Tickets.AddAsync(ticket);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return false;
            }
        }

        public static async Task<List<Ticket>?> GetAllTicketsAsync()
        {
            List<Ticket>? tickets = null;
            try
            {
                using TicketsContext db = new();
                await Task.Run(() => tickets = db.Tickets.ToList());

                if(tickets == null)
                    return null;

                return tickets;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return null;
            }
        }

        public static async Task<Ticket?> GetTicketAsync(ulong channelId)
        {
            Ticket? ticket = null;
            try
            {
                using TicketsContext db = new();
                await Task.Run(() => ticket = db.Tickets.ToList().Find(t => t.ChannelId == channelId));
                 
                if(ticket == null)
                    return null;

                return ticket;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return null;
            }
        }



        public static async Task<Ticket?> GetArchiveTicketAsync(ulong channelId)
        {
            Ticket? ticket = null;
            try
            {
                using TicketsArchiveContext db = new();
                await Task.Run(() => ticket = db.TicketArchive.ToList().Find(t => t.ChannelId == channelId));

                if (ticket == null)
                    return null;

                return ticket;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return null;
            }
        }

        public static async Task<bool> ArchiveTicket(Ticket ticket)
        {
            try
            {
                using TicketsArchiveContext db = new();
                db.TicketArchive.Add(ticket);
                await db.SaveChangesAsync();

                using TicketsContext db1 = new();
                db1.Tickets.Remove(ticket);
                await db1.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return false;
            }
        }

        public static async Task<bool> UnarchiveTicket(Ticket ticket)
        {
            try
            {
                using TicketsContext db = new();
                db.Tickets.Add(ticket);
                await db.SaveChangesAsync();

                using TicketsArchiveContext db1 = new();
                db1.TicketArchive.Remove(ticket);
                await db1.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return false;
            }
        }

        #endregion

        #region random online staff
        //public static async Task<Staff?> GetRandomOnlineStaffAsync(ulong guildId)
        //{
        //    List<Staff>? staffs = await GetAllStaffAsync();

        //    if (staffs == null)
        //        return null;

        //    if (staffs.Count <= 0)
        //        return null;

        //    DiscordGuild guild;
        //    try
        //    {
        //        guild = await Ticketbox.Ticketbox.Client.GetGuildAsync(guildId);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex.ToString());
        //        return null;
        //    }

        //    List<Staff> onlineStaffs = new();

        //    for (int i = 0; i < staffs.Count; i++)
        //    {
        //        DiscordMember staff;
        //        try
        //        {
        //            staff = await guild.GetMemberAsync(staffs[i].MemberId);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error(ex.ToString());
        //            return null;
        //        }

        //        DiscordPresence? presence = staff.Presence;
        //        if(presence != null)
        //        {
        //            if(presence.Status != UserStatus.Offline)
        //            {
        //                onlineStaffs.Add(staffs[i]);
        //            }
        //        }

        //        //if(status == null)
        //        //    return null;              
        //    }

        //    if (onlineStaffs.Count == 0)
        //        return null;

        //    Random rnd = new();
        //    return onlineStaffs[rnd.Next(0, staffs.Count - 1)];
        //}
        #endregion
    }
}
