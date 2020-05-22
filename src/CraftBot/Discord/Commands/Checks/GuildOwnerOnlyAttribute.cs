using System.Threading.Tasks;
using CraftBot.Discord.Commands;
using Qmmands;

namespace Disqord.Bot
{
    public sealed class GuildOwnerOnlyAttribute : GuildOnlyAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            var baseResult = base.CheckAsync(_).Result;
            if (!baseResult.IsSuccessful)
                return baseResult;

            var context = _ as DiscordCommandContext;
            return context?.User.Id == context.Guild.Owner.Id
                ? CheckResult.Successful
                : CheckResult.Unsuccessful("This can only be executed by the guild's owner.");
        }
    }
}
