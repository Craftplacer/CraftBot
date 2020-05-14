using System;
using System.IO;
using Newtonsoft.Json;

namespace CraftBot
{
    public static class Helpers
    {
        /// <summary>
        /// Retrieves a JSON object from disk.
        /// </summary>
        /// <param name="name">The file name without extension relative to the working directory</param>
        /// <param name="default">Default value when the JSON file doesn't exist</param>
        /// <param name="silent">Determines if the loading process should be logged</param>
        public static T GetJson<T>(string name, T @default = default, bool silent = false)
        {
            var fileName = name + ".json";

            if (!silent)
                Logger.Info($"Loading JSON... ({fileName})", "JSON");

            if (!File.Exists(fileName))
                SaveJson(name, @default, true);

            var json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Saves a JSON object to disk.
        /// </summary>
        public static void SaveJson(string name, object value, bool silent = false)
        {
            var fileName = name + ".json";

            if (!silent)
                Logger.Info($"Saving JSON... ({fileName})", "JSON");

            var json = JsonConvert.SerializeObject(value);
            File.WriteAllText(fileName, json);
        }


        public static TimeSpan GetUptime() => DateTime.Now - Program.Statistics.CurrentStartTime;
    }
}