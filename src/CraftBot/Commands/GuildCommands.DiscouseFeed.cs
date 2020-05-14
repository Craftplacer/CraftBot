using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CraftBot.Features;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace CraftBot.Commands
{
	public partial class GuildCommands
	{
		[Group("discourse")]
        [Description("Discourse Feed")]
        [RequireGuild]
        public class DiscourseCommands : BaseCommandModule
        {
            [GroupCommand]
            [Command("view")]
            public async Task ViewAsync(CommandContext context)
            {
                var feed = context.Client.GetExtension<DiscourseFeed>();
                var entry = feed.Entries.SingleOrDefault(e => e.GuildId == context.Guild.Id);

                if (entry == null)
                {
                    await context.RespondAsync("This server has no Discourse set to feed to.");
                    return;
                }
                
                var channel = context.Guild.GetChannel(entry.ChannelId);
                await context.RespondAsync($"Feeding <{entry.DiscourseUrl}> to {channel.Mention} every **{DiscourseFeed.CheckInterval} minutes**.");
            }

            [GroupCommand]
            [Command("set")]
            [RequireUserPermissions(Permissions.Administrator)]
            [RequireBotPermissions(Permissions.ManageWebhooks)]
            public async Task SetAsync(CommandContext context, string url, DiscordChannel channel)
            {
                var feed = context.Client.GetExtension<DiscourseFeed>();
                var entry = feed.Entries.SingleOrDefault(e => e.GuildId == context.Guild.Id);

                if (entry != null)
                {
                    await context.RespondAsync("Please remove the current feed first, by typing `cb!server discourse remove`");
                    return;
                }

                DiscordWebhook webhook;
                await using (var fileStream = new FileStream(Path.Combine("assets", "feed.png"), FileMode.Open))
                {
                    webhook = await channel.CreateWebhookAsync("Discourse Feed", fileStream, $"{context.User.Mention} linked a Discourse forum to this channel");
                }

                var newEntry = new DiscourseFeed.DiscourseFeedEntry
                {
                    ChannelId = channel.Id,
                    GuildId = channel.GuildId,
                    WebhookId = webhook.Id,
                    DiscourseUrl = url,
                    LastChecked = DateTime.Now.AddDays(-3)
                };

                feed.Entries.Add(newEntry);
                feed.Save();

                await context.RespondAsync("The feed has been created.");
            }

            [Command("remove")]
            [RequireUserPermissions(Permissions.Administrator)]
            [RequireBotPermissions(Permissions.ManageWebhooks)]
            public async Task RemoveAsync(CommandContext context)
            {
                var feed = context.Client.GetExtension<DiscourseFeed>();
                var entry = feed.Entries.SingleOrDefault(e => e.GuildId == context.Guild.Id);

                if (entry == null)
                {
                    await context.RespondAsync("No feed has been found");
                    return;
                }
                
                var webhook = await entry.GetWebhookAsync(context.Client);
                await webhook.DeleteAsync();

                feed.Entries.Remove(entry);
                feed.Save();

                await context.RespondAsync("Feed has been removed.");
            }

            [Command("force")]
            [Description("Forces an update of the feed.")]
            [RequireUserPermissions(Permissions.ManageChannels)]
            public async Task ForceFeed(CommandContext context)
            {
                var feed = context.Client.GetExtension<DiscourseFeed>();
                var entry = feed.Entries.SingleOrDefault(e => e.GuildId == context.Guild.Id);

                await context.RespondAsync("Updating...");

                await feed.UpdateEntryAsync(entry);
                feed.Save();
            }

            [Command("rewind")]
            [Description("Rewinds the date and time when posts were last checked.")]
            [RequireUserPermissions(Permissions.Administrator)]
            public async Task RewindAsync(CommandContext context, DateTime dateTime)
            {
                var feed = context.Client.GetExtension<DiscourseFeed>();
                var entry = feed.Entries.SingleOrDefault(e => e.GuildId == context.Guild.Id);

                if (entry == null)
                {
                    await context.RespondAsync("No feed has been found");
                    return;
                }

                await context.RespondAsync($"Rewinding to {dateTime.ToString(CultureInfo.InvariantCulture)}...");

                entry.LastChecked = dateTime;
                await feed.UpdateEntryAsync(entry);
                feed.Save();
            }
        }
	}
}