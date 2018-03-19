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
    /// Logique d'interaction pour MenuShortcut.xaml
    /// </summary>
    public partial class MenuShortcutConfiguration : Window
    {
        List<string> Shortcuts;
        bool ListWasEmpty;

        public MenuShortcutConfiguration(ref List<string> shortcuts)
        {
            InitializeComponent();
            
            if (shortcuts.Count <= 0)
            {
                ListWasEmpty = true;
                Shortcuts = new List<string>();
            }

            Shortcuts = shortcuts;
        }


        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            Shortcuts.Clear();

            foreach (string key in TextBoxShortcut.Text.Replace("\r", "").Split('\n'))
            {
                Shortcuts.Add(key);
            }

            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!ListWasEmpty)
                DialogResult = true;
            else
                Close();
        }

    }
}
