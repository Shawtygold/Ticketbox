using Ticketbox.Models;

namespace Ticketbox.Db
{
    internal class Database
    {
        public static async Task<(bool, long)> AddPanelAsync(Panel panel)
        {          
            try
            {
                using ApplicationContext db = new();
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

        public static async Task<Panel?> GetPanelById(long panelId)
        {
            try
            {
                using ApplicationContext db = new();
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

        public static async Task<bool> AddStaff(Staff staff)
        {
            try
            {
                using ApplicationContext db = new();
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

        //public static async Task<bool> AddCategoryAsync(Category category)
        //{
        //    try
        //    {
        //        using ApplicationContext db = new();
        //        await db.Categories.AddAsync(category);
        //        await db.SaveChangesAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex.ToString());
        //        return false;
        //    }
        //}

        //public static async Task<bool> RemoveCategoryAsync(Category category)
        //{
        //    try
        //    {
        //        using ApplicationContext db = new();
        //        await Task.Run(() => db.Categories.Remove(category));
        //        await db.SaveChangesAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex.ToString());
        //        return false;
        //    }
        //}

        //public static async Task<Category?> GetCategoryAsync(ulong guildId)
        //{
        //    try
        //    {
        //        using ApplicationContext db = new();
        //        Category? category = null;
        //        await Task.Run(() => category = db.Categories.ToList().Find(c => c.GuildId == guildId));

        //        if (category == null)
        //            return null;

        //        return category;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex.ToString());
        //        return null;
        //    }
        //}

        public static async Task<List<Ticket>?> GetTicketsFromAsync(ulong guildId)
        {
            try
            {
                using ApplicationContext db = new();
                List<Ticket>? tickets = null;
                await Task.Run(() => tickets = db.Tickets.ToList().FindAll(t => t.GuildId == guildId));

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

        public static async Task<(bool, long Id)> AddTicketAsync(Ticket ticket)
        {
            try
            {
                using ApplicationContext db = new();
                await db.Tickets.AddAsync(ticket);
                await db.SaveChangesAsync();
                long ticketId = db.Tickets.ToList()[^1].Id;
                return (true, ticketId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return (false, 0);
            }
        }

        public static async Task<Staff?> GetRandomStaffAsync()
        {
            List<Staff> staffs = new();
            try
            {
                using ApplicationContext db = new();
                await Task.Run(() => staffs = db.Staffs.ToList());
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return null;
            }

            if(staffs.Count <= 0)
                return null;

            Random rnd = new();
            return staffs[rnd.Next(0, staffs.Count - 1)];         
        }
    }
}
