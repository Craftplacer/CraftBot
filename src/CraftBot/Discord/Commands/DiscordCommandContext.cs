using System;
using System.Threading.Tasks;
using CraftBot.Common;
using CraftBot.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using JetBrains.Annotations;

namespace CraftBot.Discord.Commands
{
	public class DiscordCommandContext : CommonCommandContext
	{
		public DiscordCommandContext([NotNull] DiscordBot bot, [NotNull] DiscordMessage message, IServiceProvider serviceProvider = null) : base(serviceProvider)
		{
			Bot = bot ?? throw new ArgumentNullException(nameof(bot));
			Message = message ?? throw new ArgumentNullException(nameof(message));
		}

		[NotNull]
		public DiscordBot Bot { get; }

		public DiscordClient Client => Bot.Client;
		
		public DiscordUser User => Message.Author;
		
		[NotNull]
		public DiscordMessage Message { get; }

		public DiscordGuild Guild => Channel.Guild;

		public DiscordChannel Channel => Message.Channel;

		private DiscordMember _member;
		private bool _memberQueried = false;
		
		public DiscordMember Member
		{
			get
			{
				if (_member == null && !_memberQueried)
				{
					_memberQueried = true;
					_member = Guild.TryGetMemberAsync(User.Id).GetAwaiter().GetResult();
				}

				return _member;
			}
		}
		
		public override CommonMessage CommonMessage => new CommonDiscordMessage(Message);
		public override Task RespondAsync(string message) => Message.RespondAsync(message);

		public Task<DiscordMessage> RespondAsync(DiscordEmbed embed) => Message.RespondAsync(embed: embed);
	}
}