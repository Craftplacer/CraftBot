using DSharpPlus.Entities;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace CraftBot.Database
{
    public class UserData
    {
        public UserData()
        {
        }

        public UserData(DiscordUser user) => this.Id = user.Id;

        [BsonIgnore]
        public ulong Id { get; set; }

        [BsonId]
        public string DatabaseId
        {
            get => Id.ToString();
            set => Id = ulong.Parse(value);
        }

        public string Image { get; set; }

        public string Biography { get; set; }

        public List<string> Features { get; set; } = new List<string>();

        public string ColorHex { get; set; }

        public async Task<DiscordColor> GetColorAsync()
        {
            if (ColorHex == null)
            {
                var user = await Program.Client.GetUserAsync(this.Id);

                if (user == null)
                    return DiscordColor.Gray;

                using (var avatar = await user.GetAvatarImageAsync())
                using (var resizedAvatar = avatar.ResizeImage(1, 1))
                {
                    var pixel = resizedAvatar.GetPixel(0, 0);
                    ColorHex = ColorTranslator.ToHtml(pixel);

                    this.Save();
                }
            }

            return new DiscordColor(ColorHex);
        }

        public static UserData Get(DiscordUser user)
        {
            var collection = Program.Database.GetCollection<UserData>();
            var id = user.Id.ToString();
            var data = collection.FindById(id);

            if (data == null)
                collection.Insert(id, data = new UserData(user));

            return data;
        }

        public void Save()
        {
            var collection = Program.Database.GetCollection<UserData>();
            collection.Update(DatabaseId, this);
        }
    }
}