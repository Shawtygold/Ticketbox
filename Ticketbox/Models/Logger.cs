using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ticketbox.Ticketbox;

namespace Ticketbox.Models
{
    internal class Logger
    {

        public static void Debug(string message)
        {
            try
            {
                Ticketbox.Ticketbox.Client.Logger.Log(LogLevel.Debug, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), message);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
        }

        public static void Info(string message)
        {
            try
            {
                Ticketbox.Ticketbox.Client.Logger.Log(LogLevel.Information, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), message);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine($"[INFO] {message}");
            }
        }

        public static void Warn(string message)
        {
            try
            {
                Ticketbox.Ticketbox.Client.Logger.Log(LogLevel.Warning, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), message);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine($"[WARNING] {message}");
            }
        }

        public static void Error(string message)
        {
            try
            {
                Ticketbox.Ticketbox.Client.Logger.Log(LogLevel.Error, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), message);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine($"[ERROR] {message}");
            }
        }

        public static void Fatal(string message)
        {
            try
            {
                Ticketbox.Ticketbox.Client.Logger.Log(LogLevel.Critical, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), message);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine($"[CRITICAL] {message}");
            }
        }
    }
}
