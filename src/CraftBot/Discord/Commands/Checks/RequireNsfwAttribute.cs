using System.Threading.Tasks;
using CraftBot.Discord.Commands;
using Qmmands;

namespace Disqord.Bot
{
    public sealed class RequireNsfwAttribute : CheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            var context = _ as DiscordCommandContext;
            return context.Channel.IsNSFW
                ? CheckResult.Successful
                : CheckResult.Unsuccessful($"This can only be executed in NSFW channels.");
        }
    }
}
