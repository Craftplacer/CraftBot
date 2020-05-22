using CraftBot.API;
using CraftBot.Extensions;
using CraftBot.Model;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace CraftBot.Commands
{
    [Group("yandere")]
    public class BooruCommands : BaseCommandModule
    {
        [Command("search")]
        [GroupCommand]
        [Description("Searches for your query")]
        public async Task Search(CommandContext context, params string[] tags)
        {
            var rating = context.Channel.IsNSFW ? "explicit" : "safe";

            var results = await YandereApi.GetPost(tags, rating, 10);
            var filtered = results.Where(r => IsSafe(r, rating == "explicit"));

            if (filtered.Any())
            {
                await context.RespondAsync(embed: GetEmbed(filtered.Random()));
            }
            else
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = "No search results.",
                });
            }
        }

        private static bool IsSafe(YanderePost post, bool searchingForExplicit)
        {
            var forbiddenTags = ForbiddenTags.Get();

            if (post.Tags.Any(t => forbiddenTags.DiscordTosProhibited.Contains(t)))
                return false;

            var hasExplicitTags = post.Tags.Any(t => forbiddenTags.ForceExplicit.Contains(t));
            var isExplicit = post.Rating == "e" || hasExplicitTags;

            if (isExplicit && post.Tags.Any(t => forbiddenTags.ConditionalProhibited.Contains(t)))
                return false;

            if (!searchingForExplicit && isExplicit)
                return false;

            //if (result.Quality != q)
            //    return false;

            return true;
        }

        private static DiscordEmbedBuilder GetEmbed(YanderePost post)
        {
            return new DiscordEmbedBuilder
            {
                //Author = new DiscordEmbedBuilder.EmbedAuthor()
                //{
                //    Name = post.UserName,
                //    Url = post.UserUrl
                //},
                Color = new DiscordColor("ee8887"),
                Title = post.Id.ToString(),
                Url = "https://yande.re/",
                ImageUrl = post.FileUrl,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "yande.re"
                }
            };
        }
    }
}