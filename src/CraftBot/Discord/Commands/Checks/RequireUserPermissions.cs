using System;
using System.Linq;
using System.Threading.Tasks;
using CraftBot.Discord.Commands;
using DSharpPlus;
using Qmmands;

namespace Disqord.Bot
{
    public sealed class RequireUserPermissions : GuildOnlyAttribute
    {
        public Permissions Permissions { get; }

        public RequireUserPermissions(Permissions permissions)
        {
            Permissions = permissions;
        }

        public RequireUserPermissions(params Permissions[] permissions)
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
            var permissions = context.Member.PermissionsIn(context.Channel);
            return permissions.HasFlag(Permissions)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful($"You lack the necessary channel permissions ({Permissions - permissions}) to execute this.");
        }
    }
}
