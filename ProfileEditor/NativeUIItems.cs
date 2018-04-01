using System.Collections.Generic;
using System.IO;

namespace ProfileEditor
{
    public enum ItemType { None, Banner, Item, Submenu }

    public abstract class NativeUIItem
    {
        public virtual string Text { get; set; }
        public virtual string DisplayName { get => Text; }
        public virtual NativeUIMenu ParentMenu { get; set; } = null;
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

    public class NativeUIMenu : NativeUIItem
    {
        public override string DisplayName { get => Text + " [" + Items.Count + "]"; }
        public List<NativeUIItem> Items { get; set; }

        public NativeUIMenu(NativeUIMenu parent = null)
        {
            ParentMenu = parent;
            Items = new List<NativeUIItem>();
        }
    }
    public class NativeUIMenuSubmenu : NativeUIMenu
    {
        public NativeUIMenuSubmenu(NativeUIMenu parent = null)
        {
            ParentMenu = parent;
            Items = new List<NativeUIItem>();
        }

        public NativeUIMenuSubmenu(NativeUIMenuSubmenu submenu, NativeUIMenu parent = null)
        {
            ParentMenu = parent;
            Text = submenu.Text;
            Items = new List<NativeUIItem>();

            foreach (NativeUIItem item in submenu.Items)
            {
                if (item is NativeUIMenuItem)
                    Items.Add(new NativeUIMenuItem((NativeUIMenuItem)item, this));
                else if (item is NativeUIMenuSubmenu)
                    Items.Add(new NativeUIMenuSubmenu((NativeUIMenuSubmenu)item, this));
            }
        }
    }

    // Only used in menu preview
    public class NativeUIMenuSubtitle : NativeUIItem { }

    public class NativeUIMenuItem : NativeUIItem
    {
        public override string DisplayName { get => GetDisplayName(); }
        public List<string> Keys { get; set; }
        public string KeySequence { get => GetKeySequence(); }
        public Notification Notification { get; set; }
        public MenuSound Sound { get; set; }

        public NativeUIMenuItem(NativeUIMenu parent = null)
        {
            ParentMenu = parent;
            Keys = new List<string>();
        }
        public NativeUIMenuItem(NativeUIMenuItem item, NativeUIMenu parent = null)
        {
            ParentMenu = parent;
            Text = item.Text;
            Keys = new List<string>();
            Keys.AddRange(item.Keys);
            if (Notification != null) Notification = new Notification(item.Notification);
            if (Sound != null) Sound = new MenuSound(item.Sound);
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
        public NativeUIBack(NativeUIMenu parent = null)
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
}
