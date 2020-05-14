using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

using CraftBot.Extensions;
using CraftBot.Repositories;

using DSharpPlus;
using DSharpPlus.Entities;

using LiteDB;

namespace CraftBot.Database
{
    public class GuildData
    {
        public GuildData()
        {
        }

        public GuildData(ulong id) => this.Id = id;

        public GuildData(SnowflakeObject guild) : this(guild.Id)
        {
        }

        public string ColorHex { get; set; }
        public bool Public { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public List<string> Features { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();

        [BsonIgnore]
        public ulong Id { get; set; }

        public async Task<DiscordColor> GetColorAsync(DiscordClient client, GuildRepository guildRepository)
        {
            if (ColorHex != null)
                return new DiscordColor(ColorHex);

            var guild = await client.GetGuildAsync(Id);

            if (guild == null)
                return DiscordColor.Gray;

            using var icon = await guild.GetIconAsync();

            if (icon == null)
                return DiscordColor.Gray;

            using var resizedAvatar = icon.ResizeImage(1, 1);

            var pixel = resizedAvatar.GetPixel(0, 0);
            ColorHex = ColorTranslator.ToHtml(pixel);

            guildRepository.Save(this);

            return new DiscordColor(ColorHex);
        }
    }
}