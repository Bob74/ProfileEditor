using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Logique d'interaction pour MenuItemConfiguration.xaml
    /// </summary>
    public partial class MenuItemConfiguration : Window
    {
        NativeUIMenuItem Item;

        NotificationConfiguration NotificationConfiguration;
        NotificationPreviewWindow NotificationPreviewWindow;
        Notification Notification;

        public MenuItemConfiguration(ref NativeUIMenuItem item)
        {
            InitializeComponent();

            if (item == null)
            {
                item = new NativeUIMenuItem();
            }

            Item = item;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxItemText.Text = Item.Text;

            if (Item.Sound != null)
            {
                TextBoxSound.Text = Item.Sound.File;
                SliderSoundVolume.Value = Item.Sound.Volume;
            }
            if (Item.Notification != null) Notification = Item.Notification;

            if (Item.Keys.Count > 0)
            {
                foreach (string key in Item.Keys)
                    TextBoxShortcut.Text += key + "\r\n";
                TextBoxShortcut.Text = TextBoxShortcut.Text.Remove(TextBoxShortcut.Text.Length - 2, 2);
            }

            TextBoxItemText.Focus();
            TextBoxItemText.SelectAll();
        }

        // Sound
        private void ButtonSoundBrowse_Click(object sender, RoutedEventArgs e)
        {
            string filter = "Wave files|";
            foreach (string ext in MainWindow.SoundFileExtensions)
            {
                filter += "*" + ext + ";";
            }

            OpenFileDialog dial = new OpenFileDialog() { InitialDirectory = MainWindow.BaseDir, Filter = filter };
            if (dial.ShowDialog() == true)
                TextBoxSound.Text = dial.FileName;
        }
        private void SliderSoundVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextBlockSoundVolume != null)
                TextBlockSoundVolume.Text = ((int)SliderSoundVolume.Value).ToString() + "%";
        }

        // Notification Hover
        private void NotificationPreview_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Notification != null)
            {
                NotificationPreviewWindow = new NotificationPreviewWindow(Notification);
                NotificationPreviewWindow.Show();
                NotificationPreviewWindow.Left = System.Windows.Forms.Cursor.Position.X + 10;
                NotificationPreviewWindow.Top = System.Windows.Forms.Cursor.Position.Y - NotificationPreviewWindow.Height - 10;
            }
        }
        private void NotificationPreview_MouseLeave(object sender, MouseEventArgs e)
        {
            if (NotificationPreviewWindow != null)
            {
                NotificationPreviewWindow.Close();
                NotificationPreviewWindow = null;
            }
        }
        private void NotificationPreview_MouseMove(object sender, MouseEventArgs e)
        {
            if (NotificationPreviewWindow != null)
            {
                NotificationPreviewWindow.Left = System.Windows.Forms.Cursor.Position.X + 10;
                NotificationPreviewWindow.Top = System.Windows.Forms.Cursor.Position.Y - NotificationPreviewWindow.Height - 10;
            }
        }

        // Set notification
        private void ButtonNotificationSet_Click(object sender, RoutedEventArgs e)
        {
            NotificationConfiguration = new NotificationConfiguration(ref Notification) { Owner = this };
            NotificationConfiguration.ShowDialog();

            if (!NotificationConfiguration.DialogResult.HasValue || !NotificationConfiguration.DialogResult.Value)
                Notification = null;

            NotificationConfiguration = null;
        }

        // Ok
        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxItemText.Text != "" && TextBoxShortcut.Text != "")
            {
                // Text
                Item.Text = TextBoxItemText.Text;

                // Sound
                if (File.Exists(TextBoxSound.Text))
                    Item.Sound = new MenuSound() { File = TextBoxSound.Text, Volume = (int)SliderSoundVolume.Value };
                else
                    Item.Sound = null;

                // Keys
                Item.Keys.Clear();
                foreach (string key in TextBoxShortcut.Text.Replace("\r", "").Split('\n'))
                {
                    Item.Keys.Add(key);
                }

                // Notification
                Item.Notification = Notification;

                DialogResult = true;
            }
            else
            {
                MessageBox.Show("You must at least fill the fields tagged with a red asterisk.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Cancel
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Item = null;
            Close();
        }

    }
}
