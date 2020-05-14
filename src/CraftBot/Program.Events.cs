using CraftBot.Model;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Sentry;
using Sentry.Protocol;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftBot
{
    public partial class Program
    {
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
                    StreamUrl = "https://twitch.tv/settings",
                };
            }
            else
            {
                activity = new DiscordActivity("Rewritten v3");
            }

            await e.Client.UpdateStatusAsync(activity, UserStatus.Online);
        }

        private static async Task CommandsNext_CommandErrored(CommandErrorEventArgs e)
        {
            var exception = e.Exception;

            // User entered an unknown (sub-)command.
            var notFound = exception is CommandNotFoundException;
            var wrongSyntax = exception is ArgumentException && exception.Source.Equals("DSharpPlus.CommandsNext");
            var groupCommandUsed = exception is InvalidOperationException && e.Command != null;

            if (notFound || wrongSyntax || groupCommandUsed)
            {
                Statistics.CommandsMistyped++;
                Statistics.Graph.Add(Tuple.Create(DateTime.Now, "mistype"));

                if (!wrongSyntax && !groupCommandUsed)
                    return;

                //TODO: Find another way to invoke command help
                var helpCommand = e.Context.CommandsNext.FindCommand("help", out _);
                var args = e.Command.QualifiedName;
                var context = e.Context.CommandsNext.CreateFakeContext(e.Context.User,
                    e.Context.Channel,
                    $"{e.Context.Prefix}help {args}",
                    e.Context.Prefix,
                    helpCommand,
                    args);

                await helpCommand.ExecuteAsync(context);

                return;
            }

            // User ran a command that didn't pass its checks.
            if (exception is ChecksFailedException checksFailedException)
            {
                var description = new StringBuilder();

                foreach (var failedCheck in checksFailedException.FailedChecks)
                {
                    switch (failedCheck)
                    {
                        case RequirePermissionsAttribute requirePermissionsAttribute:
                        {
                            description.AppendLine(
                                $"**{_client.CurrentUser.Username} and/or you are missing following permissions:**");
                            description.AppendLine(requirePermissionsAttribute.Permissions.ToPermissionString());
                            description.AppendLine();
                            break;
                        }
                        case RequireUserPermissionsAttribute requireUserPermissionsAttribute:
                        {
                            description.AppendLine("**You are missing following permissions:**");
                            description.AppendLine(requireUserPermissionsAttribute.Permissions.ToPermissionString());
                            description.AppendLine();
                            break;
                        }
                        case RequireBotPermissionsAttribute requireBotPermissionsAttribute:
                        {
                            description.AppendLine(
                                $"**{_client.CurrentUser.Username} is missing following permissions:**");
                            description.AppendLine(requireBotPermissionsAttribute.Permissions.ToPermissionString());
                            description.AppendLine();
                            break;
                        }
                        case RequireOwnerAttribute _:
                        {
                            var mentions = string.Join(", ",
                                _client.CurrentApplication.Team.Members.Select(m => m.User.Mention));
                            description.AppendLine($"Can only be run by the bot owner(s) ({mentions})");
                            break;
                        }
                        case RequireRolesAttribute requireRolesAttribute:
                        {
                            var header = requireRolesAttribute.CheckMode switch
                            {
                                RoleCheckMode.Any => "You need any of these roles",
                                RoleCheckMode.All => "You need these roles",
                                RoleCheckMode.SpecifiedOnly => "You need exactly these roles",
                                RoleCheckMode.None => "You need to have none of these roles",
                                _ => null
                            };

                            description.AppendLine($"**{header}:**");

                            foreach (var item in requireRolesAttribute.RoleNames)
                            {
                                description.AppendLine(item);
                            }

                            description.AppendLine();
                            break;
                        }
                        case RequireGuildAttribute _:
                            description.AppendLine("Can only be run inside a server");
                            break;
                        case RequireDirectMessageAttribute _:
                            description.AppendLine("Can only be run inside direct messages");
                            break;
                        case RequireNsfwAttribute _:
                            description.AppendLine("Can only be run inside NSFW channels");
                            break;
                        default:
                            description.AppendLine(failedCheck.GetType().Name);
                            break;
                    }
                }

                await e.Context.RespondAsync(embed:
                    new DiscordEmbedBuilder
                    {
                        Title = "Checks failed",
                        Description = description.ToString(),
                        Color = Colors.Red500
                    }
                );

                return;
            }

            // Command errored out because of another reason.
            Statistics.CommandsErrored++;
            Statistics.Graph.Add(Tuple.Create(DateTime.Now, "error"));

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

            await e.Context.RespondAsync(embed:
                new DiscordEmbedBuilder
                    {
                        Title = "Command errored",
                        Description = exception.Message,
                        Color = Colors.Red500
                    }
                    .WithFooter($"{exception.GetType()} • {sentryId}")
            );
        }

        private static async Task Client_MessageCreated(MessageCreateEventArgs e)
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

            if (e.Message.Content.StartsWith("cb!", StringComparison.OrdinalIgnoreCase))
                return;

            var language = _userRepository.Get(e.Author).GetLanguage(_localization);

            await e.Message.RespondAsync(language["bot.autocompleteoops", $"<@{user}>"]);
        }

        private static async Task CommandsNext_CommandExecuted(CommandExecutionEventArgs e)
        {
            Statistics.CommandsExecuted++;

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
                    Description =
                        "We're currently conducting a survey, asking our users to tell us what they think of CraftBot at the moment.\nIf you have time we'd like you to fill out this survey.\n\n[**CraftBot Feedback Survey**](https://forms.gle/9KGMRpgBWQCJwkKQ8)",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = data.GetLanguage(_localization)["otn.footer"]
                    }
                });
                Logger.Info($"Successfully sent a survey invitation to {e.Context.User.Username}", "Survey");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Survey");
            }
        }

        private static async Task ClientOnClientErrored(ClientErrorEventArgs e)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.Level = SentryLevel.Error;
                scope.SetExtra("event-name", e.EventName);
                scope.SetTag("type", "client-error");

                var sentryId = SentrySdk.CaptureException(e.Exception);
                Logger.Warning("Captured client exception and submitted to Sentry: " + sentryId);
            });
        }

        private static void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
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

        private static Task Client_SocketClosed(SocketCloseEventArgs e)
        {
            Statistics.Graph.Add(Tuple.Create(DateTime.Now, "disconnect"));
            return Task.CompletedTask;
        }

        private static Task Client_SocketOpened()
        {
            Statistics.Graph.Add(Tuple.Create(DateTime.Now, "connect"));
            return Task.CompletedTask;
        }
    }
}