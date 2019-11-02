using DSharpPlus.Entities;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CraftBot
{
    public static class Extensions
    {
        private static readonly Random _random = new Random();

        public static DiscordActivity GetGameActivity(this DiscordPresence presence) => presence.Activities.FirstOrDefault(a => a.Name != "Custom Status");

        public static DiscordActivity GetCustomStatusActivity(this DiscordPresence presence) => presence.Activities.FirstOrDefault(a => a.Name == "Custom Status");

        /// <summary>
        /// Chooses a random element from <paramref name="ts"/>.
        /// </summary>
        public static T Random<T>(this IEnumerable<T> ts)
        {
            if (ts is null)
                throw new ArgumentNullException(nameof(ts));

            int count = ts.Count();

            int index = _random.Next(0, count - 1);
            return ts.ElementAt(index);
        }

        public static async Task<Image> GetAvatarImageAsync(this DiscordUser user)
        {
            string extension = Path.GetExtension(user.AvatarUrl.Split('?')[0]);
            string cachePath = Path.Combine("cache", "user", user.AvatarHash + extension);

            if (File.Exists(cachePath))
                return Image.FromFile(cachePath);

            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(user.AvatarUrl, cachePath);
                return Image.FromFile(cachePath);
            }
        }

        public static string GetLink(this DiscordMessage message) => $"https://discordapp.com/channels/{message.Channel.Guild.Id}/{message.Channel.Id}/{message.Id}";

        public static string GetString(this ActivityType type) => type switch
        {
            ActivityType.Playing => "Playing",
            ActivityType.Streaming => "Streaming",
            ActivityType.ListeningTo => "Listening to",
            ActivityType.Watching => "Watching",
            _ => null,
        };

        public static string GetString(this UserStatus status) => status switch
        {
            UserStatus.DoNotDisturb => "Do Not Disturb",
            _ => status.ToString(),
        };

        public static string GetString(this TimeSpan timeSpan, bool includeTime = true)
        {
            var strings = new List<string>();

            int years = timeSpan.Days / 365;
            int remainingDays = timeSpan.Days % 365;

            int weeks = remainingDays / 7;
            remainingDays %= 7;

            if (years > 0)
                strings.Add($"{years} years");

            if (weeks > 0)
                strings.Add($"{weeks} weeks");

            if (remainingDays > 0)
                strings.Add($"{remainingDays} days");

            if (includeTime)
            {
                if (timeSpan.Hours > 0)
                    strings.Add($"{timeSpan.Hours} hours");

                if (timeSpan.Minutes > 0)
                    strings.Add($"{timeSpan.Minutes} minutes");

                strings.Add($"{timeSpan.Seconds} seconds");
            }

            return string.Join(", ", strings);
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(this Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}