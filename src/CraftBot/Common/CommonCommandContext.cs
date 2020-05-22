using System;
using System.Threading.Tasks;
using Qmmands;

namespace CraftBot.Common
{
	public abstract class CommonCommandContext : CommandContext
	{
		public CommonCommandContext(IServiceProvider serviceProvider = null) : base(serviceProvider)
		{
		}

		public CommonUser CommonUser => CommonMessage.Author;
		
		public abstract CommonMessage CommonMessage { get; }

		public abstract Task RespondAsync(string message);
	}
}