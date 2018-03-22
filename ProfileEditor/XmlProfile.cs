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
        private XmlPhone _phone;
        private XmlMenu _menu;
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
                FillXmlProfile();
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
            if (_phone != null) _profileFile.Add(GetPhoneSection());
            // Menu part
            if (_menu != null) _profileFile.Add(GetMenuSection());

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
            menuSection = new XElement("Menu");

            AnalyzeTree(_menu.Items);
            return menuSection;
        }


        private void GetItems(List<NativeUIItem> menuItems)
        {

        }

        private void AnalyzeTree(List<NativeUIItem> menuItems)
        {
            foreach (var menuItem in menuItems)
            {
                switch (menuItem)
                {
                    case NativeUIMenuSubmenu submenu:
                        // add submenu to section
                        // set current section to submenu
                        //AnalyzeTree(submenu.Items);
                        break;
                    case NativeUIMenuItem item:
                        XElement subItem = new XElement("SubItem");
                        subItem.Attribute("Text").SetValue(item.Text);
                        menuSection.Add(subItem);
                        break;
                }

                // go back to root section

            }
        }

    }
}
