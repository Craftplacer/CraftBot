using System;
using System.Collections.Generic;
using System.IO;

namespace CraftBot.Model
{
    /// <summary>
    /// JSON object for storing tags that are used to mark posts which are either Discord TOS violating or explicit.
    /// </summary>
    public class ForbiddenTags
    {
        private static ForbiddenTags _instance;

        public List<string> DiscordTosProhibited { get; set; } = new List<string>();

        public List<string> ForceExplicit { get; set; } = new List<string>();

        public List<string> ConditionalProhibited { get; set; } = new List<string>();

        public static ForbiddenTags Get()
        {
            if (_instance != null)
                return _instance;

            var path = Path.Combine("config", "forbidden-tags");
            var tags = Helpers.GetJson(path, new ForbiddenTags());

            if (tags.DiscordTosProhibited.Count == 0)
                throw new Exception("TOS list is empty, pre-cautionary exception thrown.");

            _instance = tags;

            return _instance;
        }
    }
}