using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csgo_twitter
{
    class Queue
    {
        public string Id { get; set; }

        public object Post { get; set; }

        public PostType Type { get; set; }

        public string Reply { get; set; }

        public enum PostType
        {
            Comment,
            Post
        }

        public Queue(string id, object post, PostType type, string reply)
        {
            Id = id;
            Post = post;
            Type = type;
            Reply = reply;
        }
    }
}
