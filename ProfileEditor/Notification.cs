using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProfileEditor
{
    public class Notification
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Message { get; set; }
        public int Delay { get; set; }
        public bool Sound { get; set; }
        public int EndTimer { get; set; }

        public Notification()
        {
            Icon = "CHAR_DEFAULT";
            Title = "";
            Subtitle = "";
            Message = "";
            Delay = 0;
            Sound = true;
        }

        public Notification(Notification notif)
        {
            Icon = notif.Icon;
            Title = notif.Title;
            Subtitle = notif.Subtitle;
            Message = notif.Message;
            Delay = notif.Delay;
            Sound = notif.Sound;
        }
    }
}
