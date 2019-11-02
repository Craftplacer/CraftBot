using CraftBot.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace CraftBot.API
{
    public static class BooruAPI
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<List<BooruApiResult>> GetPageAsync(int page = 0, int max = 90, string query = null, BooruApiQuality f = BooruApiQuality.Safe, BooruApiOrder o = BooruApiOrder.DateDescending)
        {
            string url = $"https://cure.ninja/booru/api/json/{page}?s={max}";

            if (f != BooruApiQuality.Safe)
                url += $"&f={f.getChar()}";

            if (o != BooruApiOrder.DateDescending)
                url += $"&f={f.getChar()}";

            if (!string.IsNullOrWhiteSpace(query))
                url += $"&q={HttpUtility.UrlEncode(query)}";

            var httpResponse = await client.GetAsync(url);
            var json = await httpResponse.Content.ReadAsStringAsync();

            try
            {
                var response = (JObject)JsonConvert.DeserializeObject(json);

                if (!response["success"].ToObject<bool>())
                    throw new ApiException("The server failed to process the request.");

                return response["results"].ToObject<List<BooruApiResult>>();
            }
            catch (JsonException ex)
            {
                throw new ApiException("Malformed JSON", ex);
            }
        }

        private static char getChar(this BooruApiOrder o) => o switch
        {
            BooruApiOrder.RandomOrder => 'r',
            BooruApiOrder.NoOrder => 'n',
            BooruApiOrder.DateDescending => 'd',
            _ => throw new ArgumentException(nameof(o))
        };

        private static char getChar(this BooruApiQuality f) => f switch
        {
            BooruApiQuality.Safe => 's',
            BooruApiQuality.Questionable => 'q',
            BooruApiQuality.Explicit => 'e',
            BooruApiQuality.Any => 'a',
            _ => throw new ArgumentException(nameof(f))
        };
    }

    public enum BooruApiQuality
    {
        Safe,
        Questionable,
        Explicit,
        Any
    }

    public enum BooruApiOrder
    {
        RandomOrder,
        NoOrder,
        DateDescending
    }

    public class BooruApiResult
    {
        public string Source { get; set; }

        public string SourceUrl { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public string UserUrl { get; set; }

        public string Id { get; set; }

        public string Page { get; set; }

        public string Url { get; set; }

        [JsonIgnore]
        public BooruApiQuality Quality => _JsonQuality switch
        {
            "s" => BooruApiQuality.Safe,
            "q" => BooruApiQuality.Questionable,
            "e" => BooruApiQuality.Explicit,
            _ => BooruApiQuality.Explicit
        };

        [JsonProperty("rating")]
        private string _JsonQuality { get; set; }

        [JsonIgnore]
        public List<string> Tags { get; set; }

        [JsonProperty("tags")]
        private string _JsonTags
        {
            set => Tags = value.Split(' ').ToList();
        }
    }
}