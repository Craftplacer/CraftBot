using System.Linq;
using System.Threading.Tasks;
using CraftBot.Discord.Commands;
using Qmmands;

namespace Disqord.Bot
{
    public sealed class BotOwnerOnlyAttribute : CheckAttribute
    {
        public override async ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            var context = _ as DiscordCommandContext;
            return context.Client.CurrentApplication.Owners.Any(owner => owner.Id == context.User.Id)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful("This can only be executed by the bot's owner.");
            // switch (context.Client.CurrentUser.)
            // {
            //     case TokenType.Bot:
            //     {
            //         
            //     }
	        // 
            //     case TokenType.Bearer:
            //     case (TokenType)0:
            //     {
            //         return context.Client.CurrentUser.Id == context.User.Id
            //             ? CheckResult.Successful
            //             : CheckResult.Unsuccessful("This can only be executed by the currently logged in user.");
            //     }
            // 
            //     default:
            //         throw new InvalidOperationException("Invalid token type.");
            // }
        }
    }
}
