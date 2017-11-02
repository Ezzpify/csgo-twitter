using Newtonsoft.Json;
using System.IO;

namespace csgo_twitter
{
    public class RedditSettings
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string ClientID { get; set; } = string.Empty;
    }

    public class TwitterSettings
    {
        public string ConsumerKey { get; set; } = string.Empty;

        public string ConsumerSecret { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public string AccessTokenSecret { get; set; } = string.Empty;
    }

    class Settings
    {
        public RedditSettings RedditSettings = new RedditSettings();

        public TwitterSettings TwitterSettings = new TwitterSettings();

        public int MinutesBetweenChecks { get; set; } = 5;

        public bool LoadSettings(string filePath)
        {
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    var settings = JsonConvert.DeserializeObject<Settings>(fileContent);
                    RedditSettings = settings.RedditSettings;
                    TwitterSettings = settings.TwitterSettings;
                    MinutesBetweenChecks = settings.MinutesBetweenChecks;

                    return true;
                }
            }

            return false;
        }

        public bool PrintSettings(string filePath)
        {
            string settingsContent = JsonConvert.SerializeObject(this, Formatting.Indented);
            if (!string.IsNullOrWhiteSpace(settingsContent))
            {
                File.WriteAllText(filePath, settingsContent);
                return true;
            }

            return false;
        }
    }
}
