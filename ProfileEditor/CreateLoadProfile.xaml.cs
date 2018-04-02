using System.Windows;
using Microsoft.Win32;

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

                profile.ImportProfile(out phone, out menu);

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
