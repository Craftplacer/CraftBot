using CraftBot.Database;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CraftBot.Features
{
    [Group("server")]
    [RequireGuild]
    public class GuildCommands : BaseCommandModule
    {
        [Command("info")]
        [GroupCommand]
        public async Task Info(CommandContext context)
        {
            var data = GuildData.Get(context.Guild);
            var embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Guild.Name, null, context.Guild.IconUrl)

                .WithDescription(@$"{Emoji.ICON_CALENDAR_PLUS} **Creation Date:** {context.Guild.CreationTimestamp.ToString(Program.CultureInfo.DateTimeFormat)}
                                    {Emoji.ICON_ACCOUNT_MULTIPLE} **Member count:** {context.Guild.MemberCount}");

            if (data.Features.Count > 0)
                embed = embed.AddField("Features", string.Join(", ", data.Features));

            await context.RespondAsync(embed: embed);
        }

        [Command("emoji")]
        public async Task ListEmoji(CommandContext context)
        {
            string description = string.Empty;

            var emojis = await context.Guild.GetEmojisAsync();
            foreach (var item in emojis)
                description += $"{item.ToString()} `{item.Id}`\n";

            var pages = Program.Interactivity.GeneratePagesInContent(description, SplitType.Line); ;

            await Program.Interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
        }

        [Group("features")]
        private class Features : BaseCommandModule
        {
            [Command("enable")]
            [RequireUserPermissions(Permissions.Administrator)]
            public async Task Enable(CommandContext context, string name)
            {
                name = name.ToLowerInvariant();

                var data = GuildData.Get(context.Guild);
                if (data.Features.Contains(name))
                {
                    await context.RespondAsync($"This server already has feature `{name}` enabled.");
                    return;
                }

                data.Features.Add(name);
                data.Save();

                await context.RespondAsync($"Feature `{name}` has been enabled.");
            }

            [Command("disable")]
            [RequireUserPermissions(Permissions.Administrator)]
            public async Task Disable(CommandContext context, string name)
            {
                name = name.ToLowerInvariant();

                var data = GuildData.Get(context.Guild);
                if (!data.Features.Contains(name))
                {
                    await context.RespondAsync($"This server already has feature `{name}` disabled.");
                    return;
                }

                data.Features.Remove(name);
                data.Save();

                await context.RespondAsync($"Feature `{name}` has been disabled.");
            }
        }
    }
}