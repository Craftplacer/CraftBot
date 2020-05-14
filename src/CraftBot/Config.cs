using System.Collections.Generic;

namespace CraftBot
{
    public class Config
    {
        public Dictionary<TokenType, string> Tokens { get; set; } = new Dictionary<TokenType, string>();
    }

    public enum TokenType
    {
        Discord,
        Sentry,
        Osu,
    }
}