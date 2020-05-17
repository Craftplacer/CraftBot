using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CraftBot
{
    /// <summary>
    /// Object that holds statistics about the bot.
    /// </summary>
    public class Statistics
    {
        public DateTime StartTime = Process.GetCurrentProcess().StartTime;

        [JsonIgnore]
        public DateTime CurrentStartTime = Process.GetCurrentProcess().StartTime;

        public long CommandsErrored { get; set; }
        public long CommandsExecuted { get; set; }

        public long CommandsMistyped { get; set; }

        public long MessagesQuoted { get; set; }

        [JsonIgnore, BsonIgnore]
        public long CommandsTotal => CommandsErrored + CommandsMistyped + CommandsExecuted;

        public List<Tuple<DateTime, Exception>> Errors = new List<Tuple<DateTime, Exception>>();

        [JsonIgnore, BsonIgnore]
        public decimal ErrorRate
        {
            get
            {
                if (CommandsTotal == 0)
                    return 0;

                return CommandsErrored / (decimal)CommandsTotal;
            }
        }

        [JsonIgnore, BsonIgnore]
        public decimal MistypeRate
        {
            get
            {
                if (CommandsTotal == 0)
                    return 0;

                return CommandsMistyped / (decimal)CommandsTotal;
            }
        }

        public ulong DiscourseRequestsSent { get; set; }
    }
}