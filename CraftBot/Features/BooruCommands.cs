using CraftBot.API;
using CraftBot.Model;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CraftBot.Features
{
    [Group("booru")]
    public class BooruCommands : BaseCommandModule
    {
        [Command("auto")]
        [GroupCommand]
        [Description("Automatically gives you a random picture, also depending on the chanenl type.")]
        public async Task Auto(CommandContext context)
        {
            var q = context.Channel.IsNSFW ? BooruApiQuality.Explicit : BooruApiQuality.Safe;
            var singlePost = !context.Channel.PermissionsFor(await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id)).HasFlag(Permissions.ManageMessages);
            var forbiddenTags = ForbiddenTags.Get();

            var results = await BooruAPI.GetPageAsync(0, max: singlePost ? 3 : 30, f: q, o: BooruApiOrder.RandomOrder);
            var filtered = results.Where(r => IsSafe(r, q == BooruApiQuality.Explicit));

            if (singlePost)
            {
                await context.RespondAsync(embed: getEmbed(filtered.First()));
            }
            else
            {
                var pages = new List<Page>();

                foreach (var result in filtered)
                    pages.Add(new Page(embed: getEmbed(result)));

                await Program.Interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
            }
        }

        [Command("search")]
        [GroupCommand]
        [Description("Searches for your query")]
        public async Task Search(CommandContext context, [RemainingText] string query)
        {
            var q = context.Channel.IsNSFW ? BooruApiQuality.Explicit : BooruApiQuality.Safe;

            var results = await BooruAPI.GetPageAsync(0, 200, query, q, BooruApiOrder.RandomOrder);
            var filtered = results.Where(r => IsSafe(r, q == BooruApiQuality.Explicit));

            if (filtered.Count() == 0)
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder()
                {
                    Color = Colors.Red500,
                    Title = "No search results.",
                });
                return;
            }

            await context.RespondAsync(embed: getEmbed(filtered.Random()));
        }

        private static bool IsSafe(BooruApiResult result, bool searchingForExplicit)
        {
            var forbiddenTags = ForbiddenTags.Get();

            if (result.Tags.Any(t => forbiddenTags.DiscordTosProhibited.Contains(t)))
                return false;

            bool hasExplicitTags = result.Tags.Any(t => forbiddenTags.ForceExplicit.Contains(t));
            bool isExplicit = result.Quality == BooruApiQuality.Explicit || hasExplicitTags;

            if (isExplicit && result.Tags.Any(t => forbiddenTags.ConditionalProhibited.Contains(t)))
                return false;

            if (!searchingForExplicit && isExplicit)
                return false;

            //if (result.Quality != q)
            //    return false;

            return true;
        }

        private static DiscordEmbedBuilder getEmbed(BooruApiResult result)
        {
            return new DiscordEmbedBuilder()
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    Name = result.UserName,
                    Url = result.UserUrl
                },
                Color = new Uri(result.Page).Host switch
                {
                    "danbooru.donmai.us" => new DiscordColor("0073ff"),
                    "yande.re" => new DiscordColor("ee8887"),
                    "gelbooru.com" => new DiscordColor("006FFA"),
                    _ => new Optional<DiscordColor>()
                },
                Title = result.Id,
                Url = result.Page,
                ImageUrl = result.Url,
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = result.Source
                }
            };
        }
    }
}