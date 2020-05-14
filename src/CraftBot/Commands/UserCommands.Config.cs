using System.Diagnostics;
using CraftBot.Localization;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using System.Threading.Tasks;
using CraftBot.Database;
using CraftBot.Repositories;
using CraftBot.Extensions;

namespace CraftBot.Commands
{
    public partial class UserCommands
    {
        [Group("config")]
        public class Config : BaseCommandModule
        {
            public LocalizationEngine Localization { get; set; }
            public UserRepository UserRepository { get; set; }

            [GroupCommand]
            [Command("view")]
            public async Task View(CommandContext context)
            {
                var data = UserRepository.Get(context.User);
                var language = data.GetLanguage(Localization);

                string getLine(bool value, string key, string configKey)
                {
                    var line = $"{(value ? Emoji.IconCheckboxMarked : Emoji.IconCheckboxBlankOutline)} {language[key]} `{configKey}`\n";
                    return line;
                }

                var description = (
                    getLine(data.Features.HasFlag(UserFeatures.Quoting), "quoting.title", "quoting") +
                    getLine(data.NotifyOnQuote, "quoting.notify", "quoting-notify")
                );

                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = await data.GetColorAsync(context.Client, UserRepository),
                    Author = context.User.GetEmbedAuthor("Config"),
                    Description = description
                });
            }

            [GroupCommand]
            [Command("set")]
            public async Task Set(CommandContext context, string key, bool value)
            {
                var data = UserRepository.Get(context.User);

                switch (key)
                {
                    case "quoting":
                    {
                        _changeFeature(ref data, UserFeatures.Quoting, value);
                        break;
                    }
                    case "quoting-notify":
                    {
                        data.NotifyOnQuote = value;
                        break;
                    }
                    default:
                    {
                        await context.RespondAsync(embed: new DiscordEmbedBuilder
                        {
                            Color = Colors.Red500,
                            Description = $"Unknown key {key}, check the name of the key and try again."
                        });
                        return;
                    }
                }

                UserRepository.Save(data);

                await context.RespondAsync(embed: new DiscordEmbedBuilder
                {
                    Color = Colors.LightGreen500,
                    Description = $"{key} has been set to {value}."
                });
            }

            private void _changeFeature(ref UserData data, UserFeatures feature, bool add)
            {
                var hasFeature = data.Features.HasFlag(feature);

                if (add && !hasFeature)
                {
                    data.Features |= feature;
                }
                else if (!add && hasFeature)
                {
                    data.Features &= ~feature;
                }
                else
                {
                    Debug.WriteLine("oof");
                }
            }
        }
    }
}