using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProfileEditor
{
    /// <summary>
    /// Logique d'interaction pour NotificationPreviewWindow.xaml
    /// </summary>
    public partial class NotificationPreviewWindow : Window
    {
        public NotificationPreviewWindow(Notification notif)
        {
            InitializeComponent();
            ImageIcon.Source = new BitmapImage(new Uri("pack://application:,,,/ProfileEditor;component/resources/" + notif.Icon + ".png"));
            LabelTitle.Content = notif.Title;
            LabelSubtitle.Content = notif.Subtitle;
            LabelBody.Content = new TextBlock() { Text = notif.Message, TextWrapping = TextWrapping.Wrap };
        }

        public void SetNotificationIcon(string text)
        {
            ImageIcon.Source = new BitmapImage(new Uri("pack://application:,,,/ProfileEditor;component/resources/" + text + ".png"));
        }
        public void SetNotificationTitle(string text) { LabelTitle.Content = text; }
        public void SetNotificationSubtitle(string text) { LabelSubtitle.Content = text; }
        public void SetNotificationMessage(string text)
        {
            LabelBody.Content = new TextBlock() { Text = text, TextWrapping = TextWrapping.Wrap };
        }

    }
}
