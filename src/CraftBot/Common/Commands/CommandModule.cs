using System;
using System.Threading.Tasks;
using CraftBot.API;
using CraftBot.Extensions;
using Qmmands;

namespace CraftBot.Common.Commands
{
	public class CommandModule : ModuleBase<CommonCommandContext>
	{
		private static readonly Random rng = new Random();
		private LightshotBot _lightshot;
		
		[Command("roll", "dice")]
		[Description("Roll the dice")]
		public async Task RollAsync()
		{
			const int rollMax = 100;

			var rolledNumber = rng.Next(1, rollMax);
			
			await Context.RespondAsync($"{Context.CommonUser.Nickname} rolled a {rolledNumber}!");
		}
		
		[Command("8ball")]
		[Description("Ask the 8-Ball.")]
		public async Task Ask8Ball([Remainder]string question = "")
		{
			var answers = new[]
			{
				"It is certain",
				"Don’t count on it",
				"Outlook good",
				"Outlook not so good",
				"You may rely on it",
				"My sources say no",
				"Without a doubt",
				"Very doubtful",
				"Yes definitely",
				"My reply is no"
			};

			var answer = answers.Random(rng);
			
			await Context.RespondAsync($"{Context.CommonUser.Mention}: The 8-ball says \"{answer}\"");
		}

		[Command("lightshot", "ls")]
		public async Task RandomLightshotImage()
		{
			_lightshot ??= new LightshotBot();

			string imageUrl = null;
			
			var tries = 0;
			while (tries < 5)
			{
				imageUrl = await _lightshot.FindRandomImageAsync();	
				tries++;
			}

			if (imageUrl == null)
			{
				await Context.RespondAsync($"{Context.CommonUser.Mention}: Couldn't find a screenshot after {tries} tries");
				return;
			}
			
			await Context.RespondAsync($"{Context.CommonUser.Mention}: Here's a random screenshot {imageUrl}");
		}
	}
}