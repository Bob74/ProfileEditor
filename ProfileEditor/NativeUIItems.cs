using System.Collections.Generic;
using System.IO;

namespace ProfileEditor
{
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
            Keys = new List<string>();
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
}
