using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProfileEditor
{
    public class Notification
    {
        public static readonly string DefaultIcon = "CHAR_DEFAULT";
        public static readonly string DefaultTitle = "";
        public static readonly string DefaultSubtitle = "";
        public static readonly string DefaultMessage = "";
        public static readonly int DefaultDelay = 0;
        public static readonly bool DefaultSound = true;

        public string Icon { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Message { get; set; }
        public int Delay { get; set; }
        public bool Sound { get; set; }
        public int EndTimer { get; set; }

        public Notification()
        {
            Icon = DefaultIcon;
            Title = DefaultTitle;
            Subtitle = DefaultSubtitle;
            Message = DefaultMessage;
            Delay = DefaultDelay;
            Sound = DefaultSound;
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
