using System;
using System.Collections.Generic;
using System.Linq;

namespace CraftBot.Localization
{
    public class LocalizationEngine
    {
        /// <summary>
        /// <see cref="Language"/> to be used if a localized string can't be found.
        /// </summary>
        public Language FallbackLanguage { get; set; }

        public List<Language> Languages { get; } = new List<Language>();

        /// <summary>
        /// Retrives a localized string with the specified <paramref name="key"/>
        /// </summary>
        /// <returns>The localized string. If the key is not translated it use the fallback language, in the worst case it will return "#key".</returns>
        public string GetString(string key, Language localizedLanguage = null)
        {
            if (localizedLanguage != null && localizedLanguage.Strings.ContainsKey(key))
                return localizedLanguage.Strings[key];

            if (FallbackLanguage != null && FallbackLanguage.Strings.ContainsKey(key))
                return FallbackLanguage.Strings[key];

            // Fallback to key when absolutely nothing is found
            return "#" + key;
        }

        public Language GetLanguage(string code) => Languages.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }
}