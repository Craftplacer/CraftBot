using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using CraftBot.Extensions;
using CraftBot.Localization;
using CraftBot.Repositories;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

using HtmlAgilityPack;

namespace CraftBot.Commands
{
    public class MainCommands : BaseCommandModule
    {
        public LocalizationEngine Localization { get; set; }
        public GuildRepository GuildRepository { get; set; }
        public UserRepository UserRepository { get; set; }

        private static Random Random = new Random();

        private static Random _getRandom(string seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
                return new Random();

            int GetIntegerSeed() => seed.ToLowerInvariant().Aggregate(0, (current, @char) => current + @char);
            return new Random(GetIntegerSeed());
        }

        [Command("roll")]
        [Description("Rolls the dice")]
        public async Task Roll(CommandContext context, [RemainingText] string seed = "")
        {
            const int rollMax = 100;

            var embed = new DiscordEmbedBuilder
            {
                Title = "The totally fair dice\\™",
                Description = "Rolling the dice..."
            };
            var message = await context.RespondAsync(embed: embed);

            await Task.Delay(Random.Next(1, 3) * 1000);
            embed.WithDescription($"The dice has rolled a {_getRandom(seed).Next(1, rollMax)}!");
            await message.ModifyAsync(embed: embed.Build());
        }

        [Command("8ball")]
        [Description("Ask the 8-Ball.")]
        public async Task Ask8Ball(CommandContext context, [RemainingText] string seed = "")
        {
            var answers = new[]
            {
                "It is certain",
                "Don’t count on it",
                "Outlook good",
                "Outlook not so good",
                "You may rely on it",
                "My sources say no",
                "Without a doubt",
                "Very doubtful",
                "Yes definitely",
                "My reply is no"
            };

            var embed = new DiscordEmbedBuilder
            {
                Title = "Totally fair 8ball\\™",
                Description = "..."
            };
            var message = await context.RespondAsync(embed: embed);

            await Task.Delay(Random.Next(1, 3) * 1000);
            embed.WithDescription(answers.Random(_getRandom(seed)));
            await message.ModifyAsync(embed: embed.Build());
        }

        [Command("lightshot")]
        [Aliases("rls")]
        [RequireNsfw]
        public async Task RandomLightshot(CommandContext context, int max = 1)
        {
            var interactivity = context.Client.GetExtension<InteractivityExtension>();

            const string chars = "abcdefghijklmnopqrstuvwxyz1234567890";
            const int maxFailedAttempts = 5;
            const int waitTime = 2500;

            var firstAttempt = true;
            var failedAttempts = 0;
            var screenshots = new List<string>();
            // (total screenshots - 1) * (delay + average page load) / total ms of a second = ETA in seconds
            var eta = Math.Round((max - 1f) * (waitTime + 100) / 1000, 2);
            var message = await context.RespondAsync(
                $"Searching for screenshots...\nThis may take a while, estimated wait time is {eta} seconds.");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36");

            while (failedAttempts <= maxFailedAttempts && screenshots.Count < max)
            {
                // Form/Generate URL
                var url = new StringBuilder("https://prntscr.com/");
                for (var i = 0; i < 6; i++)
                {
                    var j = Random.Next(0, chars.Length - 1);
                    var @char = chars[j];
                    url.Append(@char);
                }

                // Wait a moment before sending another request
                if (firstAttempt)
                    firstAttempt = false;
                else
                    await Task.Delay(waitTime);

                // Try out
                using var response =
                    await httpClient.GetAsync(url.ToString(), HttpCompletionOption.ResponseContentRead);

                // Check whether it failed or not
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Info("Not success: " + response.StatusCode, "Random Lightshot");
                    failedAttempts++;
                    continue;
                }

                // Parse response
                var html = await response.Content.ReadAsStringAsync();
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var htmlBody = document.DocumentNode.SelectSingleNode("//body");
                var constrain = htmlBody.ChildNodes.First(n => n.HasClass("image-constrain"));
                var container = constrain.ChildNodes.First(n => n.HasClass("image-container"));
                var image = container.ChildNodes.First(n => n.Id == "screenshot-image" && n.Name == "img");
                var imageUrl = image.Attributes["src"].Value;

                if (imageUrl.StartsWith("//") || !imageUrl.StartsWith("http"))
                    imageUrl = "https:" + imageUrl;

                screenshots.Add(imageUrl);
            }

            if (screenshots.Any())
            {
                var pages = screenshots.Select(url => new Page(embed: new DiscordEmbedBuilder
                {
                    ImageUrl = url
                })).ToList();

                if (pages.Count == 1 || context.Channel.IsPrivate)
                    await message.ModifyAsync(string.Empty, pages.First().Embed);
                else
                    await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
            }
            else
                await message.ModifyAsync(
                    $"No screenshots have been found with {failedAttempts} failed attempts :thinking:");
        }
    }
}