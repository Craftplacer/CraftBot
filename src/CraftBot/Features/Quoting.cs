using CraftBot.Database;
using CraftBot.Localization;
using CraftBot.Model;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CraftBot.Repositories;

namespace CraftBot.Features
{
    public class Quoting : BaseExtension
    {
        private readonly Regex _idPattern = new Regex("(>>[0-9]{18})", RegexOptions.Compiled);
        private readonly Regex _linkPattern = new Regex(@"http.:\/\/.*discordapp\.com\/channels\/([0-9]{18})\/([0-9]{18})\/([0-9]{18})", RegexOptions.Compiled);

        public Quoting(UserRepository userRepository, Statistics statistics, LocalizationEngine localization)
        {
            _userRepository = userRepository;
            _statistics = statistics;
            _localization = localization;
        }

        private readonly UserRepository _userRepository;
        private readonly Statistics _statistics;
        private readonly LocalizationEngine _localization;

        protected override void Setup(DiscordClient client)
        {
            Client = client;
            client.MessageCreated += Client_MessageCreated;
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Author.IsBot)
                return;

            var data = _userRepository.Get(e.Author);
            var quotingEnabled = data.Features.HasFlag(UserFeatures.Quoting);

            var idMatches = _idPattern.Matches(e.Message.Content);
            var linkMatches = _linkPattern.Matches(e.Message.Content);
            var language = data.GetLanguage(_localization);

            if (!quotingEnabled)
            {
                if (!idMatches.Any() && !linkMatches.Any())
                    return;

                var member = await e.Guild.GetMemberAsync(e.Author.Id);
                await SendOneTimeNotice(member, data, language);

                return;
            }

            // Check for message quote in 4chan style
            foreach (Match match in idMatches)
            {
                var id = ulong.Parse(match.Value[2..]);
                var message = await e.Channel.GetMessageAsync(id);

                if (message == null)
                {
                    var failEmbed = new DiscordEmbedBuilder
                    {
                        Color = Colors.Red500,
                        Description = language["quoting.fail.message"]
                    };

                    await e.Message.RespondAsync(embed: failEmbed);

                    return;
                }

                if (!message.MessageType.HasValue || message.MessageType != MessageType.Default)
                    continue;

                var embed = await GetEmbedAsync(message);
                await e.Message.RespondAsync(embed: embed);
                _statistics.MessagesQuoted++;

                // stop here
                return;
            }

            // Check for message quote via link
            foreach (Match match in linkMatches)
            {
                DiscordGuild guild = null;
                DiscordChannel channel = null;
                DiscordMessage message = null;
                var member = await e.Guild.GetMemberAsync(e.Author.Id);

                try
                {
                    var guildId = ulong.Parse(match.Groups[1].Value);
                    guild = await e.Client.GetGuildAsync(guildId);

                    var channelId = ulong.Parse(match.Groups[2].Value);
                    channel = guild.GetChannel(channelId);

                    // Check if user has permissions for that channel to avoid leaking messages
                    if (!channel.PermissionsFor(member)
                                .HasPermission(Permissions.AccessChannels | Permissions.ReadMessageHistory))
                    {
                        var failEmbed = new DiscordEmbedBuilder
                        {
                            Color = Colors.Red500,
                            Description = language["quoting.fail.channel"]
                        };

                        await e.Message.RespondAsync(embed: failEmbed);

                        return;
                    }

                    var messageId = ulong.Parse(match.Groups[3].Value);
                    message = await channel.GetMessageAsync(messageId);
                }
                catch (UnauthorizedException)
                {
                    var failEmbed = new DiscordEmbedBuilder().WithColor(Colors.Red500);

                    if (guild == null)
                    {
                        failEmbed.WithDescription(language["quoting.fail.server"]);
                    }
                    else if (channel == null)
                    {
                        failEmbed.WithDescription(language["quoting.fail.channel"]);
                    }
                    else if (message == null)
                    {
                        failEmbed.WithDescription(language["quoting.fail.message"]);
                    }

                    await e.Message.RespondAsync(embed: failEmbed);
                    return;
                }

                if (message == null || message.MessageType != MessageType.Default)
                    continue;

                if (message.Channel.IsNSFW && !e.Channel.IsNSFW)
                {
                    await e.Message.RespondAsync(embed: new DiscordEmbedBuilder
                    {
                        Color = Colors.Red500,
                        Title = language["quoting.nsfw.title"],
                        Description = language["quoting.nsfw.description"]
                    });

                    return;
                }

                var quotingAuthorData = _userRepository.Get(message.Author);
                var content = quotingAuthorData.NotifyOnQuote ? message.Author.Mention : null;

                var embed = await GetEmbedAsync(message);
                await e.Message.RespondAsync(content, embed: embed);
                _statistics.MessagesQuoted++;

                // stop here
                return;
            }
        }

        private async Task SendOneTimeNotice(DiscordMember member, UserData data, Language language)
        {
            if (data.OneTimeNotices.HasFlag(OneTimeNotices.Quoting))
                return;
            await member.SendMessageAsync(embed: new DiscordEmbedBuilder
            {
                Color = Colors.Amber500,
                Title = language["quoting.notice.title"],
                Description = language["quoting.notice.description", "cb!user config quoting true"],
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = language["otn.footer"]
                }
            });

            data.OneTimeNotices |= OneTimeNotices.Quoting;
            _userRepository.Save(data);
        }

        /// <summary>
        /// Generates a <see cref="DiscordEmbed"/> for a <see cref="DiscordMessage"/>.
        /// </summary>
        private async Task<DiscordEmbedBuilder> GetEmbedAsync(DiscordMessage message)
        {
            string GetFooter()
            {
                var values = new System.Collections.Generic.List<string>
                {
                    message.Channel.Guild.Name,
                    '#' + message.Channel.Name
                };

                if (message.Embeds.Count > 0)
                    values.Add("contains embed");

                return string.Join(" • ", values);
            }

            string GetImageUrl()
            {
                if (message.Embeds.Any())
                {
                    var imageEmbed = message.Embeds.FirstOrDefault(e => e.Type == "image");
                    if (imageEmbed != null)
                    {
                        return imageEmbed.Url.ToString();
                    }
                }

                if (message.Attachments.Count <= 0)
                    return null;

                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

                var imageAttachment = message.Attachments.FirstOrDefault(a => imageExtensions.Any(ext => Path.GetExtension(a.FileName)
                    .Equals(ext, StringComparison.OrdinalIgnoreCase)));

                return imageAttachment?.Url;
            }

            var userData = _userRepository.Get(message.Author);
            var color = await userData.GetColorAsync(Client, _userRepository);

            return new DiscordEmbedBuilder
            {
                Color = color,
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    IconUrl = message.Author.AvatarUrl,
                    Name = message.Author.Username
                },
                Description = $"{message.Content} [{Emoji.IconMessageUp}]({message.JumpLink} \"Jump to {message.Author.Username}'s message in #{message.Channel.Name}\")",
                ImageUrl = GetImageUrl(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    IconUrl = message.Channel.Guild.IconUrl,
                    Text = GetFooter()
                },
                Timestamp = message.Timestamp
            };
        }
    }
}