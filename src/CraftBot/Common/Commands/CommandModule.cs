using System;
using System.Threading.Tasks;
using Qmmands;

namespace CraftBot.Common.Commands
{
	public class CommandModule : ModuleBase<CommonCommandContext>
	{
		private static readonly Random rng = new Random();
		
		[Command("roll", "dice")]
		[Description("Roll the dice")]
		public async Task RollAsync()
		{
			const int rollMax = 100;

			var rolledNumber = rng.Next(1, rollMax);
			
			await Context.RespondAsync($"{Context.User.Nickname} rolled a {rolledNumber}!");
		}
	}
}