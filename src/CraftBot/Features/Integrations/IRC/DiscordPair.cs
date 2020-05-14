using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace CraftBot.Features.Integrations.IRC
{
    public class DiscordPair
    {
        public DiscordPair()
        {
            
        }

        public DiscordPair(DiscordChannel channel, DiscordWebhook webhook)
        {
            ChannelId = (Channel = channel).Id;
            GuildId = (Guild = channel.Guild).Id;
            WebhookId = (Webhook = webhook).Id;
        }
        
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong WebhookId { get; set; }
        
        [JsonIgnore]
        public DiscordGuild Guild { get; set; }
            
        [JsonIgnore]
        public DiscordChannel Channel { get; set; }
            
        [JsonIgnore]
        public DiscordWebhook Webhook { get; set; }
    }
}