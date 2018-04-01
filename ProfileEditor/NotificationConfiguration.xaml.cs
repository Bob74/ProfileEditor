using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace ProfileEditor
{
    /// <summary>
    /// Logique d'interaction pour NotificationConfiguration.xaml
    /// </summary>
    public partial class NotificationConfiguration : Window
    {
        NotificationPreviewWindow ChildWindow;
        List<PhoneContact> PhoneContactCollection = new List<PhoneContact>();
        Notification Notification;
        bool NotificationWasNull = false;

        public NotificationConfiguration(ref Notification notif)
        {
            InitializeComponent();

            if (notif == null)
            {
                NotificationWasNull = true;
                notif = new Notification();
            }

            ChildWindow = new NotificationPreviewWindow(notif);
            object content = ChildWindow.Content;
            ChildWindow.Content = null;

            ChildGrid.Children.Add(content as UIElement);

            Notification = notif;

        }
        private void Window_Closed(object sender, EventArgs e)
        {
            ChildWindow.Close();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PhoneContactCollection.AddRange(MainWindow.GetPhoneContactCollection);
            ComboBoxIcons.ItemsSource = PhoneContactCollection;
            ComboBoxIcons.SelectedIndex = PhoneContactCollection.FindIndex(x => string.Compare(x.Name, Notification.Icon, true) == 0);

            TextBoxTitle.Text = Notification.Title;
            TextBoxSubtitle.Text = Notification.Subtitle;
            TextBoxMessage.Text = Notification.Message;
            CheckBoxSound.IsChecked = Notification.Sound;
            IntegerUpDownDelay.Value = Notification.Delay;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxMessage.Text != "")
            {
                Notification.Icon = TextBoxIcon.Text;
                Notification.Title = TextBoxTitle.Text;
                Notification.Subtitle = TextBoxSubtitle.Text;
                Notification.Message = TextBoxMessage.Text;
                Notification.Sound = CheckBoxSound.IsChecked ?? false;
                Notification.Delay = IntegerUpDownDelay.Value ?? 0;

                DialogResult = true;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("You must enter at least the message.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!NotificationWasNull)
                DialogResult = true;
            else
                Close();
        }
        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show("Do you want to remove the notification?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                Close();
        }

        private void ComboBoxIcons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string icon = ((PhoneContact)e.AddedItems[0]).Name;
            ChildWindow.SetNotificationIcon(icon);
            TextBoxIcon.Text = icon;
        }

        private void TextBoxTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChildWindow.SetNotificationTitle(TextBoxTitle.Text);
        }

        private void TextBoxSubtitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChildWindow.SetNotificationSubtitle(TextBoxSubtitle.Text);
        }

        private void TextBoxMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            ChildWindow.SetNotificationMessage(TextBoxMessage.Text);
        }

        private void IntegerUpDownDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            string milisec = "millisecond";
            string sec = "second";

            float seconds = IntegerUpDownDelay.Value != null ? (float)IntegerUpDownDelay.Value / 1000 : 0;

            if (seconds > 0f) milisec += "s";
            if (seconds > 1.0f) sec += "s";

            if (TextBlockDelay != null)
                TextBlockDelay.Text = milisec + " (" + seconds + " " + sec + ")";
        }
    }
}
