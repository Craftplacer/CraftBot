using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace CraftBot.Features
{
    [Group("moderation")]
    [Aliases("mod")]
    public class ModerationCommands : BaseCommandModule
    {
        [RequirePermissions(Permissions.BanMembers)]
        [Command("ban")]
        public async Task BanMember(CommandContext context, ulong id, string reason)
        {
            await context.Guild.BanMemberAsync(id, reason: $"Ban issuer: {context.User.Username}#{context.User.Discriminator} ({context.User.Id})\nReason: {reason}");
            await context.RespondAsync(embed:
                new DiscordEmbedBuilder()
                {
                    Color = Colors.LightGreen500,
                    Title = "Ban succeeded"
                }
                .AddField("Banned user", $"<@{id}>", true)
                .AddField("Ban issuer", context.User.Mention, true)
            );
        }

        [RequirePermissions(Permissions.BanMembers)]
        [Command("ban")]
        public Task BanMember(CommandContext context, DiscordUser user, string reason) => BanMember(context, user.Id, reason);
    }
}