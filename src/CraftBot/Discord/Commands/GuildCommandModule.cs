using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CraftBot.Localization;
using CraftBot.Repositories;
using Disqord.Bot;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Qmmands;

namespace CraftBot.Discord.Commands
{
	[Group("guild", "server")]
    [Description("Commands for servers")]
    [GuildOnly]
	public class GuildCommandModule : ModuleBase<DiscordCommandContext>
	{
		public GuildRepository GuildRepository { get; set; }
        public UserRepository UserRepository { get; set; }
        public LocalizationEngine Localization { get; set; }

        [Command("", "info")]
        [Description("Gives information about this server.")]
        public Task Info() => Info(Context.Guild);

        [Command("", "info")]
        [Description("Gives information about a server.")]
        public async Task Info(ulong id) => await Info(await Context.Client.GetGuildAsync(id));

        private async Task Info(DiscordGuild guild)
        {
            var language = UserRepository.Get(Context.User).GetLanguage(Localization);

            var data = GuildRepository.Get(guild);
            var builder = new StringBuilder();

            var creationDateString = guild.CreationTimestamp.ToString(language.CultureInfo.DateTimeFormat);
            builder.AppendLine($"{Emoji.IconCalendarPlus} {creationDateString}");

            builder.AppendLine($"{Emoji.IconAccountMultiple} {language.GetCounter(guild.MemberCount, "members")}");
            builder.AppendLine($"{Emoji.Blank} {GetMemberBreakdown(guild, language)}");

            builder.AppendLine(
                $"{(data.Public ? Emoji.IconCheckboxMarked : Emoji.IconCheckboxBlankOutline)} {language["guild.public.title"]}");

            var color = await data.GetColorAsync(Context.Client, GuildRepository);
            var embed = new DiscordEmbedBuilder()
                .WithColor(color)
                .WithAuthor(guild.Name, null, guild.IconUrl)
                .WithDescription(builder.ToString());

            var banner = guild.Banner;
            if (!string.IsNullOrWhiteSpace(banner))
                embed.WithImageUrl(guild.BannerUrl);

            if (data.Features.Count > 0)
                embed.AddField("Features", string.Join(", ", data.Features));

            await Context.RespondAsync(embed);
        }

        private string GetMemberBreakdown(DiscordGuild guild, Language language)
        {
            var users = guild.Members.Values.ToList();
            var counts = new List<string>();

            var allCount = users.Count;
            var botCount = users.Count(m => m.IsBot);
            var adminCount = users.Count(m => !m.IsBot && m.Roles.Any(r => r.CheckPermission(Permissions.Administrator) == PermissionLevel.Allowed));
            var otherCount = allCount - botCount - adminCount;

            if (0 < adminCount)
                counts.Add(language.GetCounter(adminCount, "administrators"));

            if (0 < botCount)
                counts.Add(language.GetCounter(botCount, "bots"));

            if (0 < otherCount)
                counts.Add(language.GetCounter(otherCount, "users"));

            return string.Join(", ", counts);
        }

        [Command("emoji", "emojis", "emote", "emotes")]
        [Description("Lists the available emoji on this server.")]
        public async Task ListEmoji()
        {
            var interactivity = Context.Client.GetExtension<InteractivityExtension>();
            var description = string.Empty;

            var emojis = await Context.Guild.GetEmojisAsync();

            if (emojis.Count == 0)
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Description = "This server has no emojis!"
                });
                return;
            }

            description = emojis.Aggregate(description, (current, item) => current + $"{item} `{item.Id}`\n");

            var pages = interactivity.GeneratePagesInContent(description, SplitType.Line);

            await interactivity.SendPaginatedMessageAsync(Context.Channel, Context.User, pages);
        }

        [Command("public")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task TogglePublic(bool enable)
        {
            var guildData = GuildRepository.Get(Context.Guild);
            var userData = UserRepository.Get(Context.User);
            var language = userData.GetLanguage(Localization);
            
            guildData.Public = enable;
            GuildRepository.Save(guildData);
            
            await Context.RespondAsync(new DiscordEmbedBuilder
            {
                Color = await guildData.GetColorAsync(Context.Client, GuildRepository),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = Context.Guild.Name,
                    IconUrl = Context.Guild.IconUrl
                },
                Description = enable ? language["guild.public.set.public"] : language["guild.public.set.private"]
            });
        }
	}
}