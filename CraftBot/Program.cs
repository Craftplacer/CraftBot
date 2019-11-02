using CraftBot.Features;
using CraftBot.Features.Special;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Net.WebSocket;

using LiteDB;

using Newtonsoft.Json;

using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CraftBot
{
    internal class Program
    {
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension CommandsNext { get; private set; }
        public static InteractivityExtension Interactivity { get; private set; }
        public static Config Config { get; private set; }
        public static LiteDatabase Database { get; private set; }
        private static FileStream DatabaseStream { get; set; }

        public static Logger Logger { get; } = new Logger();
        private static CancellationTokenSource KeepAlive { get; } = new CancellationTokenSource();

        public static CultureInfo CultureInfo { get; } = CultureInfo.InvariantCulture;

        public static T GetJson<T>(string name, T @default, bool silent = false)
        {
            string fileName = name + ".json";

            if (!silent)
                Logger.Info($"Loading JSON... ({fileName})");

            if (!File.Exists(fileName))
            {
                if (!silent)
                    Logger.Info($"Couldn't find {fileName}, creating new file.");

                string newJson = JsonConvert.SerializeObject(@default, Formatting.Indented);
                File.WriteAllText(fileName, newJson);
            }

            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static async Task Shutdown()
        {
            Logger.Info("Disconnecting...");
            await Client.DisconnectAsync();

            Logger.Info("Closing database...");
            Database.Dispose();
            DatabaseStream.Dispose();

            KeepAlive.Cancel();
        }

        private static async Task Client_Ready(ReadyEventArgs e)
        {
            Logger.Info("Ready!");
            await e.Client.UpdateStatusAsync(new DiscordActivity("Rewritten v3"), userStatus: UserStatus.DoNotDisturb);
        }

        private static void LoadConfig() => Config = GetJson("config", new Config());

        private static void LoadDatabase()
        {
            Logger.Info("Loading database...");

            DatabaseStream = new FileStream(@"data.db", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            Database = new LiteDatabase(DatabaseStream);
        }

        private static void CreateMissingDirectories()
        {
            string[] directories = new[]
            {
                @"cache",
                @"cache\user"
            };

            foreach (var directory in directories)
            {
                var path = Path.GetFullPath(directory);

                if (Directory.Exists(path))
                    continue;

                Logger.Info($"Creating missing directory '{path}'");
                Directory.CreateDirectory(path);
            }
        }

        private static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();

        private static async Task MainAsync(string[] args)
        {
            Logger.Info($"CraftBot v3");

            CreateMissingDirectories();
            LoadConfig();
            LoadDatabase();

            if (Config.DiscordToken == null)
            {
                Logger.Info("Discord token is not set, quitting...");
                return;
            }

            var discordConfig = new DiscordConfiguration()
            {
                Token = Config.DiscordToken,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                ReconnectIndefinitely = true,
                WebSocketClientFactory = WebSocket4NetCoreClient.CreateNew,
                UseInternalLogHandler = true
            };

            Client = new DiscordClient(discordConfig);

            // Add extensions
            var commandNextConfig = new CommandsNextConfiguration()
            {
                CaseSensitive = false,
                StringPrefixes = new[]
                {
                    "cb!"
                }
            };
            CommandsNext = Client.UseCommandsNext(commandNextConfig);
            CommandsNext.CommandErrored += CommandsNext_CommandErrored;

            var interactivityConfig = new InteractivityConfiguration();
            Interactivity = Client.UseInteractivity(interactivityConfig);

            // Add events
            Client.Ready += Client_Ready;

            // Register Commands
            CommandsNext.RegisterCommands<MainCommands>();
            CommandsNext.RegisterCommands<BotCommands>();
            CommandsNext.RegisterCommands<GuildCommands>();
            CommandsNext.RegisterCommands<UserCommands>();
            CommandsNext.RegisterCommands<ModerationCommands>();
            CommandsNext.RegisterCommands<EmojiLeaderboardCommands>();
            CommandsNext.RegisterCommands<BooruCommands>();

            // Add other events and classes
            Shitposting.Add(Client);
            EmojiLeaderboard.Add(Client);
            Quoting.Add(Client);

            Logger.Info("Connecting...");
            await Client.ConnectAsync();

            try
            {
                await Task.Delay(-1, KeepAlive.Token);
            }
            catch (TaskCanceledException)
            {
            }
        }

        private static async Task CommandsNext_CommandErrored(CommandErrorEventArgs e)
        {
            if (e.Exception is CommandNotFoundException)
            {
                return;
            }
            else if (e.Exception is ChecksFailedException checksFailedException)
            {
                var description = new StringBuilder();

                foreach (var failedCheck in checksFailedException.FailedChecks)
                {
                    if (failedCheck is RequirePermissionsAttribute requirePermissionsAttribute)
                    {
                        description.AppendLine("**The bot and/or the user is missing following permissions:**");
                        description.AppendLine(requirePermissionsAttribute.Permissions.ToPermissionString());
                        description.AppendLine();
                    }
                    else if (failedCheck is RequireUserPermissionsAttribute requireUserPermissionsAttribute)
                    {
                        description.AppendLine("**The user is missing following permissions:**");
                        description.AppendLine(requireUserPermissionsAttribute.Permissions.ToPermissionString());
                        description.AppendLine();
                    }
                    else if (failedCheck is RequireBotPermissionsAttribute requireBotPermissionsAttribute)
                    {
                        description.AppendLine("**The bot is missing following permissions:**");
                        description.AppendLine(requireBotPermissionsAttribute.Permissions.ToPermissionString());
                        description.AppendLine();
                    }
                    else if (failedCheck is RequireOwnerAttribute)
                    {
                        description.AppendLine("Requires you to be the bot owner");
                    }
                    else if (failedCheck is RequireGuildAttribute)
                    {
                        description.AppendLine("Requires to be run inside a server");
                    }
                    else if (failedCheck is RequireDirectMessageAttribute)
                    {
                        description.AppendLine("Requires to be run inside direct messages");
                    }
                    else
                    {
                        description.AppendLine(failedCheck.GetType().Name);
                    }
                }

                await e.Context.RespondAsync(embed:
                    new DiscordEmbedBuilder()
                    {
                        Title = "Checks failed",
                        Description = description.ToString(),
                        Color = Colors.Red500
                    }
                );
            }
            else
            {
                string message = e.Exception.ToString();
                bool exceedsMaximum = message.Length > 2040;

                if (exceedsMaximum)
                    message = $"{message.Substring(0, message.Length - 8)}...";

                await e.Context.RespondAsync(embed:
                    new DiscordEmbedBuilder()
                    {
                        Title = "Command errored",
                        Description = $"```{message}```",
                        Color = Colors.Red500
                    }
                );
            }
        }
    }
}