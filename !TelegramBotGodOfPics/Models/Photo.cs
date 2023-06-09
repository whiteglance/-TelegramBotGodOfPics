using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _TelegramBotGodOfPics.Models
{
    public class Urls
    {
        public string Regular { get; set; }
    }

    public class Photo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public Urls Urls { get; set; }
    }
}
