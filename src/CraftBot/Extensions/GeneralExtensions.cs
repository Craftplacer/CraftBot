using System;
using System.Collections.Generic;
using System.Linq;
using CraftBot.Localization;

namespace CraftBot.Extensions
{
    public static class GeneralExtensions
    {
        private static readonly Random defaultRandom = new Random();

        /// <summary>
        /// Chooses a defaultRandom element from T.
        /// </summary>
        public static T Random<T>(this IEnumerable<T> source, Random random = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var enumerable = source as T[] ?? source.ToArray();
            var count = enumerable.Length;
            var index = (random ?? defaultRandom).Next(0, count - 1);
            
            return enumerable[index];
        }

        public static string GetString(this TimeSpan timeSpan, Language language = null, bool includeTime = true)
        {
            var localizationAvailable = language == null;
            var strings = new List<string>();
            var years = timeSpan.Days / 365;

            if (years > 0)
                strings.Add(localizationAvailable ? $"{years} year(s)" : language.GetCounter(years, "years"));

            var remainingDays = timeSpan.Days % 365;
            var weeks = remainingDays / 7;
            remainingDays %= 7;
            
            if (weeks > 0)
                strings.Add(localizationAvailable ? $"{weeks} week(s)" : language.GetCounter(weeks, "weeks"));

            if (remainingDays > 0)
                strings.Add(localizationAvailable ? $"{remainingDays} day(s)" : language.GetCounter(remainingDays, "days"));

            if (includeTime)
            {
                if (timeSpan.Hours > 0)
                    strings.Add(localizationAvailable ? $"{timeSpan.Hours} hour(s)" : language.GetCounter(timeSpan.Hours, "hours"));

                if (timeSpan.Minutes > 0)
                    strings.Add(localizationAvailable ? $"{timeSpan.Minutes} minute(s)" : language.GetCounter(timeSpan.Minutes, "minutes"));

                strings.Add(localizationAvailable ? $"{timeSpan.Seconds} second(s)" : language.GetCounter(timeSpan.Seconds, "seconds"));
            }

            return string.Join(", ", strings);
        }

        public static string Cut(this string @string, int maxLength, string end = "...")
        {
            if (@string is null)
                throw new ArgumentNullException(nameof(@string));

            if (maxLength >= @string.Length)
                return @string;

            return @string.Substring(0, maxLength - end.Length) + end;
        }
    }
}