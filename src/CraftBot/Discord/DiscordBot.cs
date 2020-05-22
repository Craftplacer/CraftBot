using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CraftBot.Discord.Commands;
using CraftBot.Features;
using CraftBot.Model;
using CraftBot.Repositories;
using Disqord.Bot;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Sentry;
using Sentry.Protocol;

namespace CraftBot.Discord
{
	public class DiscordBot : SubBot
	{
		public DiscordClient Client { get; private set; }
		private GuildRepository _guildRepository;
		private UserRepository _userRepository;
		private DiscourseFeed _discourseFeed;
		private Quoting _quoting;
		private CommandService _commandService;
		private IServiceProvider _serviceProvider;
		
		public DiscordBot([NotNull] BotManager manager) : base(manager)
		{
			
		}
		
		public override async Task InitializeAsync()
		{
			var token = Manager.Config.Tokens[TokenType.Discord];
			
			if (string.IsNullOrWhiteSpace(token))
			{
				Logger.Error("Discord token is not set!", "CraftBot");
				return;
			}

			var discordConfig = new DiscordConfiguration
			{
				Token = token,
				TokenType = DSharpPlus.TokenType.Bot,
				AutoReconnect = true,
				ReconnectIndefinitely = true,
				UseInternalLogHandler = false
			};

			Client = new DiscordClient(discordConfig);
			BindEvents();

			_userRepository = new UserRepository(Manager.Database);
			_guildRepository = new GuildRepository(Manager.Database);

			_serviceProvider = GetServiceProvider();

			SetupExtensions();
		}
		
		public override async Task ConnectAsync()
		{
			Logger.Info("Connecting to Discord...", "CraftBot");
			await Client.ConnectAsync();
		}

		public override async Task StopAsync()
		{
			Logger.Info("Disconnecting from Discord...");
			await Client.DisconnectAsync();
		}

		private IServiceProvider GetServiceProvider()
		{
			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton(this);
			serviceCollection.AddSingleton(_userRepository);
			serviceCollection.AddSingleton(_guildRepository);
			Manager.AddGlobalServices(serviceCollection);

			return serviceCollection.BuildServiceProvider();
		} 
		
		private void SetupExtensions()
		{
			// Add extensions
			
			
			// Setup Interactivity
			var interactivityConfig = new InteractivityConfiguration();
			Client.UseInteractivity(interactivityConfig);

			_commandService = new CommandService();
			_commandService.AddModule<BotCommandModule>();
			_commandService.AddModule<GuildCommandModule>();
			_commandService.AddModule<UserCommandModule>();
			_commandService.AddModule<ModerationCommandModule>();
			Manager.PatchGenericCommands(_commandService);

			// Add other events and classes
			_quoting = new Quoting(_userRepository, Manager.Statistics, Manager.Localization);
			Client.AddExtension(_quoting);

			// _ircIntegration = new IrcIntegration();
			// Client.AddExtension(_ircIntegration);

			_discourseFeed = new DiscourseFeed();
			Client.AddExtension(_discourseFeed);
		}

		private void SetupCommandsNext(ServiceCollection serviceCollection)
		{
			
			// _commandsNext = Client.UseCommandsNext(commandNextConfig);
			// _commandsNext.CommandErrored += CommandsNext_CommandErrored;
			// _commandsNext.CommandExecuted += CommandsNext_CommandExecuted;

			// Register Commands
			// _commandsNext.SetHelpFormatter<HelpFormatter>();
			// _commandsNext.RegisterCommands<MainCommands>();
			// _commandsNext.RegisterCommands<BotCommands>();
			// _commandsNext.RegisterCommands<GuildCommands>();
			// _commandsNext.RegisterCommands<UserCommands>();
			// _commandsNext.RegisterCommands<ModerationCommands>();
			// _commandsNext.RegisterCommands<BooruCommands>();
			// _commandsNext.RegisterCommands<ShitpostingCommands>();
			// _commandsNext.RegisterCommands<IrcCommands>();
		}

		private void BindEvents()
		{
			Client.ClientErrored += ClientOnClientErrored;
			Client.MessageCreated += Client_MessageCreated;
			Client.MessageCreated += ClientOnMessageCreated_HandleCommand;
			Client.Ready += Client_Ready;
			Client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;
		}

		private async Task ClientOnMessageCreated_HandleCommand(MessageCreateEventArgs e)
		{
			if (!CommandUtilities.HasPrefix(e.Message.Content, Manager.Config.Prefix, out var output))
				return;

			var commonContext = new DiscordCommandContext(this, e.Message, _serviceProvider);
			var result = await _commandService.ExecuteAsync(output, commonContext);

			if (result.IsSuccessful)
			{
				Manager.Statistics.CommandsExecuted++;
				return;
			}

			switch (result)
			{
				case ExecutionFailedResult executionFailedResult:
					var exception = executionFailedResult.Exception;
					var sentryId = Guid.Empty;

					SentrySdk.ConfigureScope(scope =>
					{
						scope.User = new User
						{
							Id = e.Author.Id.ToString(),
							Username = $"{e.Author.Username}#{e.Author.Discriminator}"
						};
						scope.Level = SentryLevel.Error;
						scope.SetExtra("command", executionFailedResult.Command.ToString());
						scope.SetTag("type", "failed-command");

						sentryId = SentrySdk.CaptureException(exception);
					});

					var embed = new DiscordEmbedBuilder
					{
						Title = "Command failed",
						Description = exception.Message,
						Color = Colors.Red500
					};
					embed.WithFooter($"{exception.GetType()} • {sentryId}");

					await e.Message.RespondAsync(embed: embed);
					
					break;
				
				default:
					Logger.Warning($"Unhandled command result: {result} ({result.GetType()})");
					break;
			}
			
			Manager.Statistics.CommandsErrored++;
		}

		private static async Task Client_Ready(ReadyEventArgs e)
		{
			Logger.Info("Ready!");

			DiscordActivity activity;

			if (Debugger.IsAttached)
			{
				activity = new DiscordActivity("Under Construction");
			}
			else
			{
				activity = new DiscordActivity("Rewritten v3");
			}

			await e.Client.UpdateStatusAsync(activity, UserStatus.Online);
		}
		
		private async Task Client_MessageCreated(MessageCreateEventArgs e)
		{
			const ulong user = 194891941509332992;

			// check if CraftBot is mentioned
			if (!e.Message.MentionedUsers.Contains(e.Client.CurrentUser))
				return;

			// ignore non-server messages
			if (e.Guild == null)
				return;

			// check if i'm (Craftplacer) is in the server
			if (e.Guild.Members.Values.All(m => m.Id != user))
				return;

			if (e.Message.Content.StartsWith(Manager.Config.Prefix, StringComparison.OrdinalIgnoreCase))
				return;

			var language = _userRepository.Get(e.Author).GetLanguage(Manager.Localization);

			await e.Message.RespondAsync(language["bot.autocompleteoops", $"<@{user}>"]);
		}

		private async Task ClientOnClientErrored(ClientErrorEventArgs e)
		{
			Helpers.ReportException(e.Exception, "CraftBot", "Client error");
		}

		private void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
		{
			switch (e.Level)
			{
				case LogLevel.Warning:
					Logger.Warning(e.Message, e.Application);
					break;

				case LogLevel.Error:
				case LogLevel.Critical:
					Logger.Error(e.Message, e.Application);
					if (e.Exception != null)
					{
						Logger.Error(e.Exception, e.Application);
					}

					break;

				default:
					Logger.Info(e.Message, e.Application);
					break;
			}
		}
	}
}