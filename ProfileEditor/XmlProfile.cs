using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ProfileEditor
{
    public class XmlPhone
    {
        // Contact
        public string ContactName { get; set; }
        public bool? Bold { get; set; }
        public string ContactIcon { get; set; }

        // Dialing
        public int? DialTimeout { get; set; }

        // Sound
        public string SoundFile { get; set; }
        public int? Volume { get; set; }

        // Notification
        public Notification Notification { get; set; }

        // Shortcut keys
        public List<string> Keys { get; set; }
    }
    class XmlMenu
    {
        // Banner
        public string Banner { get; set; }

        // Shortcut to open the menu
        public List<string> Hotkey { get; set; }

        // Menu items
        public List<NativeUIItem> Items { get; set; }
    }


    class XmlProfile
    {
        private string _profilePath;
        private XElement _profileFile;
        private XmlPhone _phone = null;
        private XmlMenu _menu = null;
        private XElement phoneSection;
        private XElement menuSection;

        public XmlProfile(string path, XmlPhone xmlPhone, XmlMenu xmlMenu)
        {
            _profilePath = path;
            _phone = xmlPhone;
            _menu = xmlMenu;

            ExportProfile();
        }

        public void ExportProfile()
        {
            CreateFile();
            
            try
            {
                _profileFile = XElement.Load(_profilePath);

                try
                {
                    FillXmlProfile();
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

        private void CreateFile()
        {
            FileInfo file = new FileInfo(_profilePath);

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

        private void FillXmlProfile()
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
                    _profileFile.Add(GetMenuSection());
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while writing Menu informations: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _profileFile.Save(_profilePath);
        }

        private XElement GetPhoneSection()
        {
            phoneSection = new XElement("Phone");
            phoneSection.Add(new XElement("ContactName", _phone.ContactName));
            if (_phone.Bold != null) phoneSection.Add(new XElement("Bold", _phone.Bold));
            phoneSection.Add(new XElement("ContactIcon", _phone.ContactIcon ?? "CHAR_DEFAULT"));
            if (_phone.DialTimeout != null) phoneSection.Add(new XElement("DialTimeout", _phone.DialTimeout));

            if (_phone.SoundFile != null) phoneSection.Add(new XElement("SoundFile", _phone.SoundFile));
            if (_phone.Volume != null) phoneSection.Add(new XElement("Volume", _phone.Volume));

            return phoneSection;
        }
        private XElement GetMenuSection()
        {
            return GetItems(_menu.Items);
        }


        private XElement GetItems(List<NativeUIItem> menuItems)
        {
            XElement content = new XElement("Menu");

            foreach (var menuItem in menuItems)
            {
                switch (menuItem)
                {
                    case NativeUIMenuSubmenu submenu:
                        // Creating a new SubMenu
                        XElement submenuSection = GetItems(submenu.Items);

                        // Renaming the submenu with its real name
                        submenuSection.Name = "SubMenu";
                        submenuSection.Add(new XAttribute("text", submenu.Text));
                        content.Add(submenuSection);
                        break;
                    case NativeUIMenuItem item:
                        // Creating a new SubItem
                        XElement subItem = new XElement("SubItem", new XAttribute("text", item.Text));
                        FillItem(item, subItem);
                        content.Add(subItem); // Simply closing
                        break;
                }
            }

            return content;
        }

        private void GetItems2(XElement submenuSection, List<NativeUIItem> menuItems)
        {
            bool IsRoot = false;
            if (submenuSection == null) IsRoot = true;

            foreach (var menuItem in menuItems)
            {
                switch (menuItem)
                {
                    case NativeUIMenuSubmenu submenu:
                        // Opening SubMenu
                        submenuSection = new XElement("SubMenu", new XAttribute("text", submenu.Text));
                        GetItems2(submenuSection, submenu.Items);
                        break;
                    case NativeUIMenuItem item:
                        // Opening SubItem
                        XElement subItem = new XElement("SubItem", new XAttribute("text", item.Text));
                        FillItem(item, subItem);

                        // Closing SubItem
                        if (submenuSection != null)
                            submenuSection.Add(subItem); // Adding to current submenu
                        else
                            menuSection.Add(subItem); // Simply closing
                        break;
                }

                // Closing SubMenu if opened
                if (submenuSection != null)
                {
                    submenuSection.Add(submenuSection);
                    submenuSection = null;
                }
                else
                {
                    menuSection.Add(submenuSection);
                }

                if (IsRoot)
                {
                    if (menuSection != null) menuSection.Add(submenuSection);
                    submenuSection = null;
                }
            }
        }
        
        private void FillItem(NativeUIMenuItem item, XElement subItem)
        {
            foreach (string key in item.Keys)
                subItem.Add(new XElement("Key", key));
        }

    }
}
