using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CraftBot.Extensions;
using Craftplacer.Discourse;
using Craftplacer.Discourse.Model;
using DSharpPlus;
using DSharpPlus.Entities;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace CraftBot.Features
{
    public class DiscourseFeed : BaseExtension
    {
        public List<DiscourseFeedEntry> Entries { get; private set; } = new List<DiscourseFeedEntry>();

        public const byte CheckInterval = 5;

        public DiscourseFeed()
        {
            var timer = new Timer
            {
                AutoReset = true,
                Enabled = true,
                Interval = CheckInterval * 60 * 1000
            };

            timer.Elapsed += Timer_Elapsed;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Client == null)
                return;

            foreach (var entry in Entries)
                await UpdateEntryAsync(entry);

            Save();
        }

        public async Task UpdateEntryAsync(DiscourseFeedEntry entry)
        {
            var time = DateTime.UtcNow;

            if (time < entry.LastChecked)
                entry.LastChecked = time;

            var webhook = await entry.GetWebhookAsync(Client);
            if (webhook == null)
            {
                try
                {
                    var channel = await entry.GetChannelAsync(Client);
                    await channel.SendMessageAsync("Webhook has been deleted or couldn't be found, please re-add this feed.");
                }
                catch (Exception ex)
                {
                    Logger.Error(new Exception($"Failed to send fail message to {entry.GuildId}", ex), "Discourse Feed");
                }

                Logger.Warning($"Couldn't find webhook {entry.WebhookId} for {entry.GuildId}", "Discourse Feed");
                return;
            }

            try
            {
                var baseUri = new Uri(entry.DiscourseUrl);
                using var discourse = new DiscourseApi(baseUri);
                
                var response = await discourse.GetLatestTopicsAsync();
                var newTopics = response.Topics.Topics
                    .Where(t =>
                    {
                        return entry.LastChecked < t.CreatedAt;
                    })
                    .OrderBy(t => t.Id);

                var embeds = new List<DiscordEmbed>();

                foreach (var topic in newTopics)
                {
                    var originalPoster = response.Users.Single(u => u.Id == topic.Posters.First().UserId);

                    var fullTopic = await discourse.GetTopicAsync(topic.Id);
                    var originalPostId = fullTopic.PostStream.Stream.First();
                    var originalPost = await discourse.GetPostAsync(originalPostId);

                    var embed = ToEmbed(fullTopic, originalPost, originalPoster, discourse.BaseUri.ToString());
                    embeds.Add(embed);
                }

                if (embeds.Count > 0)
                {
                    var builder = new DiscordWebhookBuilder();
                    builder.AddEmbeds(embeds);
                    await webhook.ExecuteAsync(builder);
                }

                entry.LastChecked = time;
            }
            catch (Exception ex)
            {
                Logger.Error(new Exception($"Failed to feed a Discourse forum for {entry.GuildId}", ex), "Discourse Feed");
            }
        }

        protected override void Setup(DiscordClient client)
        {
            Client = client;
            Load();
        }

        public void Load()
        {
            var path = Path.Combine("config", "discourse");
            Entries = Helpers.GetJson(path, new List<DiscourseFeedEntry>());
        }

        public void Save()
        {
            var path = Path.Combine("config", "discourse");
            Helpers.SaveJson(path, Entries);
        }

        public static DiscordEmbed ToEmbed(DiscourseTopic topic, DiscoursePost post, DiscourseUser user, string baseUrl)
        {
            var authorName = user.Username;

            if (!string.IsNullOrWhiteSpace(user.Name) && user.Name.Length <= 30)
                authorName += $" ({user.Name})";

            Uri.TryCreate(topic.ImageUrl, UriKind.Absolute, out var imageUrl);

            if (imageUrl == null)
                Logger.Warning($"Failed to create URI for {topic.ImageUrl}", "Discourse Feed");
            
            return new DiscordEmbedBuilder
            {
                Author = new EmbedAuthor
                {
                    Name = authorName,
                    IconUrl = user.GetAvatarUrl(baseUrl, 64),
                    Url = $"{baseUrl}u/{user.Username}",
                },
                Title = topic.Title,
                Description = post.Raw.Cut(512),
                Timestamp = topic.CreatedAt,
                ImageUrl = imageUrl?.ToString(),
                Url = $"{baseUrl}t/{topic.Slug}/{topic.Id}"
            }.Build();
        }

        public class DiscourseFeedEntry
        {
            public ulong GuildId { get; set; }
            public ulong ChannelId { get; set; }
            public ulong WebhookId { get; set; }

            public DateTime LastChecked { get; set; }

            public string DiscourseUrl { get; set; }


            private DiscordGuild _guild;
            private DiscordChannel _channel;
            private DiscordWebhook _webhook;

            public async Task<DiscordGuild> GetGuildAsync(DiscordClient client)
            {
                if (_guild == null)
                {
                    _guild = await client.GetGuildAsync(GuildId);
                }
                
                return _guild;
            }

            public async Task<DiscordChannel> GetChannelAsync(DiscordClient client)
            {
                if (_channel == null)
                {
                    var guild = await GetGuildAsync(client);
                    _channel = guild.GetChannel(ChannelId);
                }

                return _channel;
            }
            
            public async Task<DiscordWebhook> GetWebhookAsync(DiscordClient client)
            {
                if (_webhook == null)
                {
                    var channel = await GetChannelAsync(client);
                    var webhooks = await channel.GetWebhooksAsync();
                    _webhook = webhooks.SingleOrDefault(w => w.Id == WebhookId);
                }
                
                return _webhook;
            }
        }
    }
}