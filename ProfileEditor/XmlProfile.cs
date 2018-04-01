using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;

namespace ProfileEditor
{
    public class XmlPhone
    {
        public static readonly string DefaultName = "Unknown";
        public static readonly string DefaultIcon = "CHAR_DEFAULT";
        public static readonly bool DefaultBold = false;
        public static readonly int DefaultDialTimeout = 0;

        // Contact
        public string ContactName { get; set; }
        public bool? Bold { get; set; } = null;
        public string ContactIcon { get; set; }

        // Dialing
        public int? DialTimeout { get; set; } = null;

        // Sound
        public MenuSound Sound { get; set; } = null;

        // Notification
        public Notification Notification { get; set; } = null;

        // Shortcut keys
        public List<string> Keys { get; set; }
    }
    public class XmlMenu
    {
        public static readonly string DefaultBanner = "";

        // Banner
        public string Banner { get; set; }

        // Shortcut to open the menu
        public ModifierKeys HotkeyModifier { get; set; }
        public Key Hotkey { get; set; }
        public int GamepadHotkey { get; set; }

        // Menu items
        public List<NativeUIItem> Items { get; set; }
    }


    class XmlProfile
    {
        private XElement _profileFile;
        private XmlPhone _phone = null;
        private XmlMenu _menu = null;

        public XmlProfile(XmlPhone xmlPhone, XmlMenu xmlMenu)
        {
            _phone = xmlPhone;
            _menu = xmlMenu;
        }
        public XmlProfile(string path)
        {
            try
            {
                _profileFile = XElement.Load(path);
            }
            catch (Exception e)
            {
                MessageBox.Show("Cannot open profile file: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Export the data into the xml profile.
        /// </summary>
        /// <param name="filepath">File path</param>
        public void ExportProfile(string filepath)
        {
            CreateFile(filepath);
            
            try
            {
                _profileFile = XElement.Load(filepath);

                try
                {
                    WriteXmlProfile(filepath);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Cannot write to the profile file: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Cannot open profile file: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Read the xml profile and return its content.
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns></returns>
        public XElement ImportProfile(out XmlPhone xmlPhone, out XmlMenu xmlMenu)
        {
            xmlPhone = ReadPhoneValues(_profileFile);
            xmlMenu = ReadMenuValues(_profileFile);

            return _profileFile;
        }


        private void CreateFile(string path)
        {
            FileInfo file = new FileInfo(path);

            if (!file.Directory.Exists)
                Directory.CreateDirectory(file.Directory.FullName);

            XDocument doc =
                new XDocument(
                    new XDeclaration("1.0", Encoding.UTF8.HeaderName, String.Empty),
                    new XComment("Xml Document")
                );
            XElement main = new XElement("NMS");
            doc.Add(main);
            doc.Save(file.FullName);
        }

        private XmlPhone ReadPhoneValues(XElement profile)
        {
            if (profile?.Element("Phone") != null)
            {
                XElement phone = profile.Element("Phone");

                XmlPhone xmlphone = new XmlPhone();
                xmlphone.ContactName = phone.Element("ContactName")?.Value ?? XmlPhone.DefaultName;
                xmlphone.ContactIcon = phone.Element("ContactIcon")?.Value ?? XmlPhone.DefaultIcon;
                xmlphone.DialTimeout = int.Parse(phone.Element("DialTimeout")?.Value ?? XmlPhone.DefaultDialTimeout.ToString());
                xmlphone.Bold = bool.Parse(phone.Element("Bold")?.Value ?? XmlPhone.DefaultBold.ToString());
                xmlphone.Sound = ReadXmlSound(phone);
                xmlphone.Notification = ReadXmlNotification(phone);

                xmlphone.Keys = new List<string>();

                try
                {
                    List<string> shortcut = new List<string>();

                    if (phone.Element("Keys") != null)
                        foreach (XElement key in phone.Element("Keys").Elements("Key"))
                            shortcut.Add(key.Value);
                    else
                        foreach (XElement key in phone.Elements("Key"))
                            shortcut.Add(key.Value);

                    xmlphone.Keys.AddRange(shortcut);
                }
                catch (Exception e)
                {
                    // If no menu, then there should be keys
                    if (profile?.Element("Menu") == null)
                        MessageBox.Show("Cannot find a <Key> entry in the <Phone> section: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                return xmlphone;
            }
            else
                return null;
        }
        private XmlMenu ReadMenuValues(XElement profile)
        {
            if (profile?.Element("Menu") != null)
            {
                XElement menu = profile.Element("Menu");

                XmlMenu xmlmenu = new XmlMenu();
                xmlmenu.Banner = menu.Element("Banner")?.Value ?? XmlMenu.DefaultBanner;
                
                try
                {
                    // Backward compatibility
                    if (menu.Element("Keys") != null)
                    {
                        xmlmenu.HotkeyModifier = (ModifierKeys)int.Parse(menu.Element("Keys").Element("ModifierKey")?.Value ?? "0");
                        xmlmenu.Hotkey = (Key)int.Parse(menu.Element("Keys").Element("Key")?.Value ?? "0");
                        xmlmenu.GamepadHotkey = int.Parse(menu.Element("Keys").Element("GamepadKey")?.Value ?? "0");
                    }
                    else
                    {
                        xmlmenu.HotkeyModifier = (ModifierKeys)int.Parse(menu.Element("ModifierKey")?.Value ?? "0");
                        xmlmenu.Hotkey = (Key)int.Parse(menu.Element("Key")?.Value ?? "0");
                        xmlmenu.GamepadHotkey = int.Parse(menu.Element("GamepadKey")?.Value ?? "0");
                    }
                }
                catch
                {
                    // Hotkeys to open the menu aren't required.
                }

                xmlmenu.Items = GetMenuSection(menu)?.Items ?? new List<NativeUIItem>();

                return xmlmenu;
            }
            else
                return null;
        }


        private MenuSound ReadXmlSound(XElement section)
        {
            MenuSound sound = new MenuSound();
            // Backward compatibility
            if (section.Element("Sound") != null)
            {
                sound.File = section.Element("Sound")?.Element("SoundFile")?.Value ?? "";
                sound.Volume = int.Parse(section.Element("Sound")?.Element("Volume")?.Value ?? MenuSound.DefaultVolume.ToString());
            }
            else
            {
                sound.File = section.Element("SoundFile")?.Value ?? "";
                sound.Volume = int.Parse(section.Element("Volume")?.Value ?? MenuSound.DefaultVolume.ToString());
            }

            return sound;
        }

        private Notification ReadXmlNotification(XElement section)
        {
            Notification notif = new Notification();
            // Backward compatibility
            if (section.Element("Notification") != null)
            {
                if (section.Element("Notification").Element("NotificationMessage") == null) return null;

                notif.Icon = section.Element("Notification").Element("NotificationIcon")?.Value ?? Notification.DefaultIcon;
                notif.Title = section.Element("Notification").Element("NotificationTitle")?.Value ?? Notification.DefaultTitle;
                notif.Subtitle = section.Element("Notification").Element("NotificationSubtitle")?.Value ?? Notification.DefaultSubtitle;
                notif.Message = section.Element("Notification").Element("NotificationMessage")?.Value ?? Notification.DefaultMessage;
                notif.Delay = int.Parse(section.Element("Notification").Element("NotificationDelay")?.Value ?? Notification.DefaultDelay.ToString());
                notif.Sound = bool.Parse(section.Element("Notification").Element("NotificationSound")?.Value ?? Notification.DefaultSound.ToString());
            }
            else
            {
                if (section.Element("NotificationMessage") == null) return null;

                notif.Icon = section.Element("NotificationIcon")?.Value ?? Notification.DefaultIcon;
                notif.Title = section.Element("NotificationTitle")?.Value ?? Notification.DefaultTitle;
                notif.Subtitle = section.Element("NotificationSubtitle")?.Value ?? Notification.DefaultSubtitle;
                notif.Message = section.Element("NotificationMessage")?.Value ?? Notification.DefaultMessage;
                notif.Delay = int.Parse(section.Element("NotificationDelay")?.Value ?? Notification.DefaultDelay.ToString());
                notif.Sound = bool.Parse(section.Element("NotificationSound")?.Value ?? Notification.DefaultSound.ToString());
            }

            return notif;
        }


        private void WriteXmlProfile(string filepath)
        {
            // Phone part
            try
            {
                if (_phone != null)
                    _profileFile.Add(GetPhoneSection());
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while writing Phone informations: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Menu part
            try
            {
                if (_menu != null)
                {
                    // Creating menu
                    XElement menuSection = GetMenuSection(_menu.Items);

                    // Adding Banner
                    if (_menu.Banner != "")
                    {
                        XElement banner = new XElement("Banner", Path.GetFileName(_menu.Banner));
                        menuSection.AddFirst(banner);
                    }

                    // Adding Keys
                    if ((int)_menu.HotkeyModifier != 0 && (int)_menu.Hotkey != 0 && _menu.GamepadHotkey != 0)
                    {
                        XElement keys = new XElement("Keys");
                        if (_menu.GamepadHotkey != 0)
                            keys.Add(new XElement("GamepadKey", _menu.GamepadHotkey));
                        else
                        {
                            if ((int)_menu.HotkeyModifier != 0) keys.Add(new XElement("ModifierKey", (int)_menu.HotkeyModifier));
                            if ((int)_menu.Hotkey != 0) keys.Add(new XElement("Key", (int)_menu.Hotkey));
                        }
                        menuSection.AddFirst(keys);
                    }

                    _profileFile.Add(menuSection);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while writing Menu informations: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _profileFile.Save(filepath);
        }

        private XElement GetPhoneSection()
        {
            XElement phoneSection = new XElement("Phone");
            phoneSection.Add(new XElement("ContactName", _phone.ContactName));
            if (_phone.Bold != null && _phone.Bold != XmlPhone.DefaultBold) phoneSection.Add(new XElement("Bold", _phone.Bold));
            phoneSection.Add(new XElement("ContactIcon", _phone.ContactIcon ?? XmlPhone.DefaultIcon));
            if (_phone.DialTimeout != null && _phone.DialTimeout != XmlPhone.DefaultDialTimeout) phoneSection.Add(new XElement("DialTimeout", _phone.DialTimeout));
            if (_phone.Sound != null) phoneSection.Add(GetSoundElement(_phone));
            if (_phone.Notification != null) phoneSection.Add(GetNotificationElement(_phone));
            if (_phone.Keys.Count > 0) phoneSection.Add(GetKeysElement(_phone));

            return phoneSection;
        }
        private XElement GetMenuSection(List<NativeUIItem> menuItems)
        {
            XElement content = new XElement("Menu");

            foreach (var menuItem in menuItems)
            {
                switch (menuItem)
                {
                    case NativeUIMenuSubmenu submenu:
                        // Creating & filling a new SubMenu
                        XElement submenuSection = GetMenuSection(submenu.Items);

                        // Renaming the submenu with its real name
                        submenuSection.Name = "SubMenu";
                        submenuSection.Add(new XAttribute("text", submenu.Text));
                        content.Add(submenuSection);
                        break;
                    case NativeUIMenuItem item:
                        // Creating a new SubItem
                        XElement subItem = new XElement("SubItem", new XAttribute("text", item.Text));
                        FillXmlItem(item, subItem);
                        content.Add(subItem);
                        break;
                }
            }

            return content;
        }
        private NativeUIMenuSubmenu GetMenuSection(XElement menuItems)
        {
            NativeUIMenuSubmenu content = new NativeUIMenuSubmenu();

            foreach (XElement menuItem in menuItems.Elements())
            {
                if (menuItem.Name == "SubMenu")
                {
                    NativeUIMenuSubmenu submenuItems = GetMenuSection(menuItem);
                    submenuItems.Text = menuItem.Attribute("text").Value;
                    submenuItems.ParentMenu = content;
                    content.Items.Add(submenuItems);
                }
                else if (menuItem.Name == "SubItem")
                {
                    NativeUIMenuItem item = new NativeUIMenuItem(content);
                    item.Text = menuItem.Attribute("text").Value;
                    
                    List<string> shortcut = new List<string>();
                    foreach (XElement key in menuItem.Elements("Key"))
                        shortcut.Add(key.Value);

                    item.Keys.AddRange(shortcut);

                    content.Items.Add(item);
                }
            }

            return content;
        }

        private void FillXmlItem(NativeUIMenuItem item, XElement subItem)
        {
            if (item.Sound != null) subItem.Add(GetSoundElement(item));
            if (item.Notification != null) subItem.Add(GetNotificationElement(item));

            foreach (string key in item.Keys)
                subItem.Add(new XElement("Key", key));
        }

        private XElement GetSoundElement(NativeUIMenuItem item)
        {
            return MenuSoundToXElement(item.Sound);
        }
        private XElement GetSoundElement(XmlPhone xmlphone)
        {
            return MenuSoundToXElement(xmlphone.Sound);
        }
        private XElement MenuSoundToXElement(MenuSound sound)
        {
            XElement soundItem = new XElement("Sound");

            if (sound != null)
            {
                string fileName = Path.GetFileName(sound.File);
                soundItem.Add(new XElement("SoundFile", fileName));
                if (sound.Volume != MenuSound.DefaultVolume) soundItem.Add(new XElement("Volume", sound.Volume));
            }

            return soundItem;
        }

        private XElement GetNotificationElement(NativeUIMenuItem item)
        {
            return NotificationToXElement(item.Notification);
        }
        private XElement GetNotificationElement(XmlPhone xmlphone)
        {
            return NotificationToXElement(xmlphone.Notification);
        }
        private XElement NotificationToXElement(Notification notif)
        {
            XElement notificationItem = new XElement("Notification");

            if (notif != null)
            {
                if (notif.Icon != Notification.DefaultIcon) notificationItem.Add(new XElement("NotificationIcon", notif.Icon));
                if (notif.Title != Notification.DefaultTitle) notificationItem.Add(new XElement("NotificationTitle", notif.Title));
                if (notif.Subtitle != Notification.DefaultSubtitle) notificationItem.Add(new XElement("NotificationSubtitle", notif.Subtitle));
                notificationItem.Add(new XElement("NotificationMessage", notif.Message));
                if (notif.Sound != Notification.DefaultSound) notificationItem.Add(new XElement("NotificationSound", notif.Sound));
                if (notif.Delay != Notification.DefaultDelay) notificationItem.Add(new XElement("NotificationDelay", notif.Delay));
            }

            return notificationItem;
        }

        private XElement GetKeysElement(XmlPhone item)
        {
            XElement keysItem = new XElement("Keys");
            foreach (string key in item.Keys)
                keysItem.Add(new XElement("Key", key));

            return keysItem;
        }
    }
}
