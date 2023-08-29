using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketbox.Config;
using Ticketbox.Models;

namespace Ticketbox.Ticketbox
{
    internal class Ticketbox
    {
        public static DiscordClient Client { get; private set; }
        public SlashCommandsExtension Extension { get; private set; }

        public async Task RunBotAsync()
        {
            JSONReader jsonReader = new();
            await jsonReader.ReadJsonAsync();

            Client = new DiscordClient(new DiscordConfiguration()
            {
                Token = jsonReader.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
                AutoReconnect = true
            });

            Client.Ready += Client_Ready;

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Logger.Info("Client is ready!");
            return Task.CompletedTask;
        }
    }
}
