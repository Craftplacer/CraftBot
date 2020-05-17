using System.Threading.Tasks;

namespace CraftBot.Common
{
	/// <summary>
	/// Abstraction class for common user properties and methods
	/// </summary>
	public abstract class CommonUser
	{
		public abstract string Username { get; }
		
		public abstract string Nickname { get; }

		public abstract Task MessageAsync(string message);
	}
}