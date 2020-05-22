using System.Collections.Generic;
using CraftBot.IRC;

namespace CraftBot
{
    public class Config
    {
        public string Prefix { get; set; }
        public Dictionary<TokenType, string> Tokens { get; set; } = new Dictionary<TokenType, string>();
        public IrcHostInformation[] IrcHosts { get; set; } = new IrcHostInformation[0];
    }

    public enum TokenType
    {
        Discord,
        Sentry,
        Osu,
    }
}