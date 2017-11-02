using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Security.Authentication;
using RedditSharp;
using RedditSharp.Things;
using Tweetinvi;

namespace csgo_twitter
{
    class Session
    {
        private Reddit _reddit;
        private Settings _settings;
        private Subreddit _subreddit;
        private List<Queue> _queuedPosts = new List<Queue>();
        private List<string> _checkedPosts = new List<string>();
        private BackgroundWorker _postBgw = new BackgroundWorker();
        private BackgroundWorker _streamBgw = new BackgroundWorker();
        private BackgroundWorker _queueBgw = new BackgroundWorker();

        public Session(Settings settings)
        {
            _settings = settings;
            
            _postBgw.WorkerSupportsCancellation = true;
            _postBgw.RunWorkerCompleted += _postBgw_RunWorkerCompleted;
            _postBgw.DoWork += _postBgw_DoWork;
            
            _streamBgw.WorkerSupportsCancellation = true;
            _streamBgw.RunWorkerCompleted += _streamBgw_RunWorkerCompleted;
            _streamBgw.DoWork += _streamBgw_DoWork;

            _queueBgw.WorkerSupportsCancellation = true;
            _queueBgw.RunWorkerCompleted += _queueBgw_RunWorkerCompleted;
            _queueBgw.DoWork += _queueBgw_DoWork;
        }

        public bool Run()
        {
            try
            {
                /*Reddit initializing*/
                var webAgent = new BotWebAgent(_settings.RedditSettings.Username, _settings.RedditSettings.Password, _settings.RedditSettings.ClientID, _settings.RedditSettings.ClientSecret, Const.URL_REDIRECT);
                _reddit = new Reddit(webAgent, true);
                _subreddit = _reddit.GetSubreddit(Const.SUBREDDIT);

                /*Twitter initializing*/
                Auth.SetUserCredentials(_settings.TwitterSettings.ConsumerKey, _settings.TwitterSettings.ConsumerSecret, _settings.TwitterSettings.AccessToken, _settings.TwitterSettings.AccessTokenSecret);
            }
            catch (AuthenticationException ex)
            {
                Console.WriteLine($"Run error: {ex.Message}");
                return false;
            }

            if (_reddit.User != null && _subreddit != null)
            {
                _streamBgw.RunWorkerAsync();
                _postBgw.RunWorkerAsync();
                _queueBgw.RunWorkerAsync();
                return true;
            }

            return false;
        }

        public void Kill()
        {
            _streamBgw.CancelAsync();
            _postBgw.CancelAsync();
            _queueBgw.CancelAsync();
        }

        public bool IsRunning()
        {
            return _streamBgw.IsBusy && _postBgw.IsBusy && _queueBgw.IsBusy;
        }

        private void _postBgw_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.ReadKey();
            while (!_postBgw.CancellationPending)
            {
                var posts = _subreddit.Hot.Take(25);
                foreach (var post in posts)
                {
                    if (post.IsSelfPost)
                    {
                        string reply = GetCommentReply(post.SelfText);
                        if (!string.IsNullOrWhiteSpace(reply))
                            _queuedPosts.Add(new Queue(post.Id, post, Queue.PostType.Post, reply));
                    }
                    else
                    {
                        string reply = GetCommentReply(post.Url.ToString());
                        if (!string.IsNullOrWhiteSpace(reply))
                            _queuedPosts.Add(new Queue(post.Id, post, Queue.PostType.Post, reply));
                    }

                    _checkedPosts.Add(post.Id);
                }

                /*Sleep for minutes converted to milliseconds*/
                Thread.Sleep(_settings.MinutesBetweenChecks * 60 * 1000);
            }
        }

        private void _postBgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                Console.WriteLine($"postBgw experienced an exception:\n{e.Error.Message}");
        }

        private void _streamBgw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_streamBgw.CancellationPending)
            {
                foreach (var comment in _subreddit.CommentStream)
                {
                    /*Checks done in function*/
                    string reply = GetCommentReply(comment.Body);
                    if (!string.IsNullOrWhiteSpace(reply))
                        _queuedPosts.Add(new Queue(comment.Id, comment, Queue.PostType.Comment, reply));

                    if (_streamBgw.CancellationPending)
                        return;
                }
            }
        }

        private void _streamBgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                Console.WriteLine($"streamBgw experienced an exception:\n{e.Error.Message}");
        }

        private void _queueBgw_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < _queuedPosts.Count; i++)
            {
                var queue = _queuedPosts[i];
                
            }
        }

        private void _queueBgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                Console.WriteLine($"queueBgw experienced an exception:\n{e.Error.Message}");
        }

        private string GetCommentReply(string comment)
        {
            var formattedTweets = new List<string>();
            foreach (var id in Utils.GetTweetIds(comment))
            {
                var tweet = Tweet.GetTweet(id);
                if (tweet == null)
                    continue;

                formattedTweets.Add(Utils.GetFormattedPost(
                    tweet.CreatedBy.Name,
                    tweet.CreatedBy.ScreenName,
                    tweet.Url,
                    tweet.CreatedBy.Url,
                    tweet.Text,
                    tweet.CreatedAt));
            }

            return Utils.AppendCommentExtras(string.Join("\n\n--\n\n", formattedTweets));
        }
    }
}
