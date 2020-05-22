using System.Linq;
using System.Threading.Tasks;
using CraftBot.Extensions;
using CraftBot.Localization;
using CraftBot.Repositories;
using Disqord.Bot;
using DSharpPlus;
using DSharpPlus.Entities;
using Qmmands;

namespace CraftBot.Discord.Commands
{
    [GuildOnly]
    [Group("moderation", "mod")]
	public class ModerationCommandModule : ModuleBase<DiscordCommandContext>
	{
        public LocalizationEngine Localization { get; set; }
        public UserRepository UserRepository { get; set; }

        [RequireUserPermissions(Permissions.KickMembers)]
        [RequireBotPermissions(Permissions.KickMembers)]
        [Command("kick")]
        public async Task KickUser(DiscordUser user, string reason = null)
        {
            var botReason = $"Kick Issuer: {Context.User.Username} ({Context.User.Id})";
            
            if (!string.IsNullOrWhiteSpace(reason))
                botReason += $"\nReason: {reason}";

            await Context.Guild.BanMemberAsync(user.Id, reason: botReason);

            var embed = new DiscordEmbedBuilder
            {
                Color = Colors.LightGreen500,
                Title = "Kick Succeeded"
            };
            
            embed.AddField("Kicked User", user.Mention, true);
            embed.AddField("Kick Issuer", Context.User.Mention, true);

            if (!string.IsNullOrWhiteSpace(reason))
                embed = embed.AddField("Reason", reason);

            await Context.RespondAsync(embed);
        }

        [RequireUserPermissions(Permissions.BanMembers)]
        [RequireBotPermissions(Permissions.BanMembers)]
        [Command("ban")]
        public async Task BanUser(DiscordUser user, string reason = null)
        {
            var botReason = $"Ban Issuer: {Context.User.Username} ({Context.User.Id})";
            if (!string.IsNullOrWhiteSpace(reason))
                botReason += $"\nReason: {reason}";

            await Context.Guild.BanMemberAsync(user.Id, reason: botReason);

            var embed = new DiscordEmbedBuilder
            {
                Color = Colors.LightGreen500,
                Title = "Ban Succeeded"
            };

            embed.AddField("Banned User", user.Mention, true);
            embed.AddField("Ban Issuer", Context.User.Mention, true);

            if (!string.IsNullOrWhiteSpace(reason))
                embed = embed.AddField("Reason", reason);

            await Context.RespondAsync(embed);
        }

        [Command("move")]
        [Description("Moves one message from one channel to another.\nNote: the messages have to be inside the same server.")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        [RequireBotPermissions(Permissions.ManageWebhooks)]
        public async Task MoveMessage(DiscordMessage message, DiscordChannel channel)
        {
            var language = UserRepository.Get(Context.User).GetLanguage(Localization);

            // checks if the source message's guild and the target channel's guild are the same.
            if (message.Channel.GuildId != channel.GuildId)
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = language["mod.move.nocross.title"],
                    Description = language["mod.move.nocross.description"]
                });
                return;
            }

            var member = await Context.Guild.GetMemberAsync(Context.Client.CurrentUser.Id);

            if (message.Author != Context.User)
            {
                if (!Context.Member.PermissionsIn(channel).HasPermission(Permissions.ManageMessages))
                {
                    await Context.RespondAsync(new DiscordEmbedBuilder
                    {
                        Color = Colors.Red500,
                        Title = language["mod.move.notown.title"],
                        Description = language["mod.move.notown.description"]
                    });
                    return;
                }
            }

            // checks if CraftBot has ManageChannels in the source channel
            if (!message.Channel.PermissionsFor(member).HasPermission(Permissions.ManageChannels))
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = language["mod.move.msp.title"],
                    Description = language["mod.move.msp.description"]
                });
                return;
            }

            // checks if CraftBot has ManageWebhook in the target channel
            if (!channel.PermissionsFor(member).HasPermission(Permissions.ManageWebhooks))
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = language["mod.move.mtp.title"],
                    Description = language["mod.move.mtp.description", channel.Mention]
                });
                return;
            }

            const string webhookName = "CraftBot: Message moving webhook";

            var webhooks = await channel.GetWebhooksAsync();
            var webhook = webhooks.FirstOrDefault(wh => wh.User == Context.Client.CurrentUser && wh.Name == webhookName);

            if (webhook == null)
            {
                var reason = $"{Context.User.Username} ({Context.User.Id}) moved message ({message.Id}) from #{message.Channel.Name} to #{channel.Name}";
                webhook = await channel.CreateWebhookAsync(webhookName, reason: reason);
            }

            await webhook.ExecuteAsync(message);

            await message.DeleteAsync();
        }
	}
}