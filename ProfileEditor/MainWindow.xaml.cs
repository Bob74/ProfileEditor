using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Win32;
using SharpDX.XInput;

/*
    1.0.2 (02/04/2018); - Fixed freezing when browsing a file

    1.0.1 (01/04/2018): - Fixed an issue where menu hotkey wouldn't be saved
                        - When exporting, it will display an error message on error.

    1.0.0 (01/04/2018): - Initial release
    
    TODO:
    -----
    - Afficher les touches utilisées par les mods
    - Permettre de bouger les éléments du menu en haut et en bas (Ctrl + UP)
    - Plusieurs contacts ?

    X Check la version pour Update
    X Icône pour Edit item
    X Autres icônes pour Add
    X Lire la section clef du Gamepad
    X Limiter le nombre de caractères dans les notifications
    X Ajouter une aide en dessous du Menu TreeView
    X Ne pas enregistrer les modifiers keys quand on utilise le Gamepad
    X Sauvegarder Bold
    X Supporter les Gamepads pour activer les menus :
        https://stackoverflow.com/questions/3929764/taking-input-from-a-joystick-with-c-sharp-net
    X Passer Hotkey et HotkeyModifier en "(Key)" et "(ModifierKeys)"
    X Treeview: Coller doit instancier les objets à l'instant T
    X Touches à envoyer pour menu items
    X Son pour menu items
    X Notifications à ajouter pour le téléphone normal + le menu
        > Comment afficher/modifier la notification d'un item simplement ?
    X Clic droit pour ajouter des éléments de menu (+ raccourcis ?)
    X Ajouter les nouveaux icônes
*/

namespace ProfileEditor
{
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
        public static readonly RoutedUICommand EditItem = new RoutedUICommand
        (
            "Edit properties",
            "EditItem",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.F2)
            }
        );

        public static readonly RoutedUICommand CopyItem = new RoutedUICommand
        (
            "Copy",
            "CopyItem",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.C, ModifierKeys.Control)
            }
        );
        public static readonly RoutedUICommand CutItem = new RoutedUICommand
        (
            "Cut",
            "CutItem",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.X, ModifierKeys.Control)
            }
        );
        public static readonly RoutedUICommand PasteItem = new RoutedUICommand
        (
            "Paste",
            "PasteItem",
            typeof(TreeViewCommands),
            new InputGestureCollection()
            {
                new KeyGesture(Key.V, ModifierKeys.Control)
            }
        );
    }

    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string BaseDir = Tools.GetBaseDir();
        public static readonly List<string> BannerFileExtensions = new List<string> { ".png", ".jpeg", ".jpg", ".bmp" };
        public static readonly List<string> SoundFileExtensions = new List<string> { ".wav" };
        
        public static readonly Dictionary<string, ModifierKeys> DictionaryModifierKeys = new Dictionary<string, ModifierKeys>
            {
                { "None", ModifierKeys.None },
                { "Ctrl", ModifierKeys.Control },
                { "Alt", ModifierKeys.Alt },
                { "Shift", ModifierKeys.Shift }
            };

        public static List<PhoneContact> GetPhoneContactCollection { get => PhoneContactCollection; }
        static List<PhoneContact> PhoneContactCollection = new List<PhoneContact>();

        List<NativeUIItem> Menu = new List<NativeUIItem>();
        List<NativeUIItem> PreviewCurrentSubmenu = new List<NativeUIItem>();
        NativeUIMenu RootMenu = new NativeUIMenu();

        Notification PhoneNotification = null;
        NotificationConfiguration NotificationConfiguration = null;
        NotificationPreviewWindow NotificationPreviewWindow = null;
        MenuItemConfiguration MenuItemConfiguration = null;
        MenuSubmenuConfiguration MenuSubmenuConfiguration = null;

        Thread CheckUpdates = null;
        Thread GamePadThread = null;

        NativeUIItem ClipBoardItem;
        bool IsClipBoardItemCut = false;

        public MainWindow()
        {
            InitializeWindow();
        }
        public MainWindow(string profilePath, XmlPhone xmlphone, XmlMenu xmlmenu)
        {
            InitializeWindow();

            if (xmlphone != null)
            {
                CheckBoxPhone.IsChecked = true;

                // Contact Name
                TextBoxPhoneContactName.Text = xmlphone.ContactName;

                // Contact Icon
                TextBoxPhoneContactIcon.Text = xmlphone.ContactIcon;
                ComboBoxPhoneContactIcons.SelectedIndex = PhoneContactCollection.FindIndex(x => string.Compare(x.Name, xmlphone.ContactIcon, true) == 0);
                
                // Dialing timeout
                IntegerUpDownPhoneDialing.Value = xmlphone.DialTimeout;

                // Sound
                if (xmlphone.Sound.File != "")
                {
                    TextBoxPhoneSound.Text = Path.GetDirectoryName(profilePath) + "\\" + xmlphone.Sound.File;
                    SliderSoundVolume.Value = xmlphone.Sound.Volume;
                }

                // Notification
                PhoneNotification = xmlphone.Notification;

                // Shortcut
                if (xmlphone.Keys.Count > 0)
                {
                    foreach (string key in xmlphone.Keys)
                        TextBoxPhoneShortcut.Text += key + "\r\n";
                    TextBoxPhoneShortcut.Text = TextBoxPhoneShortcut.Text.Remove(TextBoxPhoneShortcut.Text.Length - 2, 2);
                }
            }

            if (xmlmenu != null)
            {
                CheckBoxMenu.IsChecked = true;

                // Banner
                if (xmlmenu.Banner != "")
                    TextBoxMenuBanner.Text = Path.GetDirectoryName(profilePath) + "\\" + xmlmenu.Banner;

                // Hotkey
                if (xmlmenu.HotkeyModifier != 0)
                    if (DictionaryModifierKeys.ContainsValue(xmlmenu.HotkeyModifier))
                        ComboBoxMenuHotkeyModifiers.SelectedValue = DictionaryModifierKeys.FirstOrDefault(x => x.Value == xmlmenu.HotkeyModifier).Key;

                if (xmlmenu.Hotkey != 0)
                    TextBoxMenuHotkey.Text = xmlmenu.Hotkey.ToString();

                if (xmlmenu.GamepadHotkey != 0)
                    TextBoxMenuHotkey.Text = xmlmenu.GamepadHotkey.ToString();

                // Items
                RootMenu.Items.AddRange(xmlmenu.Items);
                foreach (NativeUIItem item in RootMenu.Items)
                    item.ParentMenu = RootMenu;
            }

        }

        private void InitializeWindow()
        {
            InitializeComponent();

            string[] resources = Tools.GetResourceNames();

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
            ComboBoxPhoneContactIcons.SelectedIndex = PhoneContactCollection.FindIndex(x => x.Name == XmlPhone.DefaultIcon);

            ComboBoxMenuHotkeyModifiers.ItemsSource = DictionaryModifierKeys;
            ComboBoxMenuHotkeyModifiers.SelectedIndex = 0;

            RootMenu = new NativeUIMenu()
            {
                Text = "Menu",
                Items = new List<NativeUIItem>()
            };

            Menu.Add(RootMenu);

            TreeViewMenu.ItemsSource = Menu;
            ItemsControlPreviewMenu.ItemsSource = RootMenu.Items;

            // Check for updates
            Thread checkUpdates = new Thread(CheckForUpdates);
            checkUpdates.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CheckUpdates != null) CheckUpdates.Abort();
        }

        private void CheckForUpdates()
        {
            if (Tools.IsUpdateAvailable())
            {
                StatusBarUpdates.Dispatcher.Invoke(() => {
                    StatusBarUpdates.Background = Brushes.LightYellow;
                });

                TextBlockUpdateProfileEditor.Dispatcher.Invoke(() => {
                    TextBlockUpdateProfileEditor.Text = "A new version of Profile Editor is available!";
                });

                TextBlockUpdateProfileEditorLink.Dispatcher.Invoke(() => {
                    TextBlockUpdateProfileEditorLink.Text = "Click to visit the download page";
                });
            }
        }

        private void TextBlockUpdateProfileEditorLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Bob74/ProfileEditor");
        }

        #region GUI - Phone
        /*
            PHONE
        */
        private void CheckBoxPhone_Checked(object sender, RoutedEventArgs e)
        {
            GridPhone.IsEnabled = true;
            GridPreviewPhone.Visibility = Visibility.Visible;
        }
        private void CheckBoxPhone_Unchecked(object sender, RoutedEventArgs e)
        {
            GridPhone.IsEnabled = false;
            GridPreviewPhone.Visibility = Visibility.Hidden;
        }

        // Contact Name
        private void TextBoxContactName_TextChanged(object sender, TextChangedEventArgs e)
        {
            PreviewContactName.Text = TextBoxPhoneContactName.Text;
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
            try
            {
                PreviewContactIcon.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/ProfileEditor;component/" + ((PhoneContact)e.AddedItems[0]).Icon)));
                TextBoxPhoneContactIcon.Text = ((PhoneContact)e.AddedItems[0]).Name;
            }
            catch
            {
                // Can happen if we try to select an invalid icon (ie: when loading a bad profile).
            }
        }
        private void TextBoxPhoneContactIcon_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!TextBoxPhoneContactIcon.Text.StartsWith("CHAR_", StringComparison.InvariantCultureIgnoreCase))
            {
                CheckBoxPhoneContactNameBold.IsChecked = true;
                CheckBoxPhoneContactNameBold.IsEnabled = false;
            }
            else
            {
                CheckBoxPhoneContactNameBold.IsChecked = false;
                CheckBoxPhoneContactNameBold.IsEnabled = true;
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


        #endregion

        #region GUI - Menu
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

        // Banner
        private void ButtonMenuBannerBrowse_Click(object sender, RoutedEventArgs e)
        {
            string filter = "Image files|";
            foreach (string ext in BannerFileExtensions)
            {
                filter += "*" + ext + ";";
            }

            OpenFileDialog dial = new OpenFileDialog() { InitialDirectory = BaseDir, Filter = filter };
            if (dial.ShowDialog() == true)
            {
                TextBoxMenuBanner.Text = dial.FileName;
                RefreshTreeview();
            }
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
        #endregion

        #region Preview - Phone
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
                NotificationPreviewWindow.Top = System.Windows.Forms.Cursor.Position.Y - NotificationPreviewWindow.Height - 10;
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
                NotificationPreviewWindow.Top = System.Windows.Forms.Cursor.Position.Y - NotificationPreviewWindow.Height - 10;
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
        #endregion

        #region Preview - Menu
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
                    NativeUIMenu parent = ((NativeUIMenuSubmenu)item).ParentMenu;

                    PreviewCurrentSubmenu.Clear();
                    if (File.Exists(TextBoxMenuBanner.Text))
                        PreviewCurrentSubmenu.Add(new NativeUIBanner() { Text = "Banner", FilePath = TextBoxMenuBanner.Text });
                    PreviewCurrentSubmenu.Add(new NativeUIMenuSubtitle() { Text = ((NativeUIMenuSubmenu)item).Text });
                    PreviewCurrentSubmenu.AddRange(((NativeUIMenuSubmenu)item).Items);
                    PreviewCurrentSubmenu.Add(new NativeUIBack(parent));
                    if (parent != null && parent != RootMenu) PreviewCurrentSubmenu.Add(new NativeUIReturnMenu());

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
                    if (((NativeUIBack)item).ParentMenu != RootMenu)
                    {
                        NativeUIMenu parent = ((NativeUIBack)item).ParentMenu;

                        PreviewCurrentSubmenu.Clear();
                        if (File.Exists(TextBoxMenuBanner.Text))
                            PreviewCurrentSubmenu.Add(new NativeUIBanner() { Text = "Banner", FilePath = TextBoxMenuBanner.Text });
                        if (parent.ParentMenu != RootMenu as NativeUIMenuSubmenu) PreviewCurrentSubmenu.Add(new NativeUIMenuSubtitle() { Text = parent.Text });
                        PreviewCurrentSubmenu.AddRange(((NativeUIBack)item).ParentMenu.Items);
                        if (parent.ParentMenu != RootMenu as NativeUIMenuSubmenu) PreviewCurrentSubmenu.Add(new NativeUIBack(parent.ParentMenu));
                        if (parent.ParentMenu != null && parent.ParentMenu != RootMenu) PreviewCurrentSubmenu.Add(new NativeUIReturnMenu());

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
        #endregion

        #region TreeView - Context menu
        /*
            TreeView Submenu + Item : Context menu
        */
        private void AddItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void AddItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AddMenuItem((NativeUIItem)TreeViewMenu.SelectedItem, ItemType.Item);
        }
        private void AddSubmenuCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void AddSubmenuCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AddMenuItem((NativeUIItem)TreeViewMenu.SelectedItem, ItemType.Submenu);
        }
        private void RemoveItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void RemoveItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RemoveItem((NativeUIItem)TreeViewMenu.SelectedItem);
        }
        private void EditItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void EditItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            EditItem((NativeUIItem)TreeViewMenu.SelectedItem);
        }
        private void CopyItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void CopyItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (TreeViewMenu.SelectedItem is NativeUIMenuItem)
                ClipBoardItem = new NativeUIMenuItem((NativeUIMenuItem)TreeViewMenu.SelectedItem);
            else if (TreeViewMenu.SelectedItem is NativeUIMenuSubmenu)
                ClipBoardItem = new NativeUIMenuSubmenu((NativeUIMenuSubmenu)TreeViewMenu.SelectedItem);
        }
        private void CutItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void CutItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ClipBoardItem = (NativeUIItem)TreeViewMenu.SelectedItem;
            GetItemParent(ClipBoardItem).Items.Remove(ClipBoardItem);

            IsClipBoardItemCut = true;

            RefreshTreeview();
        }
        private void PasteItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (ClipBoardItem != null)
                e.CanExecute = true;
        }
        private void PasteItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NativeUIMenu parent = GetItemClosestNode((NativeUIItem)TreeViewMenu.SelectedItem);

            if (ClipBoardItem is NativeUIMenuItem)
            {
                parent.Items.Add(new NativeUIMenuItem((NativeUIMenuItem)ClipBoardItem, parent));
            }
            else if (ClipBoardItem is NativeUIMenuSubmenu)
            {
                if (IsClipBoardItemCut)
                    parent.Items.Add(new NativeUIMenuSubmenu((NativeUIMenuSubmenu)ClipBoardItem, parent));
                else
                    parent.Items.Add(new NativeUIMenuSubmenu((NativeUIMenuSubmenu)ClipBoardItem, parent));
            }
            if (IsClipBoardItemCut)
                ClipBoardItem = null;

            IsClipBoardItemCut = false;

            RefreshTreeview();
        }

        /*
            TreeView Item specific : Context menu
        */
        // TreeViewItem selection using Right-click
        // https://www.telerik.com/forums/selecting-node-on-mouse-right-click
        private void TreeViewMenu_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem ClickedTreeViewItem = new TreeViewItem();

            //find the original object that raised the event
            UIElement ClickedItem = VisualTreeHelper.GetParent(e.OriginalSource as UIElement) as UIElement;

            //find the clicked TreeViewItem
            while ((ClickedItem != null) && !(ClickedItem is TreeViewItem))
            {
                ClickedItem = VisualTreeHelper.GetParent(ClickedItem) as UIElement;
            }

            ClickedTreeViewItem = ClickedItem as TreeViewItem;
            if (ClickedTreeViewItem != null)
            {
                ClickedTreeViewItem.IsSelected = true;
                ClickedTreeViewItem.Focus();
            }
        }

        /*
            TreeView Main specific : Context menu
        */
        // Clear all
        private void TreeViewMainContextClear_Click(object sender, RoutedEventArgs e)
        {
            RootMenu.Items.Clear();
            TextBoxMenuBanner.Text = "";
            RefreshTreeview();
        }
        #endregion

        #region TreeView - Context actions
        internal void AddMenuItem(NativeUIItem node, ItemType itemType)
        {
            if (itemType == ItemType.Item)
                AddItem(node);
            else if (itemType == ItemType.Submenu)
                AddSubmenu(node);
        }

        internal void AddItem(NativeUIItem node)
        {
            NativeUIMenu parent = GetSuitableParentFromItem(node);
            NativeUIMenuItem item = new NativeUIMenuItem(parent);

            MenuItemConfiguration = new MenuItemConfiguration(ref item) { Owner = this };
            MenuItemConfiguration.ShowDialog();

            if (!MenuItemConfiguration.DialogResult.HasValue || !MenuItemConfiguration.DialogResult.Value)
                item = null;

            MenuItemConfiguration = null;

            if (item != null)
            {
                parent.Items.Add(item);
                RefreshTreeview();
            }
        }

        internal void AddSubmenu(NativeUIItem node)
        {
            NativeUIMenu parent = GetSuitableParentFromItem(node);
            NativeUIMenuSubmenu submenu = new NativeUIMenuSubmenu(parent);

            MenuSubmenuConfiguration = new MenuSubmenuConfiguration(ref submenu) { Owner = this };
            MenuSubmenuConfiguration.ShowDialog();

            if (!MenuSubmenuConfiguration.DialogResult.HasValue || !MenuSubmenuConfiguration.DialogResult.Value)
                submenu = null;

            MenuSubmenuConfiguration = null;

            if (submenu != null)
            {
                parent.Items.Add(submenu);
                RefreshTreeview();
            }

            RefreshTreeview();
        }

        internal void RemoveItem(NativeUIItem itemToRemove)
        {
            NativeUIMenu parent = GetItemParent(itemToRemove);

            if (itemToRemove is NativeUIBanner)
                TextBoxMenuBanner.Text = "";
            else
                parent.Items.Remove(itemToRemove);

            RefreshTreeview();
        }

        internal void EditItem(NativeUIItem item)
        {
            if (item is NativeUIMenuItem)
            {
                NativeUIMenuItem menuItem = item as NativeUIMenuItem;
                MenuItemConfiguration = new MenuItemConfiguration(ref menuItem) { Owner = this };
                MenuItemConfiguration.ShowDialog();
                MenuItemConfiguration = null;
            }
            else if (item is NativeUIMenuSubmenu)
            {
                NativeUIMenuSubmenu submenuItem = item as NativeUIMenuSubmenu;
                MenuSubmenuConfiguration = new MenuSubmenuConfiguration(ref submenuItem) { Owner = this };
                MenuSubmenuConfiguration.ShowDialog();
                MenuSubmenuConfiguration = null;
            }

            RefreshTreeview();
        }
        #endregion

        #region TreeView - Functions
        // Return the item closest node.
        // If the item is a node, the closest node will be the item.
        // Else, it will be its parent.
        internal NativeUIMenu GetItemClosestNode(NativeUIItem item)
        {
            if (item != null)
            {
                // Item is a node
                if (item is NativeUIMenuSubmenu || item is NativeUIMenu)
                    return item as NativeUIMenu;
                // Item is not a node but has a parent
                else if (item.ParentMenu != null)
                    return item.ParentMenu;
                // Item is not a node and has no parent
                else
                    return RootMenu;
            }

            return RootMenu;
        }

        internal NativeUIMenu GetSuitableParentFromItem(NativeUIItem item)
        {
            if (item != null)
            {
                if (item is NativeUIMenuSubmenu || item is NativeUIMenu)
                    return item as NativeUIMenu;
                else if (item.ParentMenu != null)
                    return item.ParentMenu;
            }

            return RootMenu;
        }

        internal NativeUIMenu GetItemParent(NativeUIItem item)
        {
            return item.ParentMenu ?? RootMenu;
        }

        internal void RefreshTreeview()
        {
            TreeViewMenu.Items.Refresh();
            ItemsControlPreviewMenu.Items.Refresh();
            TreeViewMenu.Focus();
        }
        #endregion

        #region Menu - Hotkey
        private void ComboBoxMenuHotkeyModifiers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string[] keyCombination = TextBlockMenuHotkey.Text.Split(new string[] { " + " }, StringSplitOptions.None);
            string currentKey = keyCombination[keyCombination.GetUpperBound(0)];

            if (ComboBoxMenuHotkeyModifiers.SelectedValue != null)
            {
                if ((int)ComboBoxMenuHotkeyModifiers.SelectedValue != 0)
                {
                    TextBlockMenuHotkey.Text = ((KeyValuePair<string, ModifierKeys>)e.AddedItems[0]).Key + " + " + currentKey;
                    return;
                }
            }

            TextBlockMenuHotkey.Text = currentKey;
        }


        private void TextBoxMenuHotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int pressedKey = KeyInterop.VirtualKeyFromKey(e.Key);
            string pressedKeyName = ((System.Windows.Forms.Keys)pressedKey).ToString();

            TextBoxMenuHotkey.Text = pressedKey.ToString();

            if (ComboBoxMenuHotkeyModifiers.SelectedValue != null)
            {
                if ((int)ComboBoxMenuHotkeyModifiers.SelectedValue != 0)
                {
                    TextBlockMenuHotkey.Text = ComboBoxMenuHotkeyModifiers.Text + " + " + pressedKeyName;
                    return;
                }
            }

            TextBlockMenuHotkey.Text = pressedKeyName;
        }

        private void TextBoxMenuHotkey_GotFocus(object sender, RoutedEventArgs e)
        {
            // Initialize XInput
            Controller[] controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };

            // Get 1st controller available
            Controller controller = null;
            foreach (var selectControler in controllers)
            {
                if (selectControler.IsConnected)
                {
                    controller = selectControler;
                    break;
                }
            }

            GamePadThread = new Thread(GamePadKeyPressDetection);
            GamePadThread.Start(controller);
        }

        private void GamePadKeyPressDetection(object objectState)
        {
            Controller controller = objectState as Controller;

            if (controller != null)
            {
                bool TextBoxIsFocused = TextBoxMenuHotkey.Dispatcher.Invoke<bool>(() => {
                    return TextBoxMenuHotkey.IsFocused;
                });

                while (TextBoxIsFocused)
                {
                    // Poll events from joystick
                    if (controller.IsConnected)
                    {
                        State currentState = controller.GetState();

                        if (currentState.Gamepad.Buttons != GamepadButtonFlags.None)
                        {
                            TextBoxMenuHotkey.Dispatcher.Invoke(() => {
                                TextBoxMenuHotkey.Text = ((long)currentState.Gamepad.Buttons).ToString();
                            });

                            string ComboBoxValue = ComboBoxMenuHotkeyModifiers.Dispatcher.Invoke<string>(() => {
                                return ComboBoxMenuHotkeyModifiers.Text;
                            });

                            if (ComboBoxValue != "" && ComboBoxValue != "None")
                            {
                                string modifier = ComboBoxMenuHotkeyModifiers.Dispatcher.Invoke<string>(() => {
                                    return ComboBoxMenuHotkeyModifiers.Text;
                                });
                                TextBlockMenuHotkey.Dispatcher.Invoke(() => {
                                    TextBlockMenuHotkey.Text = modifier + " + " + currentState.Gamepad.Buttons.ToString() + " (GAMEPAD) ";
                                });
                                continue;
                            }

                            TextBlockMenuHotkey.Dispatcher.Invoke(() => {
                                TextBlockMenuHotkey.Text = currentState.Gamepad.Buttons.ToString() + " (GAMEPAD)";
                            });
                        }

                        // Give us time to release multiple buttons at once
                        Thread.Sleep(100);
                    }

                    // Check if the TextBox is still focused
                    TextBoxIsFocused = TextBoxMenuHotkey.Dispatcher.Invoke<bool>(() => {
                        return TextBoxMenuHotkey.IsFocused;
                    });
                }

            }
        }

        private void ButtonMenuClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            TextBoxMenuHotkey.Text = "";
            ComboBoxMenuHotkeyModifiers.SelectedIndex = 0;
            TextBlockMenuHotkey.Text = "";
        }
        #endregion



        private bool IsReadyToGenerate()
        {
            // Common
            // ******

            // Phone
            if (CheckBoxPhone.IsChecked ?? false)
            {
                // Empty contact name
                if (TextBoxPhoneContactName.Text == "")
                {
                    MessageBox.Show("You must specify a phone contact name.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }

            // Menu
            if (CheckBoxMenu.IsChecked ?? false)
            {
                // No items
                if (RootMenu.Items.Count <= 0)
                {
                    MessageBox.Show("If you use a menu, you must create items or submenus. Right click on the \"Menu\" item inside the frame in the menu section to add items.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }

            // Choice dependent
            // ****************

            // No Phone and no Menu
            if ((!CheckBoxPhone.IsChecked ?? true) && (!CheckBoxMenu.IsChecked ?? true))
            {
                MessageBox.Show("You must fill at least the Phone part or the Menu part.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // Menu only
            if ((!CheckBoxPhone.IsChecked ?? true) && (CheckBoxMenu.IsChecked ?? false))
            {
                // No hotkey or Modifier
                if ((TextBoxMenuHotkey.Text == "" && ((ComboBoxMenuHotkeyModifiers.SelectedValue != null) ? (int)ComboBoxMenuHotkeyModifiers.SelectedValue : 0) == 0))
                {
                    MessageBox.Show("Since there is no phone contact, you must set a hotkey to open the menu.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }

            // Phone only
            if ((CheckBoxPhone.IsChecked ?? false) && (!CheckBoxMenu.IsChecked ?? true))
            {
                // Phone + No menu + No shortcut
                if (TextBoxPhoneShortcut.Text == "" && (!CheckBoxMenu.IsChecked ?? true))
                {
                    MessageBox.Show("You must specify a shortcut to press when the phone contact will be selected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }

            // Phone and Menu
            if ((CheckBoxPhone.IsChecked ?? false) && (CheckBoxMenu.IsChecked ?? false))
            {

            }

            return true;
        }

        private void ButtonGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (IsReadyToGenerate())
            {
                string filePath = "";

                SaveFileDialog dial = new SaveFileDialog() { InitialDirectory = BaseDir, Filter = "XML file|*.xml;" };
                if (dial.ShowDialog() == true)
                {
                    filePath = dial.FileName;

                    XmlPhone phone = null;
                    XmlMenu menu = null;

                    // Phone
                    try
                    {
                        if (CheckBoxPhone.IsChecked ?? false)
                        {
                            List<string> shortcut = new List<string>();

                            // Add the keys only if there is no menu
                            if (!CheckBoxMenu.IsChecked ?? true)
                            {
                                foreach (string key in TextBoxPhoneShortcut.Text.Replace("\r", "").Split('\n'))
                                    if (key != "") shortcut.Add(key);
                            }

                            phone = new XmlPhone()
                            {
                                ContactName = TextBoxPhoneContactName.Text,
                                ContactIcon = TextBoxPhoneContactIcon.Text,
                                Keys = shortcut
                            };

                            if (CheckBoxPhoneContactNameBold.IsChecked ?? false) phone.Bold = true;
                            if (IntegerUpDownPhoneDialing.Value != null) phone.DialTimeout = IntegerUpDownPhoneDialing.Value;
                            if (TextBoxPhoneSound.Text != "") phone.Sound = new MenuSound() { File = TextBoxPhoneSound.Text, Volume = (int)SliderSoundVolume.Value };
                            if (PhoneNotification != null) phone.Notification = PhoneNotification;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error creating the phone section: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    // Menu
                    try
                    {
                        if (CheckBoxMenu.IsChecked ?? false)
                        {
                            ModifierKeys hotkeyModifier = (ComboBoxMenuHotkeyModifiers.SelectedValue != null) ? (ModifierKeys)ComboBoxMenuHotkeyModifiers.SelectedValue : 0;
                            Key hotkey = 0;
                            int gamepadHotkey = 0;

                            if (TextBlockMenuHotkey.Text != "")
                            {
                                if (TextBlockMenuHotkey.Text.Contains("GAMEPAD"))
                                {
                                    gamepadHotkey = int.Parse(TextBoxMenuHotkey.Text);
                                    hotkeyModifier = 0; // No keyboard modifier when using a gamepad
                                }
                                else
                                    hotkey = (Key)int.Parse(TextBoxMenuHotkey.Text);
                            }

                            menu = new XmlMenu()
                            {
                                Banner = TextBoxMenuBanner.Text,
                                Hotkey = hotkey,
                                GamepadHotkey = gamepadHotkey,
                                HotkeyModifier = hotkeyModifier,
                                Items = RootMenu.Items
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error creating the menu section: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                    XmlProfile profile = new XmlProfile(phone, menu);

                    try
                    {
                        profile.ExportProfile(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An error occured while exporting the file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            new CreateLoadProfile().Show();
            Close();
        }
    }
}
