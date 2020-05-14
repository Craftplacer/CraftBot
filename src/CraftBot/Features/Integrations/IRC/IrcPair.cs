namespace CraftBot.Features.Integrations.IRC
{
    public class IrcPair
    {
        public IrcPair(string host, string channel)
        {
            Host = host;
            Channel = channel;
        }
        
        public string Host { get; }
        public string Channel { get; }

        public string Password { get; set; }
    }
}