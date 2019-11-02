using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace CraftBot.Features
{
    [Group("bot")]
    public class BotCommands : BaseCommandModule
    {
        [Command("info")]
        [GroupCommand]
        public async Task Info(CommandContext context)
        {
            string getUptime()
            {
                var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;

                return uptime.GetString();
            }

            await context.RespondAsync(embed: new DiscordEmbedBuilder()
            {
                Color = Colors.Indigo500,
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    IconUrl = context.Client.CurrentUser.AvatarUrl,
                    Name = context.Client.CurrentUser.Username,
                    Url = "https://craftplacer.trexion.com/projects/craftbot"
                },
                Description = "Multi-purpose bot for various things, currently being rewritten to fit code quality. **Expect inconsistant downtime because of debugging and coding.**"
            }
            .AddField("Uptime", getUptime(), true)
            .AddField("Servers", context.Client.Guilds.Count.ToString(), true)
            .AddField("Commands", Program.CommandsNext.RegisteredCommands.Count.ToString(), true));
        }

        [Command("stop")]
        [RequireOwner]
        public async Task Stop(CommandContext context)
        {
            await context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("👋"));
            await Program.Shutdown();
        }

        [Command("shrink")]
        [Description("Shrinks CraftBot's database")]
        [RequireOwner]
        public async Task ShrinkDatabase(CommandContext context)
        {
            var bytes = Program.Database.Shrink();

            await context.RespondAsync(embed: new DiscordEmbedBuilder()
            {
                Description = $"Database's size has been shrunk by {bytes / 1000} KB"
            });
        }
    }
}