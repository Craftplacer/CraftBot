using DSharpPlus.Entities;
using LiteDB;
using System;
using System.Collections.Generic;

namespace CraftBot.Database
{
    public class GuildData
    {
        public GuildData()
        {
        }

        public GuildData(DiscordGuild guild) => this.Id = guild.Id;

        [BsonIgnore]
        public ulong Id { get; set; }

        [BsonId]
        public string DatabaseId
        {
            get => Id.ToString();
            set => Id = ulong.Parse(value);
        }

        public List<string> Features { get; set; } = new List<string>();

        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();

        public static GuildData Get(DiscordGuild guild)
        {
            var collection = Program.Database.GetCollection<GuildData>();
            var id = guild.Id.ToString();
            var data = collection.FindById(id);

            if (data == null)
                collection.Insert(id, data = new GuildData(guild));

            return data;
        }

        public static GuildData Get(ulong guildId)
        {
            var collection = Program.Database.GetCollection<GuildData>();
            var id = guildId.ToString();
            var data = collection.FindById(id);

            if (data == null)
                collection.Insert(id, data = new GuildData() { Id = guildId });

            return data;
        }

        public void Save()
        {
            var collection = Program.Database.GetCollection<GuildData>();
            collection.Update(DatabaseId, this);
        }
    }
}