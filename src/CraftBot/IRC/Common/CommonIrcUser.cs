using System;
using System.Threading.Tasks;
using CraftBot.Common;
using Craftplacer.IRC.Entities;
using JetBrains.Annotations;

namespace CraftBot.IRC
{
	public class CommonIrcUser : CommonUser
	{
		public CommonIrcUser([NotNull] IrcUser ircUser)
		{
			IrcUser = ircUser ?? throw new ArgumentNullException(nameof(ircUser));
			throw new NotImplementedException();
		}

		[NotNull]
		public IrcUser IrcUser { get; }

		public override string Username => IrcUser.Nickname;
		public override string Nickname => IrcUser.Nickname;
		public override string Mention => IrcUser.Nickname;
		
		public override async Task MessageAsync(string message)
		{
			throw new NotImplementedException();
		}
	}
}