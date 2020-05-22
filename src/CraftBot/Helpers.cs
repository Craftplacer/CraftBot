using System;
using System.IO;
using System.Runtime.InteropServices;
using CraftBot.Localization;
using Microsoft.Win32;
using Newtonsoft.Json;
using Sentry;

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

        /// <summary>
        /// Logs the provided exception including the detail given and submits it to Sentry.
        /// </summary>
        public static void ReportException(Exception exception, string module, string detail = null)
        {
            if (!string.IsNullOrWhiteSpace(detail))
                Logger.Error(detail, module);

            Logger.Error(exception, module);

            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("module", module);
                scope.SetExtra("detail", detail);

                SentrySdk.CaptureException(exception);
            });
        }

        public static void SaveLanguage(Language language)
        {
            var json = language.ToJson();
            var path = Path.Combine("lang", language.Code + ".json");

            File.WriteAllText(path, json);
        }

        public static string GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                if (key != null)
                    return (string)key.GetValue("ProductName");
            }

            return RuntimeInformation.OSDescription;
        }
    }
}