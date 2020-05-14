using System;
using System.Drawing;
using System.IO;
using System.Linq;
using CraftBot.Extensions;

namespace CraftBot.Commands
{
    public partial class ShitpostingCommands
    {
        private static class TweetRenderer
        {
            private static readonly Color backgroundColor = Color.White;

            private static readonly Color outlineColor = Color.FromArgb(230, 236, 240);
            private static readonly Pen outlinePen = new Pen(outlineColor);

            private static readonly Color darkGray = Color.FromArgb(101, 119, 134);
            private static readonly Brush darkGrayBrush = new SolidBrush(darkGray);
            private static readonly Pen darkGrayPen = new Pen(darkGray);

            private static readonly Color black = Color.FromArgb(20, 23, 26);
            private static readonly Brush blackBrush = new SolidBrush(black);

            private static readonly FontFamily fontFamily = FontFamily.Families.First(f => f.Name == "Segoe UI");
            private static readonly Font authorFont = new Font(fontFamily, 15, FontStyle.Bold, GraphicsUnit.Pixel);
            private static readonly Font usernameFont = new Font(fontFamily, 15, FontStyle.Regular, GraphicsUnit.Pixel);

            public static Image RenderTweet(Tweet tweet)
            {
                const int width = 600;
                const int authorBarHeight = 70;

                var messageFont = new Font(fontFamily, 23, FontStyle.Regular, GraphicsUnit.Pixel);
                var messageWidth = width - (16 * 2);

                var contentHeight = GetStringHeight(tweet.Content, messageFont, messageWidth);
                using var quoteBitmap = tweet.ChildTweet == null ? null : RenderQuote(tweet.ChildTweet);

                int GetQuoteHeight()
                {
                    if (quoteBitmap == null)
                        return 0;

                    return quoteBitmap.Height + 10 + 20;
                }

                var height = 160 + authorBarHeight + GetQuoteHeight() + (int)contentHeight;
                var outputBitmap = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(outputBitmap))
                {
                    graphics.SetHighQuality();
                    graphics.Clear(backgroundColor);
                    graphics.DrawRectangle(outlinePen, 0, 0, outputBitmap.Width - 1, outputBitmap.Height - 1);

                    float y = 11;

                    // tweeter
                    using (var clippedAvatar = ((Bitmap)tweet.AuthorImage).ClipToCircle())
                    {
                        const int avatarSize = 49;
                        var x = 16;
                        graphics.DrawImage(clippedAvatar, x, y, avatarSize, avatarSize);

                        x += avatarSize;
                        x += 10;

                        graphics.DrawString(tweet.Nickname, authorFont, Brushes.Black, x, y += 4);
                        graphics.DrawString("@" + tweet.Username, usernameFont, darkGrayBrush, x, y += 20);

                        y = authorBarHeight; // Until avatar
                        y += 10;             // Bottom Padding
                    }

                    // content
                    {
                        var messageRectangle = new RectangleF(16, y, messageWidth, contentHeight);
                        graphics.DrawString(tweet.Content, messageFont, blackBrush, messageRectangle);

                        y += contentHeight;
                        y += 15; // bottom padding
                    }

                    if (quoteBitmap != null)
                    {
                        y += 10;

                        graphics.DrawImage(quoteBitmap, 16, y, quoteBitmap.Width, quoteBitmap.Height);

                        y += quoteBitmap.Height;
                        y += 20;
                    }

                    // details
                    {
                        var details = $"{tweet.Date.ToShortTimeString()} · {tweet.Date.ToShortDateString()}";
                        var rectangle = new RectangleF(16, y, 145, outputBitmap.Height);
                        graphics.DrawString(details, usernameFont, darkGrayBrush, rectangle);

                        y += 20; //font height
                        y += 15; // bottom margin
                    }

                    //info bar
                    {
                        graphics.DrawLine(outlinePen, 16, y, width - 32, y);

                        y += 15;

                        graphics.DrawString($"{tweet.Retweets} Retweets {tweet.Likes} Likes", usernameFont, darkGrayBrush, 16, y);

                        y += 20;
                        y += 15;
                    }

                    // additional buttons
                    {
                        graphics.DrawLine(outlinePen, 16, y, width - 32, y);

                        y += 13;

                        using (var likeImage = Image.FromFile(Path.Combine("assets", "twitter", "light-like.png")))
                        using (var replyImage = Image.FromFile(Path.Combine("assets", "twitter", "light-reply.png")))
                        using (var shareImage = Image.FromFile(Path.Combine("assets", "twitter", "light-share.png")))
                        using (var retweetImage = Image.FromFile(Path.Combine("assets", "twitter", "light-retweet.png")))
                        {
                            graphics.DrawImage(replyImage, 75, y);
                            graphics.DrawImage(retweetImage, 217, y);
                            graphics.DrawImage(likeImage, 359, y);
                            graphics.DrawImage(shareImage, 501, y);
                        }

                        y += 2;
                        y += 20;
                        y += 15;
                    }
                }
                return outputBitmap;
            }

            private static Image RenderQuote(Tweet tweet)
            {
                const int width = 566;
                const int margin = 10;
                var height = (int)GetStringHeight(tweet.Content, usernameFont, 545);

                var bitmap = new Bitmap(width, height + (margin * 2) + 20 + 5);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SetHighQuality();
                    graphics.Clear(backgroundColor);
                    graphics.DrawRoundedRectangle(darkGrayPen, new Rectangle(1, 1, bitmap.Width - 2, bitmap.Height - 2), 14);

                    var x = margin;
                    var y = margin;

                    using (var clippedAvatar = ((Bitmap)tweet.AuthorImage).ClipToCircle())
                    {
                        graphics.DrawImage(clippedAvatar, x, y, 20, 20);
                        x += 20 + 5;

                        graphics.DrawString(tweet.Nickname, authorFont, Brushes.Black, x, y);
                        x += (int)graphics.MeasureString(tweet.Nickname, authorFont).Width;
                        x += 5;

                        graphics.DrawString($"@{tweet.Username} · {tweet.Date.ToShortDateString()}", usernameFont, darkGrayBrush, x, y);
                        y += 20;
                    }

                    x = margin;
                    {
                        y += 5;
                        graphics.DrawString(tweet.Content, usernameFont, Brushes.Black, x, y);
                    }
                }

                return bitmap;
            }

            private static float GetStringHeight(string text, Font font, int width)
            {
                using (var tempBitmap = new Bitmap(1, 1))
                using (var graphics = Graphics.FromImage(tempBitmap))
                {
                    return graphics.MeasureString(text, font, width).Height;
                }
            }

            public class Tweet : IDisposable
            {
                public Image AuthorImage { get; set; }
                public string Content { get; set; }
                public string Username { get; set; }

                public DateTime Date { get; set; }

                public Tweet ChildTweet { get; set; }

                public int Retweets { get; set; }
                public int Likes { get; set; }
                public string Nickname { get; internal set; }

                public void Dispose() => AuthorImage.Dispose();
            }
        }
    }
}