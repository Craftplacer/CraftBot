﻿using CraftBot.Database;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CraftBot.Features
{
    public static class Shitposting
    {
        private const string featureName = "shitposting";

        private static readonly List<ShitpostingEntry> entries = Program.GetJson("shitposting", new List<ShitpostingEntry>());

        private static readonly Random random = new Random();

        public static void Add(DiscordClient client) => client.MessageCreated += Client_MessageCreated;

        private static async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            var data = GuildData.Get(e.Guild);

            if (!data.Features.Contains(featureName))
                return;

            foreach (var entry in entries)
            {
                if (Regex.IsMatch(e.Message.Content, entry.Regex, RegexOptions.IgnoreCase))
                {
                    var i = random.Next(0, entry.Images.Count - 1);
                    var url = entry.Images[i];
                    var embed = new DiscordEmbedBuilder().WithImageUrl(url);
                    await e.Message.RespondAsync(embed: embed);
                }
            }
        }
    }

    public class ShitpostingEntry
    {
        [JsonProperty("regex")]
        public string Regex { get; set; }

        [JsonProperty("images")]
        public List<String> Images { get; set; }
    }
}