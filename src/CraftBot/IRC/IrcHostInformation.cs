namespace CraftBot.IRC
{
	public class IrcHostInformation
	{
		public string Host { get; set; }
		public int Port { get; set; }
		public string Nickname { get; set; }
		public string NickServPassword { get; set; }
		public string[] AutoJoinChannels { get; set; } = new string[0];
	}
}