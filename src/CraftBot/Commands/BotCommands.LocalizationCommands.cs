using CraftBot.Localization;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using System;
using System.Linq;
using System.Threading.Tasks;
using CraftBot.Repositories;
using DSharpPlus.Interactivity;
using CraftBot.Extensions;

namespace CraftBot.Commands
{
    public partial class BotCommands
    {
        [Group("localization")]
        [Aliases("local")]
        public class LocalizationCommands : BaseCommandModule
        {
            public LocalizationEngine Localization { get; set; }
            public UserRepository UserRepository { get; set; }

            [GroupCommand]
            [Command("info")]
            [Aliases("overview")]
            [Description("Shows information for the current language.")]
            public async Task Info(CommandContext context, string code)
            {
                var userLanguage = UserRepository.Get(context.User).GetLanguage(Localization);
                var language = Localization.GetLanguage(code);

                // Checks if language exists
                if (language == null)
                {
                    await SendLanguageNotFoundMessage(context, userLanguage, code);
                    return;
                }

                var count = language.Values.Count(v => !string.IsNullOrWhiteSpace(v));
                var embed = new DiscordEmbedBuilder().WithTitle($"{language.Flag} {language.Name} / Overview");

                if (Localization.FallbackLanguage == language)
                {
                    embed.WithDescription($"This is the fallback language, it has {count} string(s).");
                }
                else
                {
                    var fallbackCount = Localization.FallbackLanguage.Values.Count(v => !string.IsNullOrWhiteSpace(v));
                    var missingKeys = Localization.FallbackLanguage.Keys.Where(key => !language.Keys.Contains(key));
                    var completion = Math.Round(count / (double)fallbackCount * 100, 2);

                    embed = embed.AddField("Completion", $"{completion}% ({count} / {fallbackCount})");

                    var enumerable = missingKeys as string[] ?? missingKeys.ToArray();
                    var missingCount = enumerable.Count();
                    if (missingCount != 0)
                    {
                        const int showMax = 20;
                        if (missingCount > showMax)
                            embed = embed.AddField("Missing Strings", $"```{string.Join('\n', enumerable.Take(showMax))}\n{missingCount - showMax} more...```");
                        else
                            embed = embed.AddField("Missing Strings", $"```{string.Join('\n', enumerable.Take(showMax))}```");
                    }
                }

                embed.AddField("Authors",
                               string.Join(
                                   '\n',
                                   (
                                       await Task.WhenAll(
                                           language.Authors.Select(async id => await context.Client.GetUserAsync(id))
                                       )
                                   ).Select(u => u.ToFriendlyString()
                                   )
                               )
                );

                await context.RespondAsync(embed: embed);
            }

            [GroupCommand]
            [Command("view")]
            [Description("Shows information about a key or string.")]
            public async Task ViewKey(CommandContext context, string code, string key)
            {
                var userLanguage = UserRepository.Get(context.User).GetLanguage(Localization);
                var language = Localization.GetLanguage(code);
                var interactivity = context.Client.GetExtension<InteractivityExtension>();

                // Checks if language exists
                if (language == null)
                {
                    await SendLanguageNotFoundMessage(context, userLanguage, code);
                    return;
                }

                key = key.ToLowerInvariant();

                var fallback = Localization.FallbackLanguage.ContainsKey(key) ? Localization.FallbackLanguage[key] : null;

                // Checks if key exists in fallback language
                if (fallback == null)
                {
                    await SendKeyNotFoundMessage(context);
                    return;
                }

                var embed = new DiscordEmbedBuilder
                            {
                    Color = Colors.LightBlue500,
                    Title = "#" + key,
                }
                .AddField(Localization.FallbackLanguage.Name, fallback, true)
                .AddField("Current Translation", language.ContainsKey(key) ? language[key] : "*N/A*", true);

                var message = await context.RespondAsync(embed: embed);

                // Enable editing for authors.
                if (language.Authors.Contains(context.User.Id))
                {
                    var editEmoji = DiscordEmoji.FromGuildEmote(context.Client, 534032475358494720);

                    await message.CreateReactionAsync(editEmoji);

                    var result = await interactivity.WaitForReactionAsync(
                       (e) => e.User == context.User &&
                              e.Message == message &&
                              e.Emoji == editEmoji
                    );

                    if (!result.TimedOut)
                    {
                        embed.WithDescription($"{Emoji.IconPencil} Enter your new translation...");
                        await message.ModifyAsync(embed: embed.Build());

                        var response = await interactivity.WaitForMessageAsync((m) => m.Channel == context.Channel &&
                                                                                      m.Author == context.User,
                                                                                       TimeSpan.FromMinutes(3));

                        if (!response.TimedOut)
                        {
                            await ModifyKey(context, language.Code, key, response.Result.Content);

                            await message.DeleteAsync();
                        }
                    }
                }
            }

            [GroupCommand]
            [Command("modify")]
            [Aliases("change", "contribute")]
            [Description("Changes the content of a translation. (language authors only)")]
            public async Task ModifyKey(CommandContext context, string code, string key, [RemainingText] string value)
            {
                var userLanguage = UserRepository.Get(context.User).GetLanguage(Localization);
                var language = Localization.GetLanguage(code);

                // Checks if language exists
                if (language == null)
                {
                    await SendLanguageNotFoundMessage(context, userLanguage, code);
                    return;
                }

                if (language != Localization.FallbackLanguage)
                {
                    var fallback = Localization.FallbackLanguage.ContainsKey(key) ? Localization.FallbackLanguage[key] : null;

                    // Checks if key exists in fallback language
                    if (fallback == null)
                    {
                        await SendKeyNotFoundMessage(context);
                        return;
                    }
                }

                // Checks if user is one of the language authors
                if (!language.Authors.Contains(context.User.Id))
                {
                    await SendNotAuthorMessage(context, userLanguage);
                    return;
                }

                key = key.ToLowerInvariant();

                var oldValue = language.ContainsKey(key) ? language[key] : null;

                language[key] = value;
                Program.SaveLanguage(language);

                await context.RespondAsync(
                    embed: new DiscordEmbedBuilder
                           {
                        Color = Colors.LightGreen500,
                        Title = $"#{key} changed",
                    }
                    .AddField("Old Translation", oldValue ?? "*N/A*", true)
                    .AddField("New Translation", value, true)
                );
            }

            [Command("add")]
            [Description("Adds an user as language author.")]
            [RequireOwner]
            public async Task AddAuthor(CommandContext context, string code, DiscordUser user)
            {
                var language = Localization.GetLanguage(code);
                var userLanguage = UserRepository.Get(context.User).GetLanguage(Localization);

                if (language == null)
                {
                    await SendLanguageNotFoundMessage(context, userLanguage, code);
                    return;
                }

                language.Authors.Add(user.Id);
                Program.SaveLanguage(language);

                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.LightGreen500,
                    Title = "Author added",
                    Description = $"{user.Mention} has been added as author of {language.Name}."
                });
            }

            private async Task SendLanguageNotFoundMessage(CommandContext context, Language userLanguage, string code)
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = userLanguage["language.notfound.title"],
                    Description = userLanguage["language.notfound.description", code]
                });
            }

            private async Task SendNotAuthorMessage(CommandContext context, Language userLanguage)
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = userLanguage["language.notauthor.title"],
                    Description = userLanguage["language.notauthor.description", context.Client.CurrentApplication.Team.Members[0].User.ToFriendlyString()]
                });
            }

            private async Task SendKeyNotFoundMessage(CommandContext context)
            {
                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = "Key doesn't exist",
                    Description = $"The key you're trying to translate doesn't exist in the fallback language ({Localization.FallbackLanguage.Name})."
                });
            }
        }
    }
}