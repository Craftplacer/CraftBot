using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CraftBot.Database;
using CraftBot.Extensions;
using CraftBot.Localization;
using CraftBot.Model;
using CraftBot.Repositories;
using Disqord.Bot;
using DSharpPlus;
using DSharpPlus.Entities;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Qmmands;

namespace CraftBot.Discord.Commands
{
    [Group("user")]
	public class UserCommandModule : ModuleBase<DiscordCommandContext>
	{
		public LocalizationEngine Localization { get; set; }
        public UserRepository UserRepository { get; set; }

        [Command("", "info")]
        public async Task Info(DiscordUser user = null)
        {
            if (user == null)
                user = Context.User;

            if (user.Discriminator == null)
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = "User not found",
                    Description = "CraftBot doesn't know or couldn't find the user entered."
                }
                );

                return;
            }

            var data = UserRepository.Get(user);
            var userLanguage = data.GetLanguage(Localization);

            // ReSharper disable once PossibleNullReferenceException : False-positive, look L30
            var member = await Context.Guild.TryGetMemberAsync(user.Id);
            var color = await _getUserColorAsync(Context.Client, member, data);

            var embed = new DiscordEmbedBuilder
            {
                Color = color,
                Title = _getUserTitle(user, member),
                ThumbnailUrl = user.AvatarUrl,
                Description = _getUserDescription(userLanguage, data),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = _buildUserFooter(user)
                }
            };

            _addUserFields(embed, user, member, data, userLanguage);

            await Context.RespondAsync(embed);
        }

        private async Task<DiscordColor> _getUserColorAsync(DiscordClient client, DiscordMember member, UserData data)
        {
            if (member != null)
                return member.Color;

            return await data.GetColorAsync(client, UserRepository);
        }

        private static string _getUserTitle(DiscordUser user, DiscordMember member)
        {
            var builder = new StringBuilder();

            if (user.Presence != null)
                builder.Append(user.Presence.Status.GetEmoji() + ' ');

            builder.Append(user.Username);

            if (user.IsBot)
                builder.Append(' ' + Emoji.IconCog);

            if (member == null)
                return builder.ToString();
            
            
            
            if (member.IsOwner)
                builder.Append(' ' + Emoji.IconStar);

            if (member.VoiceState?.Channel != null)
            {
                if (member.VoiceState.IsServerMuted)
                    builder.Append(' ' + Emoji.IconMicrophoneOffRed);
                else if (member.VoiceState.IsSelfMuted)
                    builder.Append(' ' + Emoji.IconMicrophoneOff);

                if (member.VoiceState.IsServerDeafened)
                    builder.Append(' ' + Emoji.IconHeadphonesOffRed);
                else if (member.VoiceState.IsSelfDeafened)
                    builder.Append(' ' + Emoji.IconHeadphonesOff);
            }

            return builder.ToString();
        }

        private string _buildUserFooter(DiscordUser user)
        {
            var values = new List<string>
            {
                $"ID: {user.Id}"
            };

            if (user.Presence == null)
                values.Add("no presence available");

            return string.Join(" • ", values);
        }

        private void _addUserFields(DiscordEmbedBuilder builder, DiscordUser user, DiscordMember member, UserData data,
            Language language)
        {
            // Creation / Discord Join Date
            builder.AddField(Constants.BlankFieldTitle, _getUserCreationDate(user, language), true);

            // Join Date
            if (member != null)
            {
                builder.AddField(Constants.BlankFieldTitle, _getUserJoinDate(member, language), true);

                if (member.VoiceState?.Channel != null)
                    builder.AddField("Talking in", member.VoiceState.Channel.Name);
            }

            // Biography
            if (!string.IsNullOrWhiteSpace(data.Biography))
                builder.AddField(language["user.bio.title"], data.Biography);

            // Image
            if (!string.IsNullOrWhiteSpace(data.Image))
                builder.WithImageUrl(data.Image);
        }

        private string _getUserDescription(Language userLanguage, UserData data)
        {
            var builder = new StringBuilder();

            if (data.Gender.HasValue)
            {
                var line = string.Empty;

                switch (data.Gender)
                {
                    case Gender.Male:
                        line = $"{Emoji.GenderMale} {userLanguage["gender.male"]}";
                        break;

                    case Gender.Female:
                        line = $"{Emoji.GenderMale} {userLanguage["gender.female"]}";
                        break;
                }

                builder.AppendLine(line);
            } // Gender

            if (data.IsLanguageSet)
            {
                var profileLanguage = data.GetLanguage(Localization);
                builder.AppendLine($"{profileLanguage.Flag} {profileLanguage.Name}");
            } // Language

            if (data.Birthday.HasValue)
            {
                var birthday = data.Birthday.Value;
                builder.AppendLine($"{Emoji.IconCakeVariant} {birthday.Day}/{birthday.Month}/{birthday.Year}");
            } // Birthday

            return builder.ToString();
        }

        private static string _getUserCreationDate([NotNull] DiscordUser user, Language userLanguage)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var builder = new StringBuilder();

            var creationDateString = user.CreationTimestamp.ToString(userLanguage.CultureInfo.DateTimeFormat);
            builder.AppendLine($"{Emoji.IconCalendarStar} {creationDateString}");

            var creationDifference = DateTime.Now - user.CreationTimestamp;
            var creationDifferenceString = creationDifference.GetString(userLanguage, creationDifference.TotalDays < 1);
            builder.AppendLine($"{Emoji.Blank} *{creationDifferenceString}*");

            return builder.ToString();
        }

        private static string _getUserJoinDate([NotNull] DiscordMember member, Language userLanguage)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            var builder = new StringBuilder();

            builder.AppendLine(
                $"{Emoji.IconCalendarPlus} {member.JoinedAt.ToString(userLanguage.CultureInfo.DateTimeFormat)}");

            var joinDifference = DateTime.Now - member.JoinedAt;
            var joinString = joinDifference.GetString(userLanguage, joinDifference.TotalDays < 1);
            builder.AppendLine($"{Emoji.Blank} *{joinString}*");

            return builder.ToString();
        }

        [Command("biography", "bio")]
        [Description("Changes your profile biography.")]
        public async Task ModifyBiography([Remainder] string biography = null)
        {
            var data = UserRepository.Get(Context.User);
            var language = data.GetLanguage(Localization);

            if (string.IsNullOrWhiteSpace(biography))
            {
                data.Biography = string.Empty;
                UserRepository.Save(data);

                await Context.RespondAsync(
                    new DiscordEmbedBuilder().WithDescription(language["user.bio.remove"]));
            }
            else
            {
                data.Biography = biography;
                UserRepository.Save(data);

                await Context.RespondAsync(
                    new DiscordEmbedBuilder().WithDescription($"{language["user.bio.set"]}\n>>> {biography}"));
            }
        }

        [Command("image", "img", "picture", "pic")]
        [Description("Changes your profile picture.")]
        public async Task ModifyImage(string imageUrl = null)
        {
            var data = UserRepository.Get(Context.User);
            var language = data.GetLanguage(Localization);

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                data.Image = string.Empty;
                UserRepository.Save(data);

                await Context.RespondAsync(
                    new DiscordEmbedBuilder().WithDescription(language["user.image.remove"]));
            }
            else
            {
                data.Image = imageUrl;
                UserRepository.Save(data);

                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Description = language["user.image.set"],
                    ImageUrl = imageUrl
                });
            }
        }

        [Command("color", "clr")]
        [Description("Changes your profile color.")]
        public async Task ModifyColor(DiscordColor? color = null)
        {
            var data = UserRepository.Get(Context.User);
            var language = data.GetLanguage(Localization);

            if (color.HasValue)
            {
                data.ColorHex = color.Value.ToString();
                UserRepository.Save(data);

                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = color.Value,
                    Description = language["user.color.set", $"**`{data.ColorHex}`**"]
                });
            }
            else
            {
                data.ColorHex = null;
                UserRepository.Save(data);

                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Description = language["user.color.remove"],
                    Color = await data.GetColorAsync(Context.Client, UserRepository)
                });
            }
        }

        public async Task ClearGender()
        {
            var data = UserRepository.Get(Context.User);

            data.Gender = null;
            UserRepository.Save(data);

            await Context.RespondAsync(new DiscordEmbedBuilder
            {
                Description = "Your gender has been cleared"
            });
        }

        [Command("gender", "sex")]
        [Description("Changes your profile gender.")]
        public async Task ModifyGender([Description("Which gender that should be set on your profile, e.g. 'male' or 'female'")]
                                       string gender)
        {
            var data = UserRepository.Get(Context.User);
            Gender? g = null;

            if (new[] { "m", "male", "boy", "man" }.Any(str => gender.Equals(str, StringComparison.OrdinalIgnoreCase)))
            {
                g = Gender.Male;
            }
            else if (new[] { "f", "female", "girl", "woman" }.Any(str =>
                  gender.Equals(str, StringComparison.OrdinalIgnoreCase)))
            {
                g = Gender.Female;
            }

            if (g.HasValue)
            {
                data.Gender = g;

                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Description = $"Your gender has been set to {data.Gender}"
                });

                UserRepository.Save(data);
            }
            else
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Description = "The gender specified is unknown"
                });
            }
        }

        [Command("birthday")]
        [Description("Clears your birthday.")]
        public async Task ClearBirthday()
        {
            var data = UserRepository.Get(Context.User);
            var language = data.GetLanguage(Localization);
            data.Birthday = null;
            UserRepository.Save(data);

            await Context.RespondAsync(new DiscordEmbedBuilder
            {
                Description = language["user.birthday.clear"]
            });
        }

        [Command("birthday")]
        [Description("Changes your birthday.")]
        public async Task ChangeBirthday(int day, int month, int year = 1)
        {
            var data = UserRepository.Get(Context.User);
            var language = data.GetLanguage(Localization);
            data.Birthday = new DateTime(year, month, day);
            UserRepository.Save(data);

            await Context.RespondAsync(new DiscordEmbedBuilder
            {
                Description = language["user.birthday.set",
                    data.Birthday.Value.ToString(language.CultureInfo.DateTimeFormat)]
            });
        }

        [Command("language")]
        [Description("Changes your language.")]
        public async Task ListLanguages()
        {
            var language = UserRepository.Get(Context.User).GetLanguage(Localization);

            await Context.RespondAsync(new DiscordEmbedBuilder
            {
                Title = language["language.list.title"],
                Description = $"{language["language.list.description"]}\n\n{_getLanguageList()}",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = language[
                        "language.list.contribute",
                        Context.Client.CurrentApplication.Team.Members[0].User.ToFriendlyString()
                    ]
                }
            });
        }

        private string _getLanguageList()
        {
            var builder = new StringBuilder();

            foreach (var language in Localization.Languages)
            {
                var completedPercent = Math.Round(language.Completion, 2);
                builder.AppendLine(
                    $"{language.Flag} `{language.Code}` **{language.Name}** ({language["language.list.status", $"{completedPercent}%"]})");
            }

            return builder.ToString();
        }

        [Command("language", "lang")]
        [Description("Changes your language.")]
        public async Task ChangeLanguage(string code)
        {
            code = code.ToLowerInvariant();

            var data = UserRepository.Get(Context.User);
            var language = data.GetLanguage(Localization);
            var newLanguage = Localization.GetLanguage(code);

            if (newLanguage == null)
            {
                await Context.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = Colors.Red500,
                    Title = language["language.notfound.title"],
                    Description = language["language.notfound.description", code]
                });

                return;
            }

            data.Language = newLanguage.Code;
            UserRepository.Save(data);

            await Context.RespondAsync(new DiscordEmbedBuilder
            {
                Color = Colors.LightGreen500,
                Title = newLanguage["greeting"],
                Description = newLanguage["language.set", newLanguage.Name,
                    await _getAuthorList(Context.Client, newLanguage.Authors)]
            });
        }

        private static async Task<string> _getAuthorList(DiscordClient client, IEnumerable<ulong> authors)
        {
            var users = new List<DiscordUser>();

            foreach (var author in authors) users.Add(await client.GetUserAsync(author));

            var strings = users.Select(user => user.ToFriendlyString()).ToList();
            users.Clear();

            var result = string.Join(", ", strings);
            strings.Clear();

            return result;
        }

        [Command("export")]
        [Description("This command gives the user the ability to export their data from CraftBot.")]
        [PrivateOnly]
        public async Task Export()
        {
            var data = UserRepository.Get(Context.User);
            var json = JsonConvert.SerializeObject(data);
            var array = Encoding.UTF8.GetBytes(json);

            await using var stream = new MemoryStream(array);

            var filename = $"Data of {Context.User.Username} ({Context.User.Id}).json";
            await Context.Message.RespondWithFileAsync(filename, stream);
        }
	}
}