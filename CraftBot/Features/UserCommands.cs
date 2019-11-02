using CraftBot.Database;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CraftBot.Features
{
    [Group("user")]
    public class UserCommands : BaseCommandModule
    {
        [GroupCommand]
        [Command("info")]
        public async Task Info(CommandContext context, DiscordUser user = null)
        {
            if (user == null)
                user = context.User;

            var data = UserData.Get(user);
            var member = await context.Guild.GetMemberAsync(user.Id);

            string getDescription()
            {
                string description = string.Empty;

                #region Activity

                if (user.Presence != null)
                {
                    string getEmoji(DiscordPresence presence)
                    {
                        bool isMixed(DiscordClientStatus status)
                        {
                            UserStatus? previousStatus = null;

                            if (status.Desktop.HasValue)
                                if (previousStatus.HasValue)
                                    return previousStatus != status.Desktop.Value;
                                else
                                    previousStatus = status.Desktop.Value;

                            if (status.Mobile.HasValue)
                                if (previousStatus.HasValue)
                                    return previousStatus != status.Mobile.Value;
                                else
                                    previousStatus = status.Mobile.Value;

                            if (status.Web.HasValue)
                                if (previousStatus.HasValue)
                                    return previousStatus != status.Web.Value;
                                else
                                    previousStatus = status.Web.Value;

                            return false;
                        }

                        string emojis = Emoji.GetEmoji(user.Presence.Status);

                        if (isMixed(presence.ClientStatus))
                        {
                            if (presence.ClientStatus.Desktop.HasValue)
                                emojis += Emoji.GetEmoji(presence.ClientStatus.Desktop.Value, Emoji.ClientType.Desktop);

                            if (presence.ClientStatus.Mobile.HasValue)
                                emojis += Emoji.GetEmoji(presence.ClientStatus.Mobile.Value, Emoji.ClientType.Mobile);

                            if (presence.ClientStatus.Web.HasValue)
                                emojis += Emoji.GetEmoji(presence.ClientStatus.Web.Value, Emoji.ClientType.Web);
                        }

                        return emojis;
                    }

                    description += getEmoji(user.Presence);

                    string text = $" {user.Presence.Status.GetString()}";

                    if (user.Presence.Activity != null && !string.IsNullOrWhiteSpace(user.Presence.Activity.Name))
                    {
                        bool isCustomStatus = user.Presence.Activity.ActivityType == (ActivityType)4 && user.Presence.Activity.Name == "Custom Status" && !string.IsNullOrWhiteSpace(user.Presence.Activity.RichPresence.State);

                        if (isCustomStatus)
                        {
                            text = user.Presence.Activity.RichPresence.State;
                        }
                        else
                        {
                            text = $" {user.Presence.Activity.ActivityType.GetString()} **{user.Presence.Activity.Name}**";

                            var rp = user.Presence.Activity.RichPresence;
                            if (rp != null)
                            {
                                text += $"\n{Emoji.BLANK} {rp.Details}";
                                text += $"\n{Emoji.BLANK} {rp.State}";
                            }
                        }
                    }

                    description += text + "\n";
                }

                #endregion Activity

                if (user.Verified.HasValue)
                {
                    description += $"\n**'Verified': ** {user.Verified.Value}";
                }
                if (user.PremiumType.HasValue)
                {
                    description += $"\n**Nitro subscription: **";

                    switch (user.PremiumType.Value)
                    {
                        case PremiumType.NitroClassic:
                            description += "Nitro Classic";
                            break;

                        case PremiumType.Nitro:
                            description += "Nitro";
                            break;
                    }
                }

                description += $"\n{Emoji.ICON_CALENDAR_STAR} {user.CreationTimestamp.ToString(Program.CultureInfo.DateTimeFormat)}";
                description += $"\n{Emoji.BLANK} *{(DateTime.Now - user.CreationTimestamp).GetString(includeTime: false)}*";

                if (member != null)
                {
                    description += $"\n{Emoji.ICON_CALENDAR_PLUS} {member.JoinedAt.ToString(Program.CultureInfo.DateTimeFormat)}";
                    description += $"\n{Emoji.BLANK} *{(DateTime.Now - member.JoinedAt).GetString(includeTime: false)}*";
                }

                if (!string.IsNullOrWhiteSpace(data.Biography))
                    description += $"\n\n{data.Biography}";

                return description;
            }

            var embed = new DiscordEmbedBuilder()
            {
                Color = await data.GetColorAsync(),
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    Name = user.Username,
                    IconUrl = user.AvatarUrl
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = user.Id.ToString()
                }
            };

            var largeImageUrl = user.Presence?.Activity?.RichPresence?.LargeImage?.Url;
            if (largeImageUrl != null)
                embed.WithThumbnailUrl(largeImageUrl);

            var description = getDescription();
            if (!string.IsNullOrWhiteSpace(description))
                embed = embed.WithDescription(description);

            if (!string.IsNullOrWhiteSpace(data.Image))
                embed = embed.WithImageUrl(data.Image);

            await context.RespondAsync(embed: embed);
        }

        [Command("biography")]
        [Aliases("bio")]
        public async Task ModifyBiography(CommandContext context, [RemainingText] string biography = null)
        {
            var data = UserData.Get(context.User);

            if (string.IsNullOrWhiteSpace(biography))
            {
                data.Biography = string.Empty;
                data.Save();

                await context.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("Your biography has been removed"));
            }
            else
            {
                data.Biography = biography;
                data.Save();

                await context.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription($"Your biography was set to\n>>> {biography}"));
            }
        }

        [Command("image")]
        [Aliases("img")]
        public async Task ModifyImage(CommandContext context, string imageUrl = null)
        {
            var data = UserData.Get(context.User);

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                data.Image = string.Empty;
                data.Save();

                await context.RespondAsync(embed: new DiscordEmbedBuilder().WithDescription("Your image has been removed"));
            }
            else
            {
                data.Image = imageUrl;
                data.Save();

                await context.RespondAsync(embed: new DiscordEmbedBuilder()
                    .WithDescription($"Your image was set to")
                    .WithImageUrl(imageUrl));
            }
        }

        [Command("export")]
        [Description("This command gives the user the ability to export their data from CraftBot.")]
        [RequireDirectMessage]
        public async Task Export(CommandContext context)
        {
            var data = UserData.Get(context.User);
            var json = JsonConvert.SerializeObject(data);
            var array = Encoding.UTF8.GetBytes(json);

            using (var stream = new MemoryStream(array))
            {
                var filename = $"Data of {context.User.Username} ({context.User.Id}).json";
                await context.RespondWithFileAsync(filename, stream);
            }
        }

        [Group("features")]
        private class Features : BaseCommandModule
        {
            [Command("enable")]
            public async Task Enable(CommandContext context, string name)
            {
                name = name.ToLowerInvariant();

                var data = UserData.Get(context.User);
                if (data.Features.Contains(name))
                {
                    await context.RespondAsync($"You already have feature `{name}` enabled.");
                    return;
                }

                data.Features.Add(name);
                data.Save();

                await context.RespondAsync($"Feature `{name}` has been enabled.");
            }

            [Command("disable")]
            public async Task Disable(CommandContext context, string name)
            {
                name = name.ToLowerInvariant();

                var data = UserData.Get(context.User);
                if (!data.Features.Contains(name))
                {
                    await context.RespondAsync($"You already have feature `{name}` disabled.");
                    return;
                }

                data.Features.Remove(name);
                data.Save();

                await context.RespondAsync($"Feature `{name}` has been disabled.");
            }
        }
    }
}