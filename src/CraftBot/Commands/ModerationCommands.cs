using CraftBot.Localization;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;
using CraftBot.Repositories;
using CraftBot.Extensions;

namespace CraftBot.Commands
{
    [RequireGuild]
    [Group("moderation")]
    [Aliases("mod")]
    public class ModerationCommands : BaseCommandModule
    {
        public LocalizationEngine Localization { get; set; }
        public UserRepository UserRepository { get; set; }

        [RequirePermissions(Permissions.KickMembers)]
        [Command("kick")]
        public async Task KickUser(CommandContext context, DiscordUser user, string reason = null)
        {
            var botReason = $"Kick Issuer: {context.User.Username} ({context.User.Id})";
            
            if (!string.IsNullOrWhiteSpace(reason))
                botReason += $"\nReason: {reason}";

            await context.Guild.BanMemberAsync(user.Id, reason: botReason);

            var embed = new DiscordEmbedBuilder
            {
                Color = Colors.LightGreen500,
                Title = "Kick Succeeded"
            }
            .AddField("Kicked User", user.Mention, true)
            .AddField("Kick Issuer", context.User.Mention, true);

            if (!string.IsNullOrWhiteSpace(reason))
                embed = embed.AddField("Reason", reason);

            await context.RespondAsync(embed: embed);
        }

        [RequirePermissions(Permissions.BanMembers)]
        [Command("ban")]
        public async Task BanUser(CommandContext context, DiscordUser user, string reason = null)
        {
            var botReason = $"Ban Issuer: {context.User.Username} ({context.User.Id})";
            if (!string.IsNullOrWhiteSpace(reason))
                botReason += $"\nReason: {reason}";

            await context.Guild.BanMemberAsync(user.Id, reason: botReason);

            var embed = new DiscordEmbedBuilder
                        {
                Color = Colors.LightGreen500,
                Title = "Ban Succeeded"
            }
            .AddField("Banned User", user.Mention, true)
            .AddField("Ban Issuer", context.User.Mention, true);

            if (!string.IsNullOrWhiteSpace(reason))
                embed = embed.AddField("Reason", reason);

            await context.RespondAsync(embed: embed);
        }

        ///[Command("spacify")]
        ///[Aliases("space")]
        ///[Description("be gone dashes")]
        ///[RequirePermissions(Permissions.ManageChannels)]
        ///public async Task Spacify(CommandContext context, DiscordChannel channel = null)
        ///{
        ///    if (channel == null)
        ///        channel = context.Channel;
        ///
        ///    var newName = channel.Name.Replace('-', '\u2009');
        ///
        ///    if (newName == channel.Name)
        ///    {
        ///        await context.RespondAsync(embed: new DiscordEmbedBuilder()
        ///        {
        ///            Color = Colors.Red500,
        ///            Description = "The channel's name is already spaced out."
        ///        });
        ///    }
        ///    else
        ///    {
        ///        await channel.ModifyAsync((model) => model.Name = newName);
        ///
        ///        await context.RespondAsync(embed: new DiscordEmbedBuilder()
        ///        {
        ///            Color = Colors.LightGreen500,
        ///            Description = "The channel's name has been spaced out"
        ///        });
        ///    }
        ///}
        ///
        ///[Command("spacify")]
        ///public async Task Spacify(CommandContext context, [Description("Tip: Specify '*' as argument to spacify your whole server.")] string all)
        ///{
        ///    if (all != "*")
        ///        return;
        ///
        ///    var message = await context.RespondAsync(embed: new DiscordEmbedBuilder()
        ///    {
        ///        Color = Colors.LightBlue500,
        ///        Title = "Spacify",
        ///        Description = "Renaming channels..."
        ///    });
        ///
        ///    try
        ///    {
        ///        foreach (var channel in context.Guild.Channels.Values)
        ///        {
        ///            if (channel.IsCategory)
        ///                continue;
        ///
        ///            var newName = channel.Name.Replace('-', '\u2009');
        ///
        ///            if (newName != channel.Name)
        ///                await channel.ModifyAsync((model) => model.Name = newName);
        ///        }
        ///
        ///        await message.ModifyAsync(embed: new DiscordEmbedBuilder()
        ///        {
        ///            Color = Colors.LightGreen500,
        ///            Title = "Spacify",
        ///            Description = "All channels have been renamed!"
        ///        }.Build());
        ///    }
        ///    catch (UnauthorizedException ex)
        ///    {
        ///        await message.ModifyAsync(embed: new DiscordEmbedBuilder()
        ///        {
        ///            Color = Colors.Red500,
        ///            Title = "Spacify",
        ///            Description = "CraftBot failed to rename a channel because of missing permissions."
        ///        }.Build());
        ///    }
        ///}

        [Command("move")]
        [Description("Moves one message from one channel to another.\nNote: the messages have to be inside the same server.")]
        [RequirePermissions(Permissions.ManageMessages)]
        [RequireBotPermissions(Permissions.ManageWebhooks)]
        public async Task MoveMessage(CommandContext context, DiscordMessage message, DiscordChannel channel)
        {
            var language = UserRepository.Get(context.User).GetLanguage(Localization);

            // checks if the source message's guild and the target channel's guild are the same.
            if (message.Channel.GuildId != channel.GuildId)
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = language["mod.move.nocross.title"],
                    Description = language["mod.move.nocross.description"]
                });
                return;
            }

            var member = await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id);

            if (message.Author != context.User)
            {
                if (!context.Member.PermissionsIn(channel).HasPermission(Permissions.ManageMessages))
                {
                    await context.RespondAsync(embed: new DiscordEmbedBuilder
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
                await context.RespondAsync(embed: new DiscordEmbedBuilder
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
                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = language["mod.move.mtp.title"],
                    Description = language["mod.move.mtp.description", channel.Mention]
                });
                return;
            }

            const string webhookName = "CraftBot: Message moving webhook";

            var webhooks = await channel.GetWebhooksAsync();
            var webhook = webhooks.FirstOrDefault(wh => wh.User == context.Client.CurrentUser && wh.Name == webhookName);

            if (webhook == null)
            {
                var reason = $"{context.User.Username} ({context.User.Id}) moved message ({message.Id}) from #{message.Channel.Name} to #{channel.Name}";
                webhook = await channel.CreateWebhookAsync(webhookName, reason: reason);
            }

            await webhook.ExecuteAsync(message);

            await message.DeleteAsync();
        }
    }
}