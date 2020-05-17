using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CraftBot.Commands;
using CraftBot.Common;
using CraftBot.Features;
using CraftBot.Model;
using CraftBot.Repositories;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Sentry;
using Sentry.Protocol;
using CommandBuilder = DSharpPlus.CommandsNext.Builders.CommandBuilder;
using CommandContext = DSharpPlus.CommandsNext.CommandContext;

namespace CraftBot.Discord
{
	public class DiscordBot : SubBot
	{
		private Quoting _quoting;
		private DiscordClient _client;
		private CommandsNextExtension _commandsNext;
		private GuildRepository _guildRepository;
		private UserRepository _userRepository;
		private DiscourseFeed _discourseFeed;
		private CommandService _commandService;

		public DiscordBot([NotNull] BotManager manager, CommandService service = null) : base(manager)
		{
			_commandService = service;
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

			_client = new DiscordClient(discordConfig);
			BindEvents();

			_userRepository = new UserRepository(Manager.Database);
			_guildRepository = new GuildRepository(Manager.Database);

			SetupExtensions();
		}
		
		public override async Task ConnectAsync()
		{
			Logger.Info("Connecting to Discord...", "CraftBot");
			await _client.ConnectAsync();
		}

		public override async Task StopAsync()
		{
			Logger.Info("Disconnecting from Discord...");
			await _client.DisconnectAsync();
		}
		
		private void SetupExtensions()
		{
			// Add extensions
			var serviceCollection = new ServiceCollection();
			serviceCollection.AddSingleton(this);
			serviceCollection.AddSingleton(Manager.Localization);
			serviceCollection.AddSingleton(Manager.Statistics);
			serviceCollection.AddSingleton(_userRepository);
			serviceCollection.AddSingleton(_guildRepository);

			// Setup Interactivity
			var interactivityConfig = new InteractivityConfiguration();
			_client.UseInteractivity(interactivityConfig);

			SetupCommandsNext(serviceCollection);

			// Add other events and classes
			_quoting = new Quoting();
			_client.AddExtension(_quoting);

			// _ircIntegration = new IrcIntegration();
			// _client.AddExtension(_ircIntegration);

			_discourseFeed = new DiscourseFeed();
			_client.AddExtension(_discourseFeed);
		}

		private void SetupCommandsNext(ServiceCollection serviceCollection)
		{
			// Setup CommandsNext
			var commandNextConfig = new CommandsNextConfiguration
			{
				CaseSensitive = false,
				StringPrefixes = new[] { Manager.Config.Prefix },
				Services = serviceCollection.BuildServiceProvider(),
				UseDefaultCommandHandler = false
			};
			
			_commandsNext = _client.UseCommandsNext(commandNextConfig);
			_commandsNext.CommandErrored += CommandsNext_CommandErrored;
			_commandsNext.CommandExecuted += CommandsNext_CommandExecuted;

			// Register Commands
			// _commandsNext.SetHelpFormatter<HelpFormatter>();
			_commandsNext.RegisterCommands<MainCommands>();
			_commandsNext.RegisterCommands<BotCommands>();
			_commandsNext.RegisterCommands<GuildCommands>();
			_commandsNext.RegisterCommands<UserCommands>();
			_commandsNext.RegisterCommands<ModerationCommands>();
			_commandsNext.RegisterCommands<BooruCommands>();
			_commandsNext.RegisterCommands<ShitpostingCommands>();
			// _commandsNext.RegisterCommands<IrcCommands>();

			IntegrateCommonCommands();
		}

		private void IntegrateCommonCommands()
		{
			if (_commandService == null)
				return;

			var convertedCommands = new List<CommandBuilder>();
			
			foreach (var commonCommand in _commandService.GetAllCommands())
			{
				var builder = new CommandBuilder();

				builder.WithName(commonCommand.Name);
				builder.WithAliases(commonCommand.Aliases.Skip(1).ToArray());
				builder.WithDescription(commonCommand.Description);
				builder.WithHiddenStatus(!commonCommand.IsEnabled);
				
				convertedCommands.Add(builder);
			}
			
			_commandsNext.RegisterCommands(convertedCommands.ToArray());
		}

		private void BindEvents()
		{
			_client.ClientErrored += ClientOnClientErrored;
			_client.MessageCreated += Client_MessageCreated;
			_client.MessageCreated += ClientOnMessageCreated_HandleCommand;
			_client.Ready += Client_Ready;
			_client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;
		}

		private async Task ClientOnMessageCreated_HandleCommand(MessageCreateEventArgs e)
		{
			if (!CommandUtilities.HasPrefix(e.Message.Content, Manager.Config.Prefix, out var output))
				return;

			var discordCommand = _commandsNext.FindCommand(output, out var rawArguments);

			if (discordCommand != null && discordCommand.Overloads.Any())
			{
				var discordContext = _commandsNext.CreateContext(e.Message, Manager.Config.Prefix, discordCommand, rawArguments);
				await _commandsNext.ExecuteCommandAsync(discordContext);
				return;
			}
			
			if (_commandService != null)
			{
				var commandMatches = _commandService.FindCommands(output);

				if (commandMatches.Any())
				{
					var commonMessage = new CommonDiscordMessage(e.Message);
					var commonContext = new CommonCommandContext(commonMessage);
					var result = await _commandService.ExecuteAsync(output, commonContext);
					
					return;
				}
			}
			
			// TODO: Command not found
			//e.Message.RespondAsync("")
		}

		private static async Task Client_Ready(ReadyEventArgs e)
		{
			Logger.Info("Ready!");

			DiscordActivity activity;

			if (Debugger.IsAttached)
			{
				activity = new DiscordActivity
				{
					ActivityType = ActivityType.Streaming,
					Name = "Debugging",
					StreamUrl = "https://twitch.tv/settings"
				};
			}
			else
			{
				activity = new DiscordActivity("Rewritten v3");
			}

			await e.Client.UpdateStatusAsync(activity, UserStatus.Online);
		}

		private async Task CommandsNext_CommandErrored(CommandErrorEventArgs e)
		{
			var exception = e.Exception;

			// User entered an unknown (sub-)command.
			var notFound         = exception is CommandNotFoundException;
			var wrongSyntax      = exception is ArgumentException         && exception.Source?.Equals("DSharpPlus.CommandsNext") == true;
			var groupCommandUsed = exception is InvalidOperationException && e.Command                                           != null;

			//_commandService.
			
			if (notFound || wrongSyntax || groupCommandUsed)
			{
				Manager.Statistics.CommandsMistyped++;

				if (!wrongSyntax && !groupCommandUsed)
					return;

				//TODO: Find another way to invoke command help
				var helpCommand = e.Context.CommandsNext.FindCommand("help", out _);
				var args = e.Command.QualifiedName;

				var context = e.Context.CommandsNext.CreateFakeContext(
					e.Context.User,
					e.Context.Channel,
					$"{e.Context.Prefix}help {args}",
					e.Context.Prefix,
					helpCommand,
					args
				);

				await helpCommand.ExecuteAsync(context);
			}
			else if (exception is ChecksFailedException checksFailedException) // User ran a command that didn't pass its checks.
			{
				await HandleFailedChecks(e.Context, checksFailedException);
			}
			else // Command failed out because of another reason.
			{
				await HandleFailedCommand(e, exception);
			}
		}

		private async Task HandleFailedChecks(CommandContext context, ChecksFailedException checksFailedException)
		{
			var stringBuilder = new StringBuilder();

			foreach (var failedCheck in checksFailedException.FailedChecks)
			{
				var description = GetDescription(context.Client, failedCheck);
				stringBuilder.AppendLine(description);
			}

			var embed = new DiscordEmbedBuilder
			{
				Title = "Checks failed",
				Description = stringBuilder.ToString(),
				Color = Colors.Red500
			};

			await context.RespondAsync(embed: embed);
		}

		private async Task HandleFailedCommand(CommandErrorEventArgs e, Exception exception)
		{
			Manager.Statistics.CommandsErrored++;

			var sentryId = Guid.Empty;

			SentrySdk.ConfigureScope(scope =>
			{
				scope.User = new User
				{
					Id = e.Context.User.Id.ToString(),
					Username = $"{e.Context.User.Username}#{e.Context.User.Discriminator}"
				};
				scope.Level = SentryLevel.Error;
				scope.SetExtra("command", e.Command.ToString());
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

			await e.Context.RespondAsync(embed: embed);
		}
		
		private static string GetDescription(DiscordClient client, CheckBaseAttribute attribute)
		{
			var stringBuilder = new StringBuilder();

			switch (attribute)
			{
				case RequirePermissionsAttribute requirePermissionsAttribute:
				{
					stringBuilder.AppendLine($"**{client.CurrentUser.Username} and/or you are missing following permissions:**");
					stringBuilder.AppendLine(requirePermissionsAttribute.Permissions.ToPermissionString());
					break;
				}
				case RequireUserPermissionsAttribute requireUserPermissionsAttribute:
				{
					stringBuilder.AppendLine("**You are missing following permissions:**");
					stringBuilder.AppendLine(requireUserPermissionsAttribute.Permissions.ToPermissionString());
					break;
				}
				case RequireBotPermissionsAttribute requireBotPermissionsAttribute:
				{
					stringBuilder.AppendLine($"**{client.CurrentUser.Username} is missing following permissions:**");
					stringBuilder.AppendLine(requireBotPermissionsAttribute.Permissions.ToPermissionString());
					break;
				}
				case RequireOwnerAttribute _:
				{
					var mentions = string.Join(", ",
					                           client.CurrentApplication.Team.Members.Select(m => m.User.Mention));
					stringBuilder.AppendLine($"Can only be run by the bot owner(s) ({mentions})");
					break;
				}
				case RequireRolesAttribute requireRolesAttribute:
				{
					var header = requireRolesAttribute.CheckMode switch
					{
						RoleCheckMode.Any           => "You need any of these roles",
						RoleCheckMode.All           => "You need these roles",
						RoleCheckMode.SpecifiedOnly => "You need exactly these roles",
						RoleCheckMode.None          => "You need to have none of these roles",
						_                           => null
					};

					stringBuilder.AppendLine($"**{header}:**");

					foreach (var item in requireRolesAttribute.RoleNames)
					{
						stringBuilder.AppendLine(item);
					}

					break;
				}
				case RequireGuildAttribute _:
					stringBuilder.AppendLine("Can only be run inside a server");
					break;
				case RequireDirectMessageAttribute _:
					stringBuilder.AppendLine("Can only be run inside direct messages");
					break;
				case RequireNsfwAttribute _:
					stringBuilder.AppendLine("Can only be run inside NSFW channels");
					break;
				default:
					stringBuilder.AppendLine(attribute.GetType().Name);
					break;
			}

			return stringBuilder.ToString();
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

		private async Task CommandsNext_CommandExecuted(CommandExecutionEventArgs e)
		{
			Manager.Statistics.CommandsExecuted++;

			var data = _userRepository.Get(e.Context.User);

			if (data.OneTimeNotices.HasFlag(OneTimeNotices.UserSurvey))
				return;

			data.OneTimeNotices |= OneTimeNotices.UserSurvey;
			_userRepository.Save(data);

			try
			{
				await e.Context.Member.SendMessageAsync(embed: new DiscordEmbedBuilder
				{
					Title = "User Survey",
					Description = "We're currently conducting a survey, asking our users to tell us what they think of CraftBot at the moment.\nIf you have time we'd like you to fill out this survey.\n\n[**CraftBot Feedback Survey**](https://forms.gle/9KGMRpgBWQCJwkKQ8)",
					Footer = new DiscordEmbedBuilder.EmbedFooter
					{
						Text = data.GetLanguage(Manager.Localization)["otn.footer"]
					}
				});
				Logger.Info($"Successfully sent a survey invitation to {e.Context.User.Username}", "Survey");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Survey");
			}
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