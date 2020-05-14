using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace CraftBot.API
{
    public static class YandereApi
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<IReadOnlyList<YanderePost>> GetPost(string[] tags, string rating, int limit)
        {
            string GetTags()
            {
                return string.Join("%20", tags.Concat(new[] {
                    "order:random",
                    $"rating:{rating}"
                }).Select(tag => HttpUtility.UrlEncode(tag)));
            }

            var urlTags = GetTags();
            var json = await client.GetStringAsync($"https://yande.re/post.json?limit={limit}&tags={urlTags}");
            return JsonConvert.DeserializeObject<List<YanderePost>>(json);

            //tags=order%3Arandom%20rating%3Asafe%20cirno
        }
    }
}