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
            new MainWindow().Show();
            Close();
        }

        private void ButtonCreateProfile_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }
    }
}
