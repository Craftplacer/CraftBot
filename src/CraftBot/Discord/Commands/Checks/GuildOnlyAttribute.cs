using System.Threading.Tasks;
using CraftBot.Discord.Commands;
using Qmmands;

namespace Disqord.Bot
{
    public class GuildOnlyAttribute : CheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            var context = _ as DiscordCommandContext;
            return context?.Guild != null
                ? CheckResult.Successful
                : CheckResult.Unsuccessful("This can only be executed in a guild.");
        }
    }
}
