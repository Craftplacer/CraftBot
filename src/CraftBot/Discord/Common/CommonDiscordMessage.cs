using System.Threading.Tasks;
using CraftBot.Common;
using DSharpPlus.Entities;

namespace CraftBot.Discord
{
	public class CommonDiscordMessage : CommonMessage
	{
		public DiscordMessage DiscordMessage { get; }
		
		public CommonDiscordMessage(DiscordMessage message) : base(new CommonDiscordUser(message.Author))
		{
			DiscordMessage = message;
		}
		
		public override string Content => DiscordMessage.Content;
		public override Task RespondAsync(string message) => DiscordMessage.RespondAsync(message);
	}
}