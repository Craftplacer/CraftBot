using CraftBot.Database;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftBot.Features.Special
{
    [Group("elb")]
    public class EmojiLeaderboardCommands : BaseCommandModule
    {
        private static string[] prefixes = new[] { ":one:", ":two:", ":three:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:", ":keycap_ten:" };

        [GroupCommand]
        [Command("view")]
        public async Task View(CommandContext context)
        {
            if (context.Guild.Id != EmojiLeaderboard.MessengerGeekGuildId)
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder()
                {
                    Title = "Server specific feature",
                    Description = "This command is not available to your server. We are sorry for the inconvinience.",
                    Color = Colors.Red500
                });
                return;
            }

            var description = new StringBuilder();
            var top10 = EmojiLeaderboard.Leaderboard.OrderByDescending(a => a.Value).Take(10).ToList();

            for (int i = 0; i < top10.Count(); i++)
            {
                var item = top10[i];
                var username = (await context.Client.GetUserAsync(item.Key)).Username;

                description.AppendLine($"{prefixes[i]} {username} `{item.Value}`");
            }

            var leftOver = EmojiLeaderboard.Leaderboard.Count() - top10.Count();
            if (leftOver != 0)
                description.AppendLine($"\n*and {leftOver} more...*");

            await context.RespondAsync(embed: new DiscordEmbedBuilder()
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    Name = "Emoji Leaderboard",
                    IconUrl = "https://github.com/twitter/twemoji/raw/gh-pages/72x72/1f61b.png"
                },
                Description = description.ToString()
            });
        }

        [RequireOwner]
        [Command("import")]
        public async Task Import(CommandContext context)
        {
            string json = File.ReadAllText("elb.json");
            EmojiLeaderboard.Leaderboard = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json);

            await context.RespondAsync("imported.");
        }
    }

    public static class EmojiLeaderboard
    {
        public const ulong MessengerGeekGuildId = 573623720766078996;
        public static Dictionary<ulong, int> Leaderboard;

        static EmojiLeaderboard()
        {
            var guildData = GuildData.Get(MessengerGeekGuildId);

            if (guildData == null)
                return;

            if (!guildData.CustomData.ContainsKey("elb"))
                return;

            var l = (Dictionary<string, object>)guildData.CustomData["elb"];
            Leaderboard = new Dictionary<ulong, int>();

            foreach (var i in l)
                Leaderboard[ulong.Parse(i.Key)] = (int)i.Value;
        }

        public static void Add(DiscordClient client)
        {
            client.MessageCreated += Client_MessageCreated;
        }

        private static async Task Client_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Author.IsCurrent)
                return;

            string content = e.Message.Content;

            bool containsDiscordEmoji = content.Contains("😛");
            bool containsMsnEmoji = content.Contains("<:msn_tongue:484159452807823370>");
            bool containsEmoji = containsDiscordEmoji || containsMsnEmoji;

            if (!containsEmoji)
                return;

            int newRank;

            lock (Leaderboard)
            {
                if (!Leaderboard.ContainsKey(e.Author.Id))
                    Leaderboard.Add(e.Author.Id, 0);

                Leaderboard[e.Author.Id]++;
                newRank = Leaderboard[e.Author.Id];

                var guildData = GuildData.Get(MessengerGeekGuildId);
                guildData.CustomData["elb"] = Leaderboard;
                guildData.Save();
            }

            string nulls = new string('0', newRank.ToString().Length - 1);

            if (!string.IsNullOrWhiteSpace(nulls) && newRank.ToString().EndsWith(nulls))
            {
                string message = string.Format("**{0}** hit **{1}** :stuck_out_tongue:!", e.Author.Mention, newRank);
                await e.Message.RespondAsync(message);
            }
        }
    }
}