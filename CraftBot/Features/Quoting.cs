using CraftBot.Database;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Enums;
using DSharpPlus.EventArgs;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CraftBot.Features
{
    public static class Quoting
    {
        public static readonly string Pattern = "(>>[0-9]{18})";

        public static void Add(DiscordClient client) => client.MessageCreated += Client_MessageCreated;

        private static async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            var data = UserData.Get(e.Author);

            if (!data.Features.Contains("quoting"))
                return;

            var matches = Regex.Matches(e.Message.Content, Pattern);

            foreach (Match match in matches)
            {
                string @string = match.Value;
                var id = ulong.Parse(@string.Substring(2, @string.Length - 2));
                var message = await e.Channel.GetMessageAsync(id);

                if (message == null)
                    continue;

                if (!message.MessageType.HasValue || message.MessageType != MessageType.Default)
                    continue;

                await e.Message.RespondAsync(embed: await message.GetEmbedAsync());

                return;
            }
        }

        public static async Task<DiscordEmbedBuilder> GetEmbedAsync(this DiscordMessage message)
        {
            var userData = UserData.Get(message.Author);
            return new DiscordEmbedBuilder()
            {
                Color = await userData.GetColorAsync(),
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    IconUrl = message.Author.AvatarUrl,
                    Name = message.Author.Username
                },
                Description = $"{message.Content} [{Emoji.ICON_MESSAGE_UP}]({message.GetLink()} \"Jump to {message.Author.Username}'s message in #{message.Channel.Name}\")",
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    IconUrl = message.Channel.Guild.IconUrl,
                    Text = $"{message.Channel.Guild.Name} • #{message.Channel.Name}"
                },
                Timestamp = message.Timestamp,
            };
        }
    }
}