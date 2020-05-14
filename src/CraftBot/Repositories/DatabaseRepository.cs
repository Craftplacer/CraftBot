using LiteDB;

namespace CraftBot.Repositories
{
    public abstract class DatabaseRepository
    {
        public LiteDatabase Database { get; }
        
        public DatabaseRepository(LiteDatabase database)
        {
            Database = database;
        }
    }
}