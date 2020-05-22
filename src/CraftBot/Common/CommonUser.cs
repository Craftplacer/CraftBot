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

		/// <summary>
		/// A string of text that causes this user to be notified when included.
		/// </summary>
		public abstract string Mention { get; }
		
		public abstract Task MessageAsync(string message);
	}
}