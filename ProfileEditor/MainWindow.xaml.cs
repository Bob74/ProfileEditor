using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

/*
    TODO:
    -----
    - Check la version pour Update
    - Check version de NMS pour Update
    - Raccourcis à envoyer pour menu items


    X Son pour menu items
    X Notifications à ajouter pour le téléphone normal + le menu
        > Comment afficher/modifier la notification d'un item simplement ?
    X Clic droit pour ajouter des éléments de menu (+ raccourcis ?)
    X Ajouter les nouveaux icônes
*/

namespace ProfileEditor
{
    public class PhoneContact
    {
        public string Icon { get; set; }
        public string Name { get; set; }
    }

    public class NativeUIItem
    {
        public virtual string Text { get; set; }
        public virtual string DisplayName { get => Text; }
        public virtual NativeUIMenuSubmenu ParentMenu { get; set; } = null;
    }
    public class NativeUIBanner : NativeUIItem
    {
        public override string DisplayName { get => "[" + Text + "]"; }

        public string FilePath { get; set; }
        public string FileName { get => GetFileName(); }
        private string GetFileName()
        {
            return new FileInfo(FilePath).Name;
        }
    }
    public class NativeUIMenuSubmenu : NativeUIItem
    {
        public override string DisplayName { get => Text + " [" + Items.Count + "]"; }
        public List<NativeUIItem> Items { get; set; }

        public NativeUIMenuSubmenu(NativeUIMenuSubmenu parent = null)
        {
            ParentMenu = parent;
        }
    }
    public class NativeUIMenuSubtitle : NativeUIItem { }
    public class NativeUIMenuItem : NativeUIItem
    {
        public override string DisplayName { get => GetDisplayName(); }
        public List<string> Keys { get; set; }
        public Notification Notification { get; set; }
        public MenuSound Sound { get; set; }

        public NativeUIMenuItem(NativeUIMenuSubmenu parent = null)
        {
            ParentMenu = parent;
        }

        private string GetKeySequence()
        {
            string sequence = "";
            foreach (string key in Keys)
                sequence += "+" + key;

            return sequence.Remove(0, 1);
        }

        private string GetDisplayName()
        {
            string name = Text;

            if (Keys != null)
            {
                if (Keys.Count > 0)
                    name += " (" + GetKeySequence() + ")";
            }
            if (Notification != null)
            {
                name += " (Notification)";
            }
            if (Sound != null)
            {
                name += " (Sound)";
            }

            return name;
        }
    }
    public class NativeUIBack : NativeUIItem
    {
        public NativeUIBack(NativeUIMenuSubmenu parent = null)
        {
            Text = "Back";
            ParentMenu = parent;
        }
    }
    public class NativeUIReturnMenu : NativeUIItem
    {
        public NativeUIReturnMenu()
        {
            Text = "Return to main menu";
            ParentMenu = null;
        }
    }

    public static class TreeViewCommands
    {
        public static readonly RoutedUICommand AddItem = new RoutedUICommand
        (
            "Add a new item",
            "AddItem",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.A, ModifierKeys.Control)
            }
        );
        public static readonly RoutedUICommand AddSubmenu = new RoutedUICommand
        (
            "Add a submenu",
            "AddSubmenu",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.S, ModifierKeys.Control)
            }
        );
        public static readonly RoutedUICommand RemoveItem = new RoutedUICommand
        (
            "Remove selection",
            "RemoveItem",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.Delete)
            }
        );

        public static readonly RoutedUICommand ItemAddItem = new RoutedUICommand
        (
            "Add a new item",
            "ItemAddItem",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.A, ModifierKeys.Control)
            }
        );
        public static readonly RoutedUICommand ItemAddSubmenu = new RoutedUICommand
        (
            "Add a submenu",
            "ItemAddSubmenu",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.S, ModifierKeys.Control)
            }
        );

    }

    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string BaseDir = Tools.GetGamePath()?.ToString() ?? AppDomain.CurrentDomain.BaseDirectory;
        public readonly List<string> BannerFileExtensions = new List<string> { ".png", ".jpeg", ".jpg", ".bmp" };
        public readonly List<string> SoundFileExtensions = new List<string> { ".wav" };

        public List<PhoneContact> GetPhoneContactCollection { get => PhoneContactCollection; }
        List<PhoneContact> PhoneContactCollection = new List<PhoneContact>();

        List<NativeUIItem> Menu = new List<NativeUIItem>();
        List<NativeUIItem> PreviewCurrentSubmenu = new List<NativeUIItem>();
        NativeUIMenuSubmenu RootMenu;

        Notification PhoneNotification;
        NotificationConfiguration NotificationConfiguration;
        NotificationPreviewWindow NotificationPreviewWindow;
        MenuSoundConfiguration MenuSoundConfiguration;


        public MainWindow()
        {
            InitializeComponent();

            string[] resources = GetResourceNames();

            foreach (string res in resources)
            {
                if (res.StartsWith("resources/char_") ||
                    res.StartsWith("resources/dia_") ||
                    res.StartsWith("resources/hc_n_") ||
                    res.StartsWith("resources/hush_") ||
                    res.StartsWith("resources/web_"))
                {
                    string name = res.Replace("resources/", "");
                    name = name.Remove(name.Length - 4);
                    PhoneContactCollection.Add(new PhoneContact { Icon = res, Name = name.ToUpper() });
                }
            }
            PhoneContactCollection.Sort((x, y) => string.Compare(x.Name, y.Name));
            ComboBoxPhoneContactIcons.ItemsSource = PhoneContactCollection;
            ComboBoxPhoneContactIcons.SelectedIndex = PhoneContactCollection.FindIndex(x => x.Name == "CHAR_DEFAULT");


            RootMenu = new NativeUIMenuSubmenu()
            {
                Text = "Menu",
                Items = new List<NativeUIItem>()
            };

            Menu.Add(RootMenu);

            TreeViewMenu.ItemsSource = Menu;
            ItemsControlPreviewMenu.ItemsSource = RootMenu.Items;
        }

        public static string[] GetResourceNames()
        {
            var asm = Assembly.GetEntryAssembly();
            string resName = asm.GetName().Name + ".g.resources";
            using (var stream = asm.GetManifestResourceStream(resName))
            using (var reader = new ResourceReader(stream))
            {
                return reader.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToArray();
            }
        }


        /*
                PHONE
        */
        // Contact Name
        private void TextBoxContactName_TextChanged(object sender, TextChangedEventArgs e)
        {
            PreviewContactName.Text = TextBoxContactName.Text;
        }

        // Contact Name Bold
        private void CheckBoxContactNameBold_Checked(object sender, RoutedEventArgs e)
        {
            PreviewContactName.FontWeight = FontWeights.Bold;
        }
        private void CheckBoxContactNameBold_Unchecked(object sender, RoutedEventArgs e)
        {
            PreviewContactName.FontWeight = FontWeights.Normal;
        }

        // Contact Icon
        private void ComboBoxPhoneContactIcons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PreviewContactIcon.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/ProfileEditor;component/" + ((PhoneContact)e.AddedItems[0]).Icon)));
            TextBoxPhoneContactIcon.Text = ((PhoneContact)e.AddedItems[0]).Name;
        }
        private void TextBoxPhoneContactIcon_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!TextBoxPhoneContactIcon.Text.StartsWith("CHAR_", StringComparison.InvariantCultureIgnoreCase))
            {
                CheckBoxContactNameBold.IsChecked = true;
                CheckBoxContactNameBold.IsEnabled = false;
            }
            else
            {
                CheckBoxContactNameBold.IsChecked = false;
                CheckBoxContactNameBold.IsEnabled = true;
            }
        }

        // Dialing Timeout
        private void IntegerUpDownPhoneDialing_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            string milisec = "millisecond";
            string sec = "second";

            float seconds = IntegerUpDownPhoneDialing.Value != null ? (float)IntegerUpDownPhoneDialing.Value / 1000 : 0;

            if (seconds > 0f) milisec += "s";
            if (seconds > 1.0f) sec += "s";

            if (TextBlockPhoneDialing != null)
                TextBlockPhoneDialing.Text = milisec + " (" + seconds + " " + sec + ").";
        }

        // Sound File
        private void ButtonPhoneSoundBrowse_Click(object sender, RoutedEventArgs e)
        {
            string filter = "Wave files|";
            foreach (string ext in SoundFileExtensions)
            {
                filter += "*" + ext + ";";
            }

            OpenFileDialog dial = new OpenFileDialog() { InitialDirectory = BaseDir, Filter = filter };
            if (dial.ShowDialog() == true)
                TextBoxPhoneSound.Text = dial.FileName;
        }

        // Sound Volume
        private void SliderSoundVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextBlockSoundVolume != null)
                TextBlockSoundVolume.Text = ((int)SliderSoundVolume.Value).ToString() + "%";
        }


        /*
                MENU
        */
        private void CheckBoxMenu_Checked(object sender, RoutedEventArgs e)
        {
            GridMenu.IsEnabled = true;
            TextBoxPhoneShortcut.IsEnabled = false;
            ItemsControlPreviewMenu.Visibility = Visibility.Visible;
        }

        private void CheckBoxMenu_Unchecked(object sender, RoutedEventArgs e)
        {
            GridMenu.IsEnabled = false;
            TextBoxPhoneShortcut.IsEnabled = true;
            ItemsControlPreviewMenu.Visibility = Visibility.Hidden;
        }

        private void ButtonMenuBannerBrowse_Click(object sender, RoutedEventArgs e)
        {
            string filter = "Image files|";
            foreach (string ext in BannerFileExtensions)
            {
                filter += "*" + ext + ";";
            }

            OpenFileDialog dial = new OpenFileDialog() { InitialDirectory = BaseDir, Filter = filter };
            if (dial.ShowDialog() == true)
                TextBoxMenuBanner.Text = dial.FileName;
        }
        
        private void TextBoxMenuBanner_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (File.Exists(TextBoxMenuBanner.Text) && BannerFileExtensions.Any(TextBoxMenuBanner.Text.EndsWith))
            {
                RootMenu.Items.Insert(0, new NativeUIBanner() { Text = "Banner", FilePath = TextBoxMenuBanner.Text });
                RefreshTreeview();
            }
            else
            {
                if (RootMenu.Items.Count > 0)
                    if (RootMenu.Items[0] is NativeUIBanner)
                    {
                        RootMenu.Items.RemoveAt(0);
                        RefreshTreeview();
                        TextBoxMenuBanner.Focus();
                    }
            }
        }

        /*
                PREVIEW PHONE
        */
        // Phone contact Hover
        private void PreviewContactGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            PreviewContactName.Foreground = Brushes.WhiteSmoke;
            PreviewContactGrid.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x4C, 0x8A, 0xD7));

            if (!(bool)CheckBoxMenu.IsChecked)
            {
                TextBlock content = new TextBlock() { TextWrapping = TextWrapping.WrapWithOverflow };

                if (TextBoxPhoneShortcut.Text != "")
                {
                    string keySequence = TextBoxPhoneShortcut.Text.Replace("\n", "+");
                    keySequence = keySequence.Replace("\r", "");
                    content.Text = "Key combination: " + keySequence;
                }
                else
                    content.Text = "No key set.";

                PreviewContactShortcut.Content = content;
                PreviewContactShortcut.Background = Brushes.Black;
            }
            else
            {
                TextBlock content = new TextBlock() { TextWrapping = TextWrapping.WrapWithOverflow, Text = "No key combination. The menu will be displayed instead." };
                PreviewContactShortcut.Content = content;
                PreviewContactShortcut.Background = Brushes.Black;
            }
        }
        private void PreviewContactGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            PreviewContactName.Foreground = Brushes.Black;
            PreviewContactGrid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            PreviewContactShortcut.Content = "";
            PreviewContactShortcut.Background = Brushes.Transparent;
        }

        // Phone notification Hover
        private void PhoneNotificationPreview_MouseEnter(object sender, MouseEventArgs e)
        {
            if (PhoneNotification != null)
            {
                NotificationPreviewWindow = new NotificationPreviewWindow(PhoneNotification);
                NotificationPreviewWindow.Show();
                NotificationPreviewWindow.Left = System.Windows.Forms.Cursor.Position.X + 10;
                NotificationPreviewWindow.Top = System.Windows.Forms.Cursor.Position.Y + 10;
            }
        }
        private void PhoneNotificationPreview_MouseLeave(object sender, MouseEventArgs e)
        {
            if (NotificationPreviewWindow != null)
            {
                NotificationPreviewWindow.Close();
                NotificationPreviewWindow = null;
            }
        }
        private void PhoneNotificationPreview_MouseMove(object sender, MouseEventArgs e)
        {
            if (NotificationPreviewWindow != null)
            {
                NotificationPreviewWindow.Left = System.Windows.Forms.Cursor.Position.X + 10;
                NotificationPreviewWindow.Top = System.Windows.Forms.Cursor.Position.Y + 10;
            }
        }
        private void ButtonPhoneNotificationSet_Click(object sender, RoutedEventArgs e)
        {
            NotificationConfiguration = new NotificationConfiguration(ref PhoneNotification) { Owner = this };
            NotificationConfiguration.ShowDialog();

            if (!NotificationConfiguration.DialogResult.HasValue || !NotificationConfiguration.DialogResult.Value)
                PhoneNotification = null;

            NotificationConfiguration = null;
        }


        /*
                PREVIEW MENU
        */
        // Click on submenu
        private void OnSubmenuClick(object sender, MouseButtonEventArgs e)
        {
            object item = ItemsControlPreviewMenu.ItemContainerGenerator.ItemFromContainer(ItemsControlPreviewMenu.ContainerFromElement((Label)sender));
            if (item != null)
            {
                if (item is NativeUIMenuSubmenu)
                {
                    NativeUIMenuSubmenu parent = ((NativeUIMenuSubmenu)item).ParentMenu;

                    PreviewCurrentSubmenu.Clear();
                    if (File.Exists(TextBoxMenuBanner.Text))
                        PreviewCurrentSubmenu.Add(new NativeUIBanner() { Text = "Banner", FilePath = TextBoxMenuBanner.Text });
                    PreviewCurrentSubmenu.AddRange(((NativeUIMenuSubmenu)item).Items);
                    PreviewCurrentSubmenu.Add(new NativeUIBack(parent));
                    if (parent != null && parent != RootMenu as NativeUIMenuSubmenu) PreviewCurrentSubmenu.Add(new NativeUIReturnMenu());

                    ItemsControlPreviewMenu.ItemsSource = PreviewCurrentSubmenu;
                    RefreshTreeview();
                }
            }
        }

        private void OnBackClick(object sender, MouseButtonEventArgs e)
        {
            object item = ItemsControlPreviewMenu.ItemContainerGenerator.ItemFromContainer(ItemsControlPreviewMenu.ContainerFromElement((Label)sender));
            if (item != null)
            {
                if (item is NativeUIBack)
                {
                    if (((NativeUIBack)item).ParentMenu != null)
                    {
                        NativeUIMenuSubmenu parent = ((NativeUIBack)item).ParentMenu;

                        PreviewCurrentSubmenu.Clear();
                        if (File.Exists(TextBoxMenuBanner.Text))
                            PreviewCurrentSubmenu.Add(new NativeUIBanner() { Text = "Banner", FilePath = TextBoxMenuBanner.Text });
                        PreviewCurrentSubmenu.AddRange(((NativeUIBack)item).ParentMenu.Items);
                        if (parent.ParentMenu != RootMenu as NativeUIMenuSubmenu) PreviewCurrentSubmenu.Add(new NativeUIBack(parent.ParentMenu));
                        if (parent.ParentMenu != null && parent.ParentMenu != RootMenu as NativeUIMenuSubmenu) PreviewCurrentSubmenu.Add(new NativeUIReturnMenu());

                        ItemsControlPreviewMenu.ItemsSource = PreviewCurrentSubmenu;
                        RefreshTreeview();
                    }
                    else
                    {
                        OnReturnMenuClick(sender, e);
                    }
                }
            }
        }

        private void OnReturnMenuClick(object sender, MouseButtonEventArgs e)
        {
            PreviewCurrentSubmenu.Clear();
            ItemsControlPreviewMenu.ItemsSource = RootMenu.Items;
            RefreshTreeview();
        }


        /*
            TreeView Submenu + Item : Context menu
        */
        private void AddItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void AddItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AddItem((NativeUIItem)TreeViewMenu.SelectedItem);
        }
        private void AddSubmenuCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void AddSubmenuCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AddSubmenu((NativeUIItem)TreeViewMenu.SelectedItem);
        }
        private void RemoveItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void RemoveItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RemoveItem((NativeUIItem)TreeViewMenu.SelectedItem);
        }

        /*
            TreeView Item : Context menu
        */
        // Set Notification
        private void MenuItemSetItemNotification_Click(object sender, RoutedEventArgs e)
        {
            ItemSetNotification((NativeUIMenuItem)TreeViewMenu.SelectedItem);
        }
        private void MenuItemSetItemSound_Click(object sender, RoutedEventArgs e)
        {
            ItemSetSound((NativeUIMenuItem)TreeViewMenu.SelectedItem);
        }

        /*
            TreeView Main : Context menu
        */
        // Clear all
        private void TreeViewMainContextClear_Click(object sender, RoutedEventArgs e)
        {
            RootMenu.Items.Clear();
            RefreshTreeview();
        }







        internal void AddItem(NativeUIItem node)
        {
            string name = "Item";
            List<string> keys = new List<string> { "SHIFT", "C" };
            NativeUIMenuSubmenu parent = GetSuitableParentFromItem(node);

            NativeUIMenuItem item = new NativeUIMenuItem(parent)
            {
                Text = name,
                Keys = keys
            };

            if (parent is NativeUIMenuSubmenu)
                parent.Items.Add(item);
            else
                RootMenu.Items.Add(item);

            RefreshTreeview();
        }

        internal void AddSubmenu(NativeUIItem node)
        {
            string name = "Submenu";
            NativeUIMenuSubmenu parent = GetSuitableParentFromItem(node);

            NativeUIMenuSubmenu submenu = new NativeUIMenuSubmenu(parent)
            {
                Text = name,
                Items = new List<NativeUIItem>()
            };

            if (parent is NativeUIMenuSubmenu)
                parent.Items.Add(submenu);
            else
                RootMenu.Items.Add(submenu);

            RefreshTreeview();
        }

        internal void RemoveItem(NativeUIItem itemToRemove)
        {
            NativeUIMenuSubmenu parent = itemToRemove.ParentMenu;

            if (itemToRemove is NativeUIBanner)
                TextBoxMenuBanner.Text = "";
            else
            {
                if (parent != null)
                    parent.Items.Remove(itemToRemove);
                else
                    RootMenu.Items.Remove(itemToRemove);
            }

            RefreshTreeview();
        }

        internal void ItemSetNotification(NativeUIMenuItem item)
        {
            Notification itemNotif = item.Notification;
            NotificationConfiguration = new NotificationConfiguration(ref itemNotif) { Owner = this };
            NotificationConfiguration.ShowDialog();

            // Choice: Remove
            if (!NotificationConfiguration.DialogResult.HasValue || !NotificationConfiguration.DialogResult.Value)
            {
                item.Notification = null;
                itemNotif = null;
            }

            // Dispose of the window
            NotificationConfiguration = null;

            // If a new notification has been accepted:
            if (itemNotif != null) item.Notification = itemNotif;

            RefreshTreeview();
        }

        internal void ItemSetSound(NativeUIMenuItem item)
        {
            MenuSound itemSound = item.Sound;
            MenuSoundConfiguration = new MenuSoundConfiguration(ref itemSound) { Owner = this };
            MenuSoundConfiguration.ShowDialog();

            if (!MenuSoundConfiguration.DialogResult.HasValue || !MenuSoundConfiguration.DialogResult.Value)
            {
                item.Sound = null;
                itemSound = null;
            }

            MenuSoundConfiguration = null;

            if (itemSound != null) item.Sound = itemSound;

            RefreshTreeview();
        }

        internal NativeUIMenuSubmenu GetSuitableParentFromItem(NativeUIItem item)
        {
            NativeUIMenuSubmenu parent = null;
            if (item != null)
            {
                if (item is NativeUIMenuSubmenu)
                    parent = item as NativeUIMenuSubmenu;
                else if (item.ParentMenu != RootMenu)
                    parent = item.ParentMenu;
            }
            return parent;
        }

        internal void RefreshTreeview()
        {
            TreeViewMenu.Items.Refresh();
            ItemsControlPreviewMenu.Items.Refresh();
            TreeViewMenu.Focus();
        }

    }
}
