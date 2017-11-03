using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace csgo_twitter
{
    class Utils
    {
        public static string GetFormattedPost(string displayName, string username, string link, string userlink, string content, DateTime createdAt)
        {
            /* Formatted post example tweet
             * (DaZeD thought competitive CS was as easy as being an armchair analyst LUL)
             * 
             *  Sam M‏ ([@GODaZeD](https://twitter.com/GODaZeD/status/925923757742473216))
             *
             *  > I'm gonna step down from playing competitively, i told the guys to get a new player for me. I just dont have the motivation or drive 1/2
             *
             *  --
             *
             *  ^^[Source](https://github.com/Ezzpify/csgo-twitter) ^^| ^^[Issues](https://github.com/Ezzpify/csgo-twitter/issues) ^^| [^^Link ^^to ^^tweet](https://twitter.com/GODaZeD/status/925923757742473216)
             *
             */

            return string.Format("{0} ([@{1}]({2}))\n\n^^{3}\n\n> {4}\n\n[^^Link ^^to ^^tweet]({5})",
                displayName,
                username,
                userlink,
                createdAt.ToLongDateString().Replace(" ", " ^^"),
                Regex.Replace(content, @"\s+", " "),
                link);
        }

        public static string AppendCommentExtras(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return string.Empty;

            string extras = string.Format("^[Source]({0}) ^| ^[Issues]({1})", 
                Const.URL_SOURCE, 
                Const.URL_ISSUES);

            return $"{comment}\n\n--\n\n{extras}";
        }

        public static string GetConsoleTitle(bool running, InfoHolder info)
        {
            return $"{Const.CONSOLE_TITLE} {(running ? "[RUNNING]" : "[OFFLINE]")} [Queue: {info.QueueSize} | Checked: {info.CheckedSize}]";
        }

        public static List<long> GetTweetIds(string comment)
        {
            /*Prepare yourself*/
            /*TODO - Improve regex to catch twitter.com & status instead of using linq ayy*/
            var linkRegex = new Regex(@"(https?)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return linkRegex.Matches(comment)
                .Cast<Match>()
                .Where(o => o.Value.Contains("twitter.com") && o.Value.Contains("status"))
                .Select(o => o.Value.Substring(o.Value.LastIndexOf('/') + 1))
                .Where(o => !string.IsNullOrWhiteSpace(o) && OnlyNumbers(o))
                .Select(long.Parse)
                .ToList();
        }

        public static bool OnlyNumbers(string str)
        {
            return new Regex("^[0-9]+$").IsMatch(str);
        }
    }
}
