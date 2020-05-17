using System.Threading.Tasks;

namespace CraftBot.Common
{
	public abstract class CommonMessage
	{
		protected CommonMessage(CommonUser author) => Author = author;

		public CommonUser Author { get; }
		public abstract string Content { get; }

		public abstract Task RespondAsync(string message);
	}
}