using CraftBot.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CraftBot.Commands
{
    [Group("shitposting")]
    [Description("Group of commands for making shitposts.")]
    public partial class ShitpostingCommands : BaseCommandModule
    {
        [Command("ehre")]
        public async Task EhreEntnehmen(CommandContext context, DiscordUser opfer)
        {
            using (var image = Image.FromFile(@"assets\ehre.jpg"))
            using (var overlay = Image.FromFile(@"assets\ehre-overlay.png"))
            {
                using (var ehrenAvatar = await context.User.GetAvatarAsync())
                using (var opferAvatar = await opfer.GetAvatarAsync())
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

        [Command("tweet")]
        [Aliases("twitter")]
        public async Task GenerateTweet(CommandContext context, DiscordMessage message)
        {
            var member = await message.Channel.Guild.GetMemberAsync(message.Author.Id);

            var random = new Random((int)message.Id);

            using (var avatar = await message.Author.GetAvatarAsync())
            using (var tweetBitmap = TweetRenderer.RenderTweet(new TweetRenderer.Tweet
            {
                AuthorImage = avatar,
                Content = message.Content,
                Nickname = member.Nickname ?? message.Author.Username,
                Username = message.Author.Username,
                Retweets = random.Next(999),
                Likes = random.Next(999),
                Date = message.CreationTimestamp.DateTime,
            }))
            {
                await context.RespondWithImageAsync(tweetBitmap, "tweet");
            }
        }

        [Command("trump")]
        public async Task GenerateTrumpTweet(CommandContext context, DiscordMessage message)
        {
            var member = await message.Channel.Guild.GetMemberAsync(message.Author.Id);

            var random = new Random((int)message.Id);

            using (var avatar = await message.Author.GetAvatarAsync())
            using (var trump = Image.FromFile(Path.Combine("assets", "twitter", "trump.jpg")))
            using (var tweetBitmap = TweetRenderer.RenderTweet(new TweetRenderer.Tweet
            {
                AuthorImage = trump,
                Content = $"Thank you {message.Author.Username}, very cool!",
                Nickname = "Donald J. Trump",
                Username = "realDonaldTrump",
                Retweets = random.Next(999),
                Likes = random.Next(999),
                Date = DateTime.Now,
                ChildTweet = new TweetRenderer.Tweet
                {
                    AuthorImage = avatar,
                    Content = message.Content,
                    Nickname = member.Nickname ?? message.Author.Username,
                    Username = message.Author.Username,
                    Retweets = random.Next(999),
                    Likes = random.Next(999),
                    Date = message.CreationTimestamp.DateTime,
                }
            }))
            {
                await context.RespondWithImageAsync(tweetBitmap, "tweet");
            }
        }

        [Command("tubeme")]
        public async Task GenerateVideo(CommandContext context, string imageUrl, [RemainingText] string title)
        {
            var darkGray = Color.FromArgb(54, 54, 54);
            var darkGrayPen = new Pen(darkGray);

            var gray = Color.FromArgb(170, 170, 170);
            var grayBrush = new SolidBrush(gray);

            Image userImage;

            using (var client = new WebClient())
            using (var memoryStream = new MemoryStream(await client.DownloadDataTaskAsync(imageUrl)))
            {
                userImage = Image.FromStream(memoryStream);
            }

            using (var roboto = FontFamily.Families.First(f => f.Name == "Roboto"))
            using (var robotoMedium = FontFamily.Families.First(f => f.Name == "Roboto Medium"))
            using (var channelFont = new Font(robotoMedium, 14, GraphicsUnit.Pixel))
            using (var titleFont = new Font(roboto, 18, GraphicsUnit.Pixel))
            using (var subtitleFont = new Font(roboto, 13, GraphicsUnit.Pixel))
            using (var subscriberFont = new Font(roboto, 13, GraphicsUnit.Pixel))
            using (var labelFont = new Font(robotoMedium, 13, GraphicsUnit.Pixel))
            using (var buttonBitmap = Image.FromFile(Path.Combine("assets", "youtube", "buttons.png")))
            using (var userAvatar = (Bitmap)await context.User.GetAvatarAsync())
            using (var clippedUserAvatar = userAvatar.ClipToCircle())
            using (var bitmap = new Bitmap(997, 739))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(31, 31, 31));
                graphics.SetHighQuality();
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                graphics.FillRectangle(Brushes.Black, 24, 24, 949, 534);
                graphics.DrawImage(userImage, 24, 24, 949, 534);

                graphics.DrawString(title, titleFont, Brushes.White, 24, 583);
                graphics.DrawString($"69,696,969 views • {DateTime.Now.ToString("d MMM yyyy")}", subtitleFont, grayBrush, 24, 616);

                graphics.DrawImageUnscaled(buttonBitmap, 615, 615);

                var likes = 69;
                var dislikes = 69;

                graphics.DrawString(likes.ToString(), labelFont, grayBrush, 651, 616);
                graphics.DrawString(dislikes.ToString(), labelFont, grayBrush, 725, 616);

                graphics.FillRectangle(new SolidBrush(Color.FromArgb(96, 96, 96)), 615, 648, 133, 2);

                var likeBarWidth = (int)((float)likes / (likes + dislikes) * 133);
                graphics.FillRectangle(grayBrush, 615, 648, likeBarWidth, 2);

                graphics.FillRoundedRectangle(new SolidBrush(Color.FromArgb(204, 0, 0)), new Rectangle(865, 675, 105, 37), 2);
                graphics.DrawString("Subscribe".ToUpper(), channelFont, Brushes.White, 879, 686);

                graphics.DrawLine(darkGrayPen, 24, 650, 972, 650);

                graphics.DrawImage(clippedUserAvatar, 23, 667, 48, 48);
                graphics.DrawString(context.User.Username, channelFont, Brushes.White, 88, 677);
                graphics.DrawString($"{int.Parse(context.User.Discriminator)} subscribers",
                                    subscriberFont,
                                    grayBrush,
                                    88,
                                    695);

                await context.RespondWithImageAsync(bitmap, "video");
            }
        }
    }
}