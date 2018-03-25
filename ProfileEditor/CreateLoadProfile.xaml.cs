using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProfileEditor
{
    /// <summary>
    /// Logique d'interaction pour CreateLoadProfile.xaml
    /// </summary>
    public partial class CreateLoadProfile : Window
    {
        public CreateLoadProfile()
        {
            InitializeComponent();
        }

        private void ButtonLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dial = new OpenFileDialog() { InitialDirectory = MainWindow.BaseDir, Filter = "Xml file|*.xml;" };
            if (dial.ShowDialog() == true)
            {
                string path = dial.FileName;
                XmlProfile profile = new XmlProfile(path);
                XmlPhone phone;
                XmlMenu menu;

                profile.ImportProfile(path, out phone, out menu);

                new MainWindow(path, phone, menu).Show();
                Close();
            }
        }

        private void ButtonCreateProfile_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }
    }
}
