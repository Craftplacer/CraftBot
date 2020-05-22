using System;
using System.Linq;
using System.Threading.Tasks;
using CraftBot.Discord.Commands;
using DSharpPlus;
using Qmmands;

namespace Disqord.Bot
{
    public sealed class RequireBotPermissionsAttribute : GuildOnlyAttribute
    {
        public Permissions Permissions { get; }

        public RequireBotPermissionsAttribute(Permissions permissions)
        {
            Permissions = permissions;
        }

        public RequireBotPermissionsAttribute(params Permissions[] permissions)
        {
            if (permissions == null)
                throw new ArgumentNullException(nameof(permissions));

            Permissions = permissions.Aggregate(Permissions.None, (total, permission) => total | permission);
        }

        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            var baseResult = base.CheckAsync(_).Result;
            if (!baseResult.IsSuccessful)
                return baseResult;

            var context = _ as DiscordCommandContext;
            var permissions = context.Guild.CurrentMember.PermissionsIn(context.Channel);
            return permissions.HasFlag(Permissions)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful($"The bot lacks the necessary channel permissions ({Permissions - permissions}) to execute this.");
        }
    }
}
