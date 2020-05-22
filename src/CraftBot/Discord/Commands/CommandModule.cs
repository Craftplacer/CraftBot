using CraftBot.Localization;
using CraftBot.Repositories;

namespace CraftBot.Discord.Commands
{
	public class CommandModule
	{
		public LocalizationEngine Localization { get; set; }
		public GuildRepository GuildRepository { get; set; }
		public UserRepository UserRepository { get; set; }
	}
}