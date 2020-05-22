using System;
using System.Threading.Tasks;
using CraftBot.Common;
using DSharpPlus.Entities;

namespace CraftBot.Discord
{
	public class CommonDiscordUser : CommonUser
	{
		public CommonDiscordUser(DiscordUser user) => DiscordUser = user;

		public DiscordUser DiscordUser { get; }
		public override string Username => DiscordUser.Username;
		public override string Nickname
		{
			get
			{
				if (DiscordUser is DiscordMember member)
					return member.Nickname;

				return DiscordUser.Username;
			}
		}

		public override string Mention => DiscordUser.Mention;

		public override async Task MessageAsync(string message)
		{
			if (!(DiscordUser is DiscordMember member))
				throw new InvalidOperationException("Cannot message Discord users that aren't members.");

			await member.SendMessageAsync(message);
		}
	}
}