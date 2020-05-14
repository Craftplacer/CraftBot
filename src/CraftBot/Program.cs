using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using CraftBot.Commands;
using CraftBot.Features;
using CraftBot.Localization;
using CraftBot.Repositories;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;

using LiteDB;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentry;

using Timer = System.Timers.Timer;

namespace CraftBot
{
    public partial class Program
    {
        public static event EventHandler Update;

        public static event EventHandler ShuttingDown;

        private static BsonMapper _bsonMapper;
        private static CancellationTokenSource _keepAlive;
        private static CommandsNextExtension _commandsNext;
        private static Config _config;
        private static DiscordClient _client;
        private static GuildRepository _guildRepository;

        // private static IrcIntegration _ircIntegration;
        private static LiteDatabase _database;

        private static LocalizationEngine _localization;
        private static Quoting _quoting;
        private static Timer _updateTimer;
        private static UserRepository _userRepository;
        private static DiscourseFeed _discourseFeed;
        private static IDisposable _sentry;

        public static Statistics Statistics { get; private set; }

        public static void SaveLanguage(Language language)
        {
            var json = language.ToJson();
            var path = Path.Combine("lang", language.Code + ".json");

            File.WriteAllText(path, json);
        }

        public static async Task Shutdown()
        {
            Logger.Info("Disconnecting...");
            await _client.DisconnectAsync();

            Logger.Info("Closing database...");
            _database.Dispose();

            Logger.Info("Invoking update timer...");
            _updateTimer.Close();
            Update?.Invoke(null, null);

            Logger.Info("Raising shutdown event...");
            ShuttingDown?.Invoke(null, null);

            _keepAlive.Cancel();
        }

        private static void CreateMissingDirectories()
        {
            string[] directories =
            {
                @"assets",
                @"cache",
                Path.Combine("cache", "user"),
                Path.Combine("cache", "channel"),
                Path.Combine("cache", "guild"),
                Path.Combine("cache", "attachments"),
                @"lang",
                @"errors"
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

        private static void InitializeLocalizationEngine()
        {
            const string fallback = "en";

            _localization = new LocalizationEngine();

            Logger.Info("Loading available languages...", "Localization Engine");
            foreach (var path in Directory.GetFiles("lang", "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var language = Language.FromJson(_localization, json);
                    _localization.Languages.Add(language);
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to load language at {path}", "Localization Engine");
                    Logger.Error(e, "Localization Engine");
                }
            }

            _localization.FallbackLanguage =
                _localization.Languages.First(l => l.Code.Equals(fallback, StringComparison.OrdinalIgnoreCase));
            Logger.Info($"Loaded {_localization.Languages.Count} languages", "Localization Engine");
        }

        private static void LoadConfig()
        {
            var path = Path.Combine("config", "config");
            _config = Helpers.GetJson(path, new Config());
        }

        private static void LoadDatabase()
        {
            Logger.Info("Loading database...", "Database");

            _bsonMapper = new BsonMapper
            {
                EmptyStringToNull = true
            };

            _database = new LiteDatabase("data.db", _bsonMapper);

            _userRepository = new UserRepository(_database);
            _guildRepository = new GuildRepository(_database);
        }

        private static void StartTimer()
        {
            _updateTimer = new Timer(5 * 60 * 1000)
            {
                AutoReset = true,
                Enabled = true
            };
            _updateTimer.Elapsed += (s, e) => Update?.Invoke(null, null);

            Update += (s, e) =>
            {
                //var today = DateTime.Now;
                //var userDatabase = Database.GetCollection<UserData>();
                //var celebratableUsers = userDatabase.Find(u => u.Birthday.HasValue &&
                //                                               u.Birthday.Value.Day == today.Day &&
                //                                               u.Birthday.Value.Month == today.Month);
                //
                //foreach (var userData in celebratableUsers)
                //{
                //
                //    var user = Client.GetUserAsync();
                //}
            };
            Update += (s, e) => Helpers.SaveJson(Path.Combine("data", "statistics"), Statistics);
            Update += (s, e) =>
            {
                _database.Checkpoint();
                Logger.Info("Created database checkpoint", "CraftBot");
            };
        }

        public static void Main() => MainAsync().GetAwaiter().GetResult();

        public static void SetupExtensions()
        {
            // Add extensions
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(_localization);
            serviceCollection.AddSingleton(_userRepository);
            serviceCollection.AddSingleton(_guildRepository);

            // Setup CommandsNext
            var commandNextConfig = new CommandsNextConfiguration
            {
                CaseSensitive = false,
                StringPrefixes = new[] { "cb!" },
                Services = serviceCollection.BuildServiceProvider()
            };
            _commandsNext = _client.UseCommandsNext(commandNextConfig);
            _commandsNext.CommandErrored += CommandsNext_CommandErrored;
            _commandsNext.CommandExecuted += CommandsNext_CommandExecuted;

            // Setup Interactivity
            var interactivityConfig = new InteractivityConfiguration();
            _client.UseInteractivity(interactivityConfig);

            // Register Commands
            //CommandsNext.SetHelpFormatter<HelpFormatter>();
            _commandsNext.RegisterCommands<MainCommands>();
            _commandsNext.RegisterCommands<BotCommands>();
            _commandsNext.RegisterCommands<GuildCommands>();
            _commandsNext.RegisterCommands<UserCommands>();
            _commandsNext.RegisterCommands<ModerationCommands>();
            _commandsNext.RegisterCommands<BooruCommands>();
            _commandsNext.RegisterCommands<ShitpostingCommands>();
            // _commandsNext.RegisterCommands<IrcCommands>();

            // Add other events and classes
            _quoting = new Quoting();
            _client.AddExtension(_quoting);

            // _ircIntegration = new IrcIntegration();
            // _client.AddExtension(_ircIntegration);

            _discourseFeed = new DiscourseFeed();
            _client.AddExtension(_discourseFeed);
        }

        public static async Task MainAsync()
        {
            Logger.Info("CraftBot v3", "CraftBot");

            CreateMissingDirectories();
            LoadConfig();

            if (string.IsNullOrWhiteSpace(_config.Tokens[TokenType.Discord]))
            {
                Logger.Error("Discord token is not set! Quitting...", "CraftBot");
                return;
            }

            SetupSentry();
            LoadDatabase();
            InitializeLocalizationEngine();

            var statisticsPath = Path.Combine("data", "statistics");
            Statistics = Helpers.GetJson(statisticsPath, new Statistics());

            var discordConfig = new DiscordConfiguration
            {
                Token = _config.Tokens[TokenType.Discord],
                TokenType = DSharpPlus.TokenType.Bot,
                AutoReconnect = true,
                ReconnectIndefinitely = true,
                UseInternalLogHandler = false
            };

            _client = new DiscordClient(discordConfig);

            // Add events
            _client.ClientErrored += ClientOnClientErrored;
            _client.MessageCreated += Client_MessageCreated;
            _client.Ready += Client_Ready;
            _client.SocketClosed += Client_SocketClosed;
            _client.SocketOpened += Client_SocketOpened;
            _client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;

            SetupExtensions();

            StartTimer();

            Logger.Info("Connecting...", "CraftBot");
            await _client.ConnectAsync();

            try
            {
                _keepAlive = new CancellationTokenSource();
                await Task.Delay(-1, _keepAlive.Token);
            }
            catch (TaskCanceledException)
            {
            }

            _sentry?.Dispose();
        }

        private static void SetupSentry()
        {
            var sentryDsn = _config.Tokens[TokenType.Sentry];

            if (string.IsNullOrWhiteSpace(sentryDsn))
                return;

            _sentry = SentrySdk.Init(sentryDsn);
        }
    }
}