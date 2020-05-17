using System;
using System.Threading.Tasks;
using CraftBot.Discord;
using CraftBot.IRC;
using Sentry;

namespace CraftBot
{
    public partial class Program
    {
        private static BotManager _manager;
        
        public static void Main() => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            
            Logger.Info("CraftBot v3", "CraftBot");

            _manager = new BotManager();
            _manager.AddBot(new DiscordBot(_manager, _manager.CommandService));
            // _manager.AddBot(new IrcBot(_manager, _manager.CommandService));

            Logger.Info($"{_manager.Bots.Count} sub-bots available", "CraftBot");

            await _manager.StartAsync();
        }

        private static async void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;

            if (_manager == null)
            {
                Logger.Warning("Bots haven't initialized yet.");
                return;
            }
            
            await _manager.StopAsync();
        }
    }
}