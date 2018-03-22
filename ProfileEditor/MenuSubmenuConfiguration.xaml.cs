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
    /// Logique d'interaction pour MenuSubmenuConfiguration.xaml
    /// </summary>
    public partial class MenuSubmenuConfiguration : Window
    {
        NativeUIMenuSubmenu Submenu;

        public MenuSubmenuConfiguration(ref NativeUIMenuSubmenu submenu)
        {
            InitializeComponent();

            if (submenu == null)
            {
                submenu = new NativeUIMenuSubmenu();
            }

            Submenu = submenu;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxItemText.Text = Submenu.Text;

            TextBoxItemText.Focus();
            TextBoxItemText.SelectAll();
        }

        // Ok
        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxItemText.Text != "")
            {
                // Text
                Submenu.Text = TextBoxItemText.Text;

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
            Submenu = null;
            Close();
        }

    }
}
