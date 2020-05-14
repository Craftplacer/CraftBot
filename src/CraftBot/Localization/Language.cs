#nullable enable

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CraftBot.Localization
{
    public class Language
    {
        public Language(LocalizationEngine engine, string code, string name, Dictionary<string, string> strings)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("The language code can't be null or a whitespace.", nameof(code));
            }
            else
            {
                Code = code;
                
                try
                {
                    CultureInfo = new CultureInfo(Code);
                }
                catch (CultureNotFoundException)
                {
                    CultureInfo = CultureInfo.InvariantCulture;
                }
            }
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("The language name can't be null or a whitespace.", nameof(name));
            else
                Name = name;

            Strings = strings ?? throw new ArgumentNullException(nameof(strings));
        }

        public LocalizationEngine Engine { get; }

        public string this[string key, params object?[] values]
        {
            get
            {
                if (Strings.ContainsKey(key))
                {
                    if (values == null || values.Length == 0)
                        return Strings[key];
                    else
                        return string.Format(this[key], values);
                }

                if (Engine.FallbackLanguage?.Strings.ContainsKey(key) == true)
                    return Engine.FallbackLanguage[key, values];

                return '#' + key;
            }
            set => Strings[key] = value;
        }

        public List<ulong> Authors { get; set; } = new List<ulong>();

        public string Code { get; set; }

        public string Name { get; set; }

        public string Flag { get; private set; } = "🏴";

        public CultureInfo CultureInfo { get; }

        public float Completion
        {
            get
            {
                if (Engine.FallbackLanguage == this)
                    return 100f;

                var allKeys = (float)Engine.FallbackLanguage.Keys.Count;
                var translatedKeys = (float)Engine.FallbackLanguage.Keys.Count(key => ContainsKey(key));

                return translatedKeys / allKeys * 100;
            }
        }

        public Dictionary<string, string> Strings { get; set; }

        public IReadOnlyList<string> Keys => Strings.Keys.ToList();

        public IEnumerable<string> Values => Strings.Values.ToList();

        public string GetCounter(int count, string key)
        {
            if (Code.Equals("pl", StringComparison.OrdinalIgnoreCase))
            {
                if (count == 1)
                {
                    var k = $"counters.{key}.n";
                    if (ContainsKey(k))
                        return this[k, count];
                }
                else
                {
                    var lastDigit = int.Parse(count.ToString()[^1].ToString());

                    if (2 <= lastDigit && lastDigit <= 4)
                    {
                        var k = $"counters.{key}.a";
                        if (ContainsKey(k))
                            return this[k, count];
                    }
                    else
                    {
                        var k = $"counters.{key}.g";
                        if (ContainsKey(k))
                            return this[k, count];
                    }
                }
            }
            else
            {
                if (count == 1)
                {
                    var k = $"counters.{key}.singular";
                    if (ContainsKey(k))
                        return this[k, count];
                }

                // Checks if there's a string for a specific count
                {
                    var k = $"counters.{key}.{count}";
                    if (ContainsKey(k))
                        return this[k, count];
                }

                if (count > 1)
                {
                    var k = $"counters.{key}.plural";
                    if (ContainsKey(k))
                        return this[k, count];
                }
            }

            if (Engine.FallbackLanguage == this)
            {
                return $"{count} #{key}";
            }
            else
            {
                return Engine.FallbackLanguage.GetCounter(count, key);
            }
        }

        public static Language FromJson(LocalizationEngine engine, string json)
        {
            var jObject = (JObject)JsonConvert.DeserializeObject(json);

            var code = jObject["code"].ToObject<string>();
            var name = jObject["name"].ToObject<string>();
            var strings = jObject["strings"].ToObject<Dictionary<string, string>>();

            return new Language(engine, code, name, strings)
            {
                Flag = jObject["flag"].ToObject<string>(),
                Authors = jObject["authors"]?.ToObject<List<ulong>>()
            };
        }

        public string ToJson() => JsonConvert.SerializeObject
        (
            new
            {
                name = Name,
                code = Code,
                flag = Flag,
                authors = Authors,
                strings = Strings
            },
            Formatting.Indented
        );

        public bool ContainsKey(string key) => Strings.ContainsKey(key);
    }
}