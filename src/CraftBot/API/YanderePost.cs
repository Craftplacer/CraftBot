using System;
using Newtonsoft.Json;

namespace CraftBot.API
{
    public class YanderePost
    {
        public int Id;

        [JsonIgnore]
        public string[] Tags;

        [JsonProperty("tags")]
        [Obsolete(null, true)]
        public string Json1
        {
            set => Tags = value.Split(' ');
        }

        public string Source;
        public int Score;
        
        [JsonProperty("md5")]
        public string Md5;
        
        [JsonProperty("file_ext")]
        public string FileExt;
        
        [JsonProperty("file_url")]
        public string FileUrl;
        
        [JsonProperty("rating")]
        public string Rating;
    }
}