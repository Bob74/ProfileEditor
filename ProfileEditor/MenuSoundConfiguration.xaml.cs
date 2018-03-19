using System.Windows.Forms;
using System.Windows;

namespace ProfileEditor
{
    /// <summary>
    /// Logique d'interaction pour MenuSoundConfiguration.xaml
    /// </summary>
    public partial class MenuSoundConfiguration : Window
    {
        MenuSound Sound;
        bool SoundWasNull = false;

        public MenuSoundConfiguration(ref MenuSound sound)
        {
            InitializeComponent();

            if (sound == null)
            {
                SoundWasNull = true;
                sound = new MenuSound();
            }

            Sound = sound;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxSound.Text = Sound.File;
            SliderSoundVolume.Value = Sound.Volume;
        }

        private void ButtonSoundBrowse_Click(object sender, RoutedEventArgs e)
        {
            string filter = "Wave files|";
            foreach (string ext in MainWindow.SoundFileExtensions)
            {
                filter += "*" + ext + ";";
            }

            Microsoft.Win32.OpenFileDialog dial = new Microsoft.Win32.OpenFileDialog() { InitialDirectory = MainWindow.BaseDir, Filter = filter };
            if (dial.ShowDialog() == true)
                TextBoxSound.Text = dial.FileName;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            Sound.File = TextBoxSound.Text;
            Sound.Volume = (int)SliderSoundVolume.Value;
            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!SoundWasNull)
                DialogResult = true;
            else
                Close();
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show("Do you want to remove the sound from the item?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                Close();
        }

        private void SliderSoundVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextBlockSoundVolume != null)
                TextBlockSoundVolume.Text = ((int)SliderSoundVolume.Value).ToString() + "%";
        }
    }
}
