﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Runtime.InteropServices;
using RedditSharp;
using RedditSharp.Things;
using Tweetinvi;
using Newtonsoft.Json;

namespace csgo_twitter
{
    class Session
    {
        private Reddit _reddit;
        private InfoHolder _info;
        private Settings _settings;
        private Subreddit _subreddit;

        private List<Queue> _queuedPosts = new List<Queue>();
        private List<PostHolder> _checkedPosts = new List<PostHolder>();

        private BackgroundWorker _postBgw = new BackgroundWorker();
        private BackgroundWorker _queueBgw = new BackgroundWorker();

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        private static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private bool Handler(CtrlType sig)
        {
            Console.WriteLine("Exiting...");
            Kill();

            return true;
        }

        public Session(Settings settings)
        {
            _settings = settings;
            _info = new InfoHolder();
            
            _postBgw.WorkerSupportsCancellation = true;
            _postBgw.RunWorkerCompleted += _postBgw_RunWorkerCompleted;
            _postBgw.DoWork += _postBgw_DoWork;

            _queueBgw.WorkerSupportsCancellation = true;
            _queueBgw.RunWorkerCompleted += _queueBgw_RunWorkerCompleted;
            _queueBgw.DoWork += _queueBgw_DoWork;
        }

        public bool Run()
        {
            /*Read checked posts*/
            if (File.Exists(Const.LOG_PATH))
            {
                _checkedPosts = JsonConvert.DeserializeObject<List<PostHolder>>(File.ReadAllText(Const.LOG_PATH));
                _info.CheckedSize = _checkedPosts.Count();
            }

            try
            {
                /*Reddit initializing*/
                var webAgent = new BotWebAgent(_settings.RedditSettings.Username, _settings.RedditSettings.Password, _settings.RedditSettings.ClientID, _settings.RedditSettings.ClientSecret, Const.URL_REDIRECT);
                _reddit = new Reddit(webAgent, true);
                _subreddit = _reddit.GetSubreddit(_settings.Subreddit);

                /*Twitter initializing*/
                Auth.SetUserCredentials(_settings.TwitterSettings.ConsumerKey, _settings.TwitterSettings.ConsumerSecret, _settings.TwitterSettings.AccessToken, _settings.TwitterSettings.AccessTokenSecret);
            }
            catch (AuthenticationException ex)
            {
                Console.WriteLine($"Run error: {ex.Message}");
                return false;
            }

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            if (_reddit.User != null && _subreddit != null)
            {
                _postBgw.RunWorkerAsync();
                _queueBgw.RunWorkerAsync();
                UpdateConsoleTitle();
                return true;
            }

            return false;
        }

        public void Kill()
        {
            _postBgw.CancelAsync();
            _queueBgw.CancelAsync();
            File.WriteAllText(Const.LOG_PATH, JsonConvert.SerializeObject(_checkedPosts, Formatting.Indented));
        }

        public bool IsRunning()
        {
            return _postBgw.IsBusy && _queueBgw.IsBusy;
        }

        private void AddToQueue(Queue item)
        {
            if (!_checkedPosts.Any(o => o.Id == item.Id))
            {
                _checkedPosts.Add(new PostHolder(item.Id));
                _info.CheckedSize = _checkedPosts.Count();
                _queuedPosts.Add(item);
            }
        }

        private void UpdateConsoleTitle()
        {
            Console.Title = Utils.GetConsoleTitle(true, _info);
        }

        private void _postBgw_DoWork(object sender, DoWorkEventArgs e)
        {
            new Thread(() =>
            {
                try
                {
                    Thread.CurrentThread.IsBackground = true;
                    foreach (var comment in _subreddit.CommentStream)
                    {
                        if (comment.AuthorName == _settings.RedditSettings.Username)
                            continue;

                        string reply = GetCommentReply(comment.Body);
                        if (!string.IsNullOrWhiteSpace(reply))
                            AddToQueue(new Queue(comment.Id, comment, Queue.PostType.Comment, reply));

                        if (_postBgw.CancellationPending)
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Stream broke: {ex.ToString()}");
                }

            }).Start();

            while (!_postBgw.CancellationPending)
            {
                foreach (var post in _subreddit.New.Take(25))
                {
                    if (post.AuthorName == _settings.RedditSettings.Username)
                        continue;

                    if (post.IsSelfPost)
                    {
                        string reply = GetCommentReply(post.SelfText);
                        if (!string.IsNullOrWhiteSpace(reply))
                            AddToQueue(new Queue(post.Id, post, Queue.PostType.Post, reply));
                    }
                    else
                    {
                        string reply = GetCommentReply(post.Url.ToString());
                        if (!string.IsNullOrWhiteSpace(reply))
                            AddToQueue(new Queue(post.Id, post, Queue.PostType.Post, reply));
                    }
                }

                /*Sleep for minutes converted to milliseconds*/
                Thread.Sleep(_settings.MinutesBetweenChecks * 60 * 1000);
            }
        }

        private void _postBgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                Console.WriteLine($"postBgw experienced an exception:\n{e.Error.Message}");

            Console.WriteLine("postBgw exited");
        }

        private void _queueBgw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_queueBgw.CancellationPending)
            {
                var toRemove = new List<string>();
                for (int i = 0; i < _queuedPosts.Count; i++)
                {
                    var queue = _queuedPosts[i];
                    try
                    {
                        switch (queue.Type)
                        {
                            case Queue.PostType.Comment:
                                ((Comment)queue.Post).Reply(queue.Reply);
                                Console.WriteLine($"Replied to comment containing tweet. ID: {queue.Id}");
                                break;

                            case Queue.PostType.Post:
                                ((Post)queue.Post).Comment(queue.Reply);
                                Console.WriteLine($"Replied to post containing tweet. ID: {queue.Id}");
                                break;
                        }

                        toRemove.Add(queue.Id);
                        _checkedPosts.Where(o => o.Id == queue.Id).ToList().ForEach(o => o.Posted = true);
                        Thread.Sleep(_settings.MinutesBetweenReplies * 60 * 1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unable to comment!");
                    }
                }

                if (toRemove.Count() > 0)
                    _queuedPosts = _queuedPosts.Where(o => !toRemove.Contains(o.Id)).ToList();

                _info.QueueSize = _queuedPosts.Count();
                UpdateConsoleTitle();
                Thread.Sleep(5000);
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
                    "https://twitter.com/" + tweet.CreatedBy.ScreenName,
                    tweet.Text,
                    tweet.CreatedAt));
            }

            return Utils.AppendCommentExtras(string.Join("\n\n", formattedTweets));
        }
    }
}
