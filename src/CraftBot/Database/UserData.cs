using CraftBot.Localization;
using CraftBot.Model;
using DSharpPlus.Entities;
using LiteDB;
using System;
using System.Drawing;
using System.Threading.Tasks;
using CraftBot.Repositories;
using DSharpPlus;
using Newtonsoft.Json;
using CraftBot.Extensions;

namespace CraftBot.Database
{
    public class UserData
    {
        public UserData()
        {
        }

        public UserData(DiscordUser user) => Id = (User = user).Id;

        [BsonField("lang")]
        public string Language { get; set; }

        [BsonField("bio")]
        public string Biography { get; set; }

        public DateTime? Birthday { get; set; }
        public string ColorHex { get; set; }
        public UserFeatures Features { get; set; } = UserFeatures.None;
        public Gender? Gender { get; set; }
        public ulong Id { get; set; }
        public string Image { get; set; }
        public OneTimeNotices OneTimeNotices { get; set; } = OneTimeNotices.None;

        public short LastBirthdayCelebrated { get; set; } = short.MinValue;

        public bool NotifyOnQuote { get; set; }

        [BsonIgnore, JsonIgnore] public DiscordUser User { get; }

        [BsonIgnore, JsonIgnore] public bool IsLanguageSet => !string.IsNullOrWhiteSpace(Language);

        public async Task<DiscordColor> GetColorAsync(DiscordClient client, UserRepository userRepository)
        {
            if (ColorHex != null)
                return new DiscordColor(ColorHex);

            if (client == null)
                throw new ArgumentNullException(nameof(client));

            var user = await client.GetUserAsync(Id);

            if (user == null || user.Discriminator == null || user.AvatarUrl == null)
                return DiscordColor.Gray;

            using var avatar = await user.GetAvatarAsync();
            using var resizedAvatar = avatar.ResizeImage(1, 1);

            var pixel = resizedAvatar.GetPixel(0, 0);
            ColorHex = ColorTranslator.ToHtml(pixel);

            userRepository.Save(this);

            return new DiscordColor(ColorHex);
        }

        [BsonIgnore]
        public Language GetLanguage(LocalizationEngine engine) =>
            IsLanguageSet ? engine.GetLanguage(Language) : engine.FallbackLanguage;
    }
}