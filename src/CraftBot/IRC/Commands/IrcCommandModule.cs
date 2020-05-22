using System.Threading.Tasks;
using Qmmands;

namespace CraftBot.IRC
{
	public class IrcCommandModule : ModuleBase<IrcCommandContext>
	{
		[Command("vagene")]
		public async Task TestAsync()
		{
			await Context.RespondAsync("hello to irc user");
		}
	}
}