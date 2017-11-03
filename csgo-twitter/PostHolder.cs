using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csgo_twitter
{
    class PostHolder
    {
        public string Id { get; set; }

        public bool Posted { get; set; }

        public PostHolder(string id)
        {
            Id = id;
        }
    }
}
