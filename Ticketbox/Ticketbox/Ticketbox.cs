using DSharpPlus;
using DSharpPlus.SlashCommands;
using Ticketbox.Commands;
using Ticketbox.Config;
using EventHandler = Ticketbox.Models.EventHandler;

namespace Ticketbox.Ticketbox
{
    internal class Ticketbox
    {
        public static DiscordClient Client { get; private set; }
        public SlashCommandsExtension SlashCommands { get; private set; }

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

            SlashCommands = Client.UseSlashCommands();
            SlashCommands.RegisterCommands<CreatePanelCommand>();
            SlashCommands.RegisterCommands<RemovePanelCommand>();
            SlashCommands.RegisterCommands<AddStaffCommand>();
            SlashCommands.RegisterCommands<RemoveStaffCommand>();

            Client.Ready += EventHandler.OnReady;
            Client.ComponentInteractionCreated += EventHandler.OnComponentInteractionCreated;

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
