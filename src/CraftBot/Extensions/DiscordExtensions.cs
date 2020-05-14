using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using CraftBot.Localization;

using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using JetBrains.Annotations;

namespace CraftBot.Extensions
{
    public static class DiscordExtensions
    {
        /// <summary>
        /// Sends a webhook message impersonating an already existing <see cref="DiscordMessage"/>.
        /// </summary>
        /// <remarks>
        /// Caches all attachments from the specified <paramref name="message"/>.
        /// </remarks>
        public static async Task ExecuteAsync(this DiscordWebhook webhook, DiscordMessage message)
        {
            var files = new Dictionary<string, Stream>();

            using (var webClient = new WebClient())
            {
                foreach (var attachment in message.Attachments)
                {
                    var filePath = Path.Combine("cache", "attachments", attachment.Id.ToString());

                    if (!File.Exists(filePath))
                        await webClient.DownloadFileTaskAsync(attachment.Url, filePath);

                    files[attachment.FileName] = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
            }

            var builder = new DiscordWebhookBuilder
            {
                Content = message.Content,
                Username = message.Author.Username,
                AvatarUrl = message.Author.AvatarUrl
            }
            .AddEmbeds(message.Embeds)
            .AddFiles(files);

            await webhook.ExecuteAsync(builder);

            // clean up
            foreach (var file in files)
            {
                if (file.Value is MemoryStream stream)
                    await stream.DisposeAsync();
            }
        }

        public static async Task<Image> GetAvatarAsync(this DiscordUser user)
        {
            var extension = Path.GetExtension(user.AvatarUrl.Split('?')[0]);
            var cachePath = Path.Combine("cache", "user", user.AvatarHash + extension);

            if (File.Exists(cachePath))
                return Image.FromFile(cachePath);

            using var client = new WebClient();

            await client.DownloadFileTaskAsync(user.AvatarUrl, cachePath);
            return Image.FromFile(cachePath);
        }

        public static async Task<Image> GetIconAsync(this DiscordGuild guild)
        {
            var iconUrl = guild.IconUrl;

            if (iconUrl == null)
                return null;

            var extension = Path.GetExtension(iconUrl.Split('?')[0]);
            var cachePath = Path.Combine("cache", "guild", guild.IconHash + extension);

            if (File.Exists(cachePath))
                return Image.FromFile(cachePath);

            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(guild.IconUrl, cachePath);
                return Image.FromFile(cachePath);
            }
        }

        public static DiscordActivity GetCustomStatusActivity(this DiscordPresence presence) => presence.Activities.FirstOrDefault(a => a.Name == "Custom Status");

        public static DiscordActivity GetGameActivity(this DiscordPresence presence) => presence.Activities.FirstOrDefault(a => a.Name != "Custom Status");

        public static string GetString(this ActivityType type) => type switch
        {
            ActivityType.Playing => "Playing",
            ActivityType.Streaming => "Streaming",
            ActivityType.ListeningTo => "Listening to",
            ActivityType.Watching => "Watching",
            _ => null,
        };

        public static string GetString(this UserStatus status, Language language)
        {
            var key = status switch
            {
                UserStatus.Offline => "status.offline",
                UserStatus.Invisible => "status.invisible",
                UserStatus.Online => "status.online",
                UserStatus.Idle => "status.idle",
                UserStatus.DoNotDisturb => "status.donotdisturb",
                _ => null
            };

            return language[key];
        }

        public static Task RespondWithImageAsync(this CommandContext context, Image image, string filename) => RespondWithImageAsync(context.Message, image, filename);

        public static async Task RespondWithImageAsync(this DiscordMessage message, Image image, string filename)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Png);

                stream.Position = 0;

                await message.RespondWithFileAsync(filename + ".png", stream);
            }
        }

        /// <summary>
        /// Returns a <see cref="string"/> that others on Discord would see.
        /// </summary>
        public static string ToFriendlyString(this DiscordUser user) => $"{user.Username}#{user.Discriminator}";

        public static DiscordEmbedBuilder.EmbedAuthor GetEmbedAuthor(this DiscordUser user, params string[] paths)
        {
            const string separator = " / ";

            return new DiscordEmbedBuilder.EmbedAuthor
            {
                Name = string.Join(separator, new[] { user.Username }.Concat(paths)),
                IconUrl = user.AvatarUrl
            };
        }

        /// <summary>
        /// Tries to get a member for the guild specified, if no guild was provided or it failed, it returns null.
        /// </summary>
        [CanBeNull]
        public static async Task<DiscordMember> TryGetMemberAsync(this DiscordGuild guild, ulong id)
        {
            if (guild is null)
                return null;

            try
            {
                return await guild.GetMemberAsync(id);
            }
            catch (NotFoundException)
            {
            }

            return null;
        }

        public static string SanitizeMentions(this string input)
        {
            return input
                .Replace("@everyone", $"@{Constants.ZeroWidthSpace}everyone")
                .Replace("@here", $"@{Constants.ZeroWidthSpace}here");
        }
    }
}