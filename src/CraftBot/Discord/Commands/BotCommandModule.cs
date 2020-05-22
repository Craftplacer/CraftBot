using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CraftBot.Extensions;
using CraftBot.Localization;
using CraftBot.Repositories;
using Disqord.Bot;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace CraftBot.Discord.Commands
{
	[Group("bot")]
	public class BotCommandModule : ModuleBase<DiscordCommandContext>
	{
        public LocalizationEngine Localization { get; set; }
        public UserRepository UserRepository { get; set; }
        
		[Command("", "info", "about")]
        public async Task Info()
        {
            var language = UserRepository.Get(Context.User).GetLanguage(Localization);
            var statistics = Context.ServiceProvider.GetService<Statistics>();
            
            string getMemoryUsage() => $"{Math.Round(GC.GetTotalMemory(false) / 1000000f, 2)} MB";
            async Task<string> getChangelogsLastUpdateAsync()
            {
                using var webClient = new WebClient();

                var file = await webClient.DownloadStringTaskAsync("https://gist.githubusercontent.com/Craftplacer/2e27cd05d485acf38aee1cedca5fabb9/raw/CraftBot%20Changelogs.md");
                var lines = file.Split('\n');
                var firstLine = lines.First(l => l.StartsWith("# "));
                return firstLine.Substring(2);
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = Colors.Indigo500,
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = Context.Client.CurrentUser.AvatarUrl,
                    Name = Context.Client.CurrentUser.Username,
                    Url = "https://github.com/Craftplacer/CraftBot"
                },
                Description =
                    "Multi-purpose bot\n:warning: **Expect inconsistent uptime because of real-time debugging.**"
            };
                
            // {Emoji.IconCommand} {language.GetCounter(Context.Bot.RegisteredCommands.Count, "commands")}
            
            embed.AddField(
                language["bot.info.botinfo"],
                @$"{Emoji.IconAccountGroup} {language.GetCounter(Context.Client.Guilds.Count, "server")}
                {Emoji.IconCircleDouble} {Context.Client.Ping}ms
                {Emoji.IconTextureBox} {language.GetCounter(Context.Client.ShardCount, "shards")}",
                true
            );
            
            embed.AddField(
                language["bot.info.appinfo"],
                @$"{Emoji.IconClockOutline} {(DateTime.Now - statistics.CurrentStartTime).GetString(language)}
                {Emoji.IconMonitor} {Helpers.GetOperatingSystem()}
                {Emoji.IconDsharpplus} v{Context.Client.VersionString}
                {Emoji.IconMemory} {getMemoryUsage()}",
                true
            );
            embed.AddField(
            language["bot.info.links.title"],
            "[GitHub](https://github.com/Craftplacer/CraftBot)"
            );

            var message = await Context.RespondAsync(embed);
            var lastUpdate = await getChangelogsLastUpdateAsync();

            const string changelogsGist = "https://gist.github.com/Craftplacer/2e27cd05d485acf38aee1cedca5fabb9";
            embed.Fields[2].Value += $"\n[{language["bot.info.links.changelogs"]}]({changelogsGist}) (last updated {lastUpdate})";

            await message.ModifyAsync(embed: embed.Build());
        }
        
        [Command("contributors","translators", "credits")]
        public async Task ShowContributors()
        {
            var language = UserRepository.Get(Context.User).GetLanguage(Localization);

            var translators = GetTranslators();
            var embed = new DiscordEmbedBuilder().WithTitle(language["bot.info.contributors"])
                .AddField("CraftBot", "Craftplacer#4006")
                .AddField("Libraries", @"[DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/)
                                                    [LiteDB](https://github.com/mbdavid/litedb)
                                                    [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)", true)
                .AddField("Translation & Localization", translators, true);

            await Context.RespondAsync(embed);
        }

        private string GetTranslators()
        {
            var builder = new StringBuilder();

            foreach (var language in Localization.Languages)
            {
                var users = language.Authors.Select(async id => await Context.Client.GetUserAsync(id)).Select(task => task.Result);
                var names = users.Select(user => $"{user.Username}#{user.Discriminator}");
                builder.AppendLine($"**{language.Name}**\n{string.Join(", ", names)}");
            }

            return builder.ToString();
        }

        [Command("stop", "shutdown")]
        [Description("Shuts down CraftBot")]
        [BotOwnerOnly]
        public async Task Stop()
        {
            var bot = Context.ServiceProvider.GetService<DiscordBot>();
            
            await Context.Client.UpdateStatusAsync(new DiscordActivity("Shutting down..."), UserStatus.DoNotDisturb);
            await Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("👋"));

            await bot.StopAsync();
        }

        [Command("hopoff","detach")]
        [Description("Switches to a new non-debug instance.")]
        [BotOwnerOnly]
        public async Task HopOff()
        {
            var bot = Context.ServiceProvider.GetService<DiscordBot>();
            
            var filePath = typeof(Program).Assembly.Location;
            var arguments = string.Join(' ', Environment.GetCommandLineArgs());

            // .NET Core support
            if (Path.GetExtension(filePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                arguments = $"{filePath} {arguments}";
                filePath = "dotnet";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments,
                CreateNoWindow = false,
                ErrorDialog = true
            };

            await Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏫"));

            Process.Start(startInfo);

            await bot.StopAsync();
        }

        [Command("attach")]
        [Description("Attaches a debugger (e.g. Visual Studio)")]
        [BotOwnerOnly]
        public async Task Attach()
        {
            if (Debugger.IsAttached)
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = "You already have a debugger attached.",
                    Description = "CraftBot can't launch a debugger if one is already attached."
                });

                return;
            }

            var message = await Context.RespondAsync(new DiscordEmbedBuilder
            {
                Color = Colors.LightBlue500,
                Title = "Launching debugger...",
                Description = "CraftBot may hang up."
            });

            Debugger.Launch();

            await message.ModifyAsync(embed: new DiscordEmbedBuilder
            {
                Color = Colors.LightGreen500,
                Title = "Debugger attached"
            }.Build());
        }

        [Command("break", "pause")]
        [Description("Debugger.Break();")]
        [BotOwnerOnly]
        public async Task Break()
        {
            if (!Debugger.IsAttached)
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = "No debugger attached!",
                    Description = "CraftBot cannot be 'broken' unless a debugger is attached."
                });

                return;
            }

            await Context.Client.UpdateStatusAsync(new DiscordActivity("Breaking"), UserStatus.Idle);
            await Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏸️"));

            Debugger.Break();

            await Context.Client.UpdateStatusAsync(new DiscordActivity("Awaking from debugger"), UserStatus.Online);
            await Context.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("⏸️"));
        }

        [Command("statistics","stat", "stats")]
        public async Task ViewStatistics()
        {
            var language = UserRepository.Get(Context.User).GetLanguage(Localization);
            var statistics = Context.ServiceProvider.GetService<Statistics>();
            
            var embed = new DiscordEmbedBuilder
            {
                Color = Colors.Indigo500,
                Title = language["bot.stats.title"]
            }
                .AddField(
                    language["bot.stats.commandsrecognized"],
                    $"{statistics.CommandsTotal}"
                )
                .AddField(
                    language["bot.stats.commandsexecuted"],
                    $"{statistics.CommandsExecuted}", true
                )
                .AddField(
                    language["bot.stats.errorrate"],
                    $"{statistics.CommandsErrored} ({Math.Round(statistics.ErrorRate * 100, 2)}%)"
                )
                .AddField(
                    language["bot.stats.mistyperate"],
                    $"{statistics.CommandsMistyped} ({Math.Round(statistics.MistypeRate * 100, 2)}%)",
                    true
                )
                .AddField(
                    language["bot.stats.messagesquoted"],
                    statistics.MessagesQuoted.ToString()
                )
                .WithFooter(
                    language["bot.stats.since",
                        (DateTime.Now - statistics.StartTime).GetString(language)]
                );

            await Context.RespondAsync(embed);
        }
    }
}