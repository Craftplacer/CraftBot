using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CraftBot.Discord;
using CraftBot.Extensions;
using CraftBot.Localization;
using CraftBot.Repositories;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace CraftBot.Commands
{
    [Group("bot")]
    public partial class BotCommands : BaseCommandModule
    {
        public LocalizationEngine Localization { get; set; }
        public UserRepository UserRepository { get; set; }

        [Command("info")]
        [Aliases("about")]
        [GroupCommand]
        public async Task Info(CommandContext context)
        {
            var language = UserRepository.Get(context.User).GetLanguage(Localization);
            var statistics = context.Services.GetService<Statistics>();
            
            string getMemoryUsage() => $"{Math.Round(GC.GetTotalMemory(false) / 1000000f, 2)} MB";
            static string getOS()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                    using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                    if (key != null)
                        return (string)key.GetValue("ProductName");
                }

                return RuntimeInformation.OSDescription;
            }
            async Task<string> getChangelogsLastUpdateAsync()
            {
                using var webClient = new System.Net.WebClient();

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
                    IconUrl = context.Client.CurrentUser.AvatarUrl,
                    Name = context.Client.CurrentUser.Username,
                    Url = "https://github.com/Craftplacer/CraftBot"
                },
                Description =
                    "Multi-purpose bot\n:warning: **Expect inconsistent uptime because of real-time debugging.**"
            };
                
            embed.AddField(
                language["bot.info.botinfo"],
                @$"{Emoji.IconAccountGroup} {language.GetCounter(context.Client.Guilds.Count, "server")}
                {Emoji.IconCommand} {language.GetCounter(context.CommandsNext.RegisteredCommands.Count, "commands")}
                {Emoji.IconCircleDouble} {context.Client.Ping}ms
                {Emoji.IconTextureBox} {language.GetCounter(context.Client.ShardCount, "shards")}",
                true
            );
            
            embed.AddField(
                language["bot.info.appinfo"],
                @$"{Emoji.IconClockOutline} {(DateTime.Now - statistics.CurrentStartTime).GetString(language)}
                {Emoji.IconMonitor} {getOS()}
                {Emoji.IconDsharpplus} v{context.Client.VersionString}
                {Emoji.IconMemory} {getMemoryUsage()}",
                true
            );
            embed.AddField(
            language["bot.info.links.title"],
            "[GitHub](https://github.com/Craftplacer/CraftBot)"
            );

            var message = await context.RespondAsync(embed: embed);
            var lastUpdate = await getChangelogsLastUpdateAsync();

            const string changelogsGist = "https://gist.github.com/Craftplacer/2e27cd05d485acf38aee1cedca5fabb9";
            embed.Fields[2].Value += $"\n[{language["bot.info.links.changelogs"]}]({changelogsGist}) (last updated {lastUpdate})";

            await message.ModifyAsync(embed: embed.Build());
        }

        [Command("contributors")]
        [Aliases("translators", "credits")]
        public async Task ShowContributors(CommandContext context)
        {
            var language = UserRepository.Get(context.User).GetLanguage(Localization);

            var translators = GetTranslators(context);
            var embed = new DiscordEmbedBuilder().WithTitle(language["bot.info.contributors"])
                .AddField("CraftBot", "Craftplacer#4006")
                .AddField("Libraries", @"[DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/)
                                                    [LiteDB](https://github.com/mbdavid/litedb)
                                                    [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)", true)
                .AddField("Translation & Localization", translators, true);

            await context.RespondAsync(embed: embed);
        }

        private string GetTranslators(CommandContext context)
        {
            var builder = new StringBuilder();

            foreach (var language in Localization.Languages)
            {
                var users = language.Authors.Select(async id => await context.Client.GetUserAsync(id)).Select((task) => task.Result);
                var names = users.Select((user) => $"{user.Username}#{user.Discriminator}");
                builder.AppendLine($"**{language.Name}**\n{string.Join(", ", names)}");
            }

            return builder.ToString();
        }

        [Command("stop")]
        [Aliases("shutdown")]
        [Description("Shuts down CraftBot")]
        [RequireOwner]
        public async Task Stop(CommandContext context)
        {
            var bot = context.Services.GetService<DiscordBot>();
            
            await context.Client.UpdateStatusAsync(new DiscordActivity("Shutting down..."), UserStatus.DoNotDisturb);
            await context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("👋"));

            await bot.StopAsync();
        }

        [Command("hopoff")]
        [Aliases("detach")]
        [Description("Switches to a new non-debug instance.")]
        [RequireOwner]
        public async Task HopOff(CommandContext context)
        {
            var bot = context.Services.GetService<DiscordBot>();
            
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

            await context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏫"));

            Process.Start(startInfo);

            await bot.StopAsync();
        }

        [Command("attach")]
        [Description("Attaches a debugger (e.g. Visual Studio)")]
        [RequireOwner]
        public async Task Attach(CommandContext context)
        {
            if (Debugger.IsAttached)
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = "You already have a debugger attached.",
                    Description = "CraftBot can't launch a debugger if one is already attached."
                });

                return;
            }

            var message = await context.RespondAsync(embed: new DiscordEmbedBuilder
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

        [Command("break")]
        [Aliases("pause")]
        [Description("Debugger.Break();")]
        [RequireOwner]
        public async Task Break(CommandContext context)
        {
            if (!Debugger.IsAttached)
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = "No debugger attached!",
                    Description = "CraftBot cannot be 'broken' unless a debugger is attached."
                });

                return;
            }

            await context.Client.UpdateStatusAsync(new DiscordActivity("Breaking"), UserStatus.Idle);
            await context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏸️"));

            Debugger.Break();

            await context.Client.UpdateStatusAsync(new DiscordActivity("Awaking from debugger"), UserStatus.Online);
            await context.Message.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode("⏸️"));
        }

        [Command("statistics")]
        [Aliases("stat", "stats")]
        public async Task ViewStatistics(CommandContext context)
        {
            var language = UserRepository.Get(context.User).GetLanguage(Localization);
            var statistics = context.Services.GetService<Statistics>();
            
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

            await context.RespondAsync("graph.png", embed: embed);
        }

        [Command("embedtest")]
        [RequireOwner]
        public async Task EmbedTest(CommandContext context)
        {
            await context.RespondAsync(embed: new DiscordEmbedBuilder
            {
                Title = "Title" + Emoji.IconCloseCircle,
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = context.Client.CurrentUser.AvatarUrl,
                    Name = "Name " + Emoji.IconCloseCircle
                }
            });
        }
    }
}