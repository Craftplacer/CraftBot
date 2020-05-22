using System;
using System.Threading.Tasks;
using CraftBot.Common;
using Craftplacer.IRC.Entities;
using JetBrains.Annotations;

namespace CraftBot.IRC
{
	public class IrcCommandContext : CommonCommandContext
	{
		public IrcCommandContext([NotNull] IrcMessage message, IServiceProvider serviceProvider = null) : base(serviceProvider)
		{
			Message = message ?? throw new ArgumentNullException(nameof(message));
		}

		public IrcUser User => Message.Author;
		
		public IrcMessage Message { get; }

		public IrcChannel Channel => Message.Channel;

		public override CommonMessage CommonMessage => new CommonIrcMessage(Message);
		public override Task RespondAsync(string message) => Message.RespondAsync(message);
	}
}