using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CraftBot.Common.Commands;
using CraftBot.Localization;
using LiteDB;
using Qmmands;
using Sentry;
using Timer = System.Timers.Timer;

namespace CraftBot
{
	/// <summary>
	///     Class that manages all bots and includes base functionality for things like config, error reporting and so on.
	/// </summary>
	public class BotManager : IStartable
	{
		private readonly List<SubBot> _bots;
		private readonly CancellationTokenSource _keepAlive;
		private BsonMapper _bsonMapper;
		private IDisposable _sentry;
		private Timer _updateTimer;

		public BotManager()
		{
			_bots = new List<SubBot>();
			_keepAlive = new CancellationTokenSource();

			CommandService = new CommandService();
			CommandService.AddModule<CommandModule>();
		}
		
		public CommandService CommandService { get; }
		
		public LiteDatabase Database { get; private set; }
		
		public LocalizationEngine Localization { get; private set; }

		public IReadOnlyCollection<SubBot> Bots => _bots;

		public Config Config { get; private set; }

		public Statistics Statistics { get; private set; }

		public void AddBot(SubBot bot)
		{
			_bots.Add(bot);
		}

		public async Task StartAsync()
		{
			if (!_bots.Any())
				throw new InvalidOperationException("There are no bots to prepare and run.");

			CreateMissingDirectories();
			LoadConfig();
			SetupSentry();
			LoadDatabase();
			InitializeLocalizationEngine();
			LoadStatistics();

			foreach (var bot in _bots)
				await bot.InitializeAsync();

			Update += (s, e) => Helpers.SaveJson(Path.Combine("data", "statistics"), Statistics);
			Update += (s, e) =>
			{
				Database.Checkpoint();
				Logger.Verbose("Created database checkpoint", "CraftBot");
			};
			StartTimer();

			foreach (var bot in _bots)
				await bot.ConnectAsync();

			await KeepAliveAsync();

			_sentry?.Dispose();
		}

		public async Task StopAsync()
		{
			foreach (var bot in _bots)
				await bot.StopAsync();

			Logger.Info("Closing database...");
			Database.Dispose();

			Logger.Info("Invoking update callback...");
			Update?.Invoke(null, EventArgs.Empty);

			_keepAlive.Cancel();
		}

		public event EventHandler Update;

		private async Task KeepAliveAsync()
		{
			try
			{
				await Task.Delay(-1, _keepAlive.Token);
			}
			catch (TaskCanceledException)
			{
			}
		}

		private void LoadStatistics()
		{
			var statisticsPath = Path.Combine("data", "statistics");
			Statistics = Helpers.GetJson(statisticsPath, new Statistics());
		}

		private void LoadDatabase()
		{
			Logger.Info("Loading database...", "Database");

			_bsonMapper = new BsonMapper
			{
				EmptyStringToNull = true
			};

			Database = new LiteDatabase("data.db", _bsonMapper);
		}

		private void LoadConfig()
		{
			var path = Path.Combine("config", "config");
			Config = Helpers.GetJson(path, new Config());
		}

		private void SetupSentry()
		{
			var sentryDsn = Config.Tokens[TokenType.Sentry];

			if (string.IsNullOrWhiteSpace(sentryDsn))
				return;

			_sentry = SentrySdk.Init(sentryDsn);
		}

		private static void CreateMissingDirectories()
		{
			string[] directories =
			{
				"assets",
				"cache",
				Path.Combine("cache", "user"),
				Path.Combine("cache", "channel"),
				Path.Combine("cache", "guild"),
				Path.Combine("cache", "attachments"),
				"lang"
			};

			foreach (var directory in directories)
			{
				var path = Path.GetFullPath(directory);

				if (Directory.Exists(path))
					continue;

				Directory.CreateDirectory(path);
			}
		}

		private void InitializeLocalizationEngine()
		{
			const string fallback = "en";

			Localization = new LocalizationEngine();
			var languages = Localization.Languages;

			Logger.Info("Loading available languages...", "Localization Engine");

			foreach (var path in Directory.GetFiles("lang", "*.json"))
			{
				try
				{
					var json = File.ReadAllText(path);
					var language = Language.FromJson(Localization, json);
					languages.Add(language);
				}
				catch (Exception e)
				{
					Helpers.ReportException(e, $"Failed to load language at {path}", "Localization Engine");
				}
			}

			Localization.FallbackLanguage = Localization.GetLanguage(fallback);

			Logger.Info($"Loaded {languages.Count} languages", "Localization Engine");
		}

		private void StartTimer()
		{
			_updateTimer = new Timer(5 * 60 * 1000)
			{
				AutoReset = true,
				Enabled = true
			};
			_updateTimer.Elapsed += (s, e) => Update?.Invoke(null, EventArgs.Empty);
		}
	}
}