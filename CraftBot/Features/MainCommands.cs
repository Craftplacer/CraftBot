using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CraftBot.Features
{
    public class MainCommands : BaseCommandModule
    {
        [Command("ehre")]
        public async Task EhreEntnehmen(CommandContext context, DiscordUser opfer)
        {
            using (var webClient = new WebClient())
            using (var image = Image.FromFile(@"assets\ehre.jpg"))
            using (var overlay = Image.FromFile(@"assets\ehre-overlay.png"))
            {
                Image downloadImage(string url)
                {
                    var avatarBytes = webClient.DownloadData(url);

                    using (var avatarStream = new MemoryStream(avatarBytes))
                    {
                        return Image.FromStream(avatarStream);
                    }
                }

                using (var ehrenAvatar = downloadImage(context.User.AvatarUrl))
                using (var opferAvatar = downloadImage(opfer.AvatarUrl))
                using (var graphics = Graphics.FromImage(image))
                {
                    graphics.DrawImage(opferAvatar, new Rectangle(80, 69, 72, 72));
                    graphics.DrawImage(ehrenAvatar, new Rectangle(278, 114, 50, 50));
                    graphics.DrawImage(overlay, 0, 0);
                }

                using (var stream = new MemoryStream())
                {
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);

                    stream.Position = 0;

                    await context.RespondWithFileAsync("ehre.jpg", stream, $"**{opfer.Username}** wurde die Ehre entnommen.");
                }
            }
        }
    }
}