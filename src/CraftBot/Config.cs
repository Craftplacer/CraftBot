using System.Collections.Generic;

namespace CraftBot
{
    public class Config
    {
        public string Prefix { get; set; }
        public Dictionary<TokenType, string> Tokens { get; set; } = new Dictionary<TokenType, string>();
    }

    public enum TokenType
    {
        Discord,
        Sentry,
        Osu,
    }
}