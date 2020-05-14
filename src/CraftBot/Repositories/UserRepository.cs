using CraftBot.Database;
using DSharpPlus.Entities;
using LiteDB;

namespace CraftBot.Repositories
{
    public class UserRepository : DatabaseRepository
    {
        public UserRepository(LiteDatabase database) : base(database)
        {
        }
        
        public UserData Get(DiscordUser user)
        {
            var collection = Database.GetCollection<UserData>();
            var data = collection.FindById(user.Id.ToString());

            if (data == null)
                return new UserData(user);

            data.Id = user.Id;
            return data;
        }
        
        public void Save(UserData data)
        {
            var collection = Database.GetCollection<UserData>();
            var id = data.Id.ToString();
            var dataExists = collection.FindById(id) != null;

            if (dataExists)
            {
                // TODO: Consider saving failed transactions to a cache/temp directory,
                //       serialized using JSON, for data restoration.
                if (!collection.Update(id, data))
                    Logger.Warning($"Entry for {id}, was not found.", "Database");
            }
            else
            {
                collection.Insert(id, data);
                Logger.Info($"Inserted new user data for {id}", "Database");
            }
        }
    }
}