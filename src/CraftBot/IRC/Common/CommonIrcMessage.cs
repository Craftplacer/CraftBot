using System.Threading.Tasks;
using CraftBot.Common;
using Craftplacer.IRC.Entities;

namespace CraftBot.IRC
{
	public class CommonIrcMessage : CommonMessage
	{
		// TODO: Implement CommonIrcUser
		public CommonIrcMessage(IrcMessage ircMessage) : base(null)
		{
			IrcMessage = ircMessage;
		}

		public IrcMessage IrcMessage { get; }
		public override string Content => IrcMessage.Message;
		
		public override Task RespondAsync(string message) => IrcMessage.RespondAsync(message);
	}
}