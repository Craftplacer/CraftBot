using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JetBrains.Annotations;

namespace CraftBot.API
{
	public class LightshotBot
    {
        private const string UrlCharacters = "abcdefghijklmnopqrstuvwxyz1234567890";
        private readonly HttpClient _httpClient;
        private static readonly Random random = new Random();

        public LightshotBot()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36");
        }
        
        [CanBeNull]
        public async Task<string> FindRandomImageAsync()
        {
            // Form/Generate URL
            var url = GetRandomUrl();

            // Try out
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);

            // Check whether it failed or not
            if (!response.IsSuccessStatusCode)
            {
                Logger.Info("Not success: " + response.StatusCode, "Random Lightshot");
                return null;
            }

            // Parse response
            var html = await response.Content.ReadAsStringAsync();
            var imageUrl = FindImageUrl(html);

            return imageUrl;
        }

        private static string FindImageUrl(string htmlBody)
        {
            var document = new HtmlDocument();
            document.LoadHtml(htmlBody);
            return FindImageUrl(document);
        }

        private static string FindImageUrl(HtmlDocument document)
        {
            var htmlBody = document.DocumentNode.SelectSingleNode("//body");
            var constrain = htmlBody.ChildNodes.First(n => n.HasClass("image-constrain"));
            var container = constrain.ChildNodes.First(n => n.HasClass("image-container"));
            var image = container.ChildNodes.First(n => n.Id == "screenshot-image" && n.Name == "img");
            var imageUrl = image.Attributes["src"].Value;
            
            if (imageUrl.StartsWith("//") || !imageUrl.StartsWith("http"))
                imageUrl = "https:" + imageUrl;

            return imageUrl;
        }

        private string GetRandomUrl()
        {
            var stringBuilder = new StringBuilder("https://prntscr.com/");
            
            for (var i = 0; i < 6; i++)
            {
                var j = random.Next(0, UrlCharacters.Length - 1);
                var @char = UrlCharacters[j];
                stringBuilder.Append(@char);
            }

            return stringBuilder.ToString();
        }
	}
}