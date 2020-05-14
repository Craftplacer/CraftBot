using CraftBot.Database;
using DSharpPlus.Entities;
using LiteDB;

namespace CraftBot.Repositories
{
    public class GuildRepository : DatabaseRepository
    {
        
        
        public GuildRepository(LiteDatabase database) : base(database)
        {
        }
        
        public void Save(GuildData data)
        {
            var collection = Database.GetCollection<GuildData>();
            var id = data.Id.ToString();
            //var id = this.Id;
            var dataExists = collection.FindById(id) != null;

            if (dataExists)
            {
                if (!collection.Update(id, data))
                {
                    Logger.Warning($"Entry for {id}, was not found.", "Database");
                }
            }
            else
            {
                collection.Insert(id, data);
                Logger.Info($"Inserted new guild data for {id}", "Database");
            }
        }
        
        public GuildData Get(DiscordGuild guild) => Get(guild.Id);

        public GuildData Get(ulong guildId)
        {
            var collection = Database.GetCollection<GuildData>();
            GuildData data;
            
            lock (collection)
            {
                data = collection.FindById(guildId.ToString());
            }
            
            //var data = collection.FindById(guildId);

            if (data == null)
            {
                return new GuildData(guildId);
            }
            
            data.Id = guildId;
            return data;
        }
    }
}