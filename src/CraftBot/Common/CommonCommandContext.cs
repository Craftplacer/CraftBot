using System;
using System.Threading.Tasks;
using Qmmands;

namespace CraftBot.Common
{
	public class CommonCommandContext : CommandContext
	{
		public CommonCommandContext(CommonMessage message) : base(null)
		{
			Message = message;
		}

		public CommonUser User => Message.Author;
		
		public CommonMessage Message { get; }

		public Task RespondAsync(string message) => Message.RespondAsync(message);
	}
}