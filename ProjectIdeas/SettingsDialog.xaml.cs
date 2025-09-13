using System.Windows;

namespace ProjectIdeas
{
    public partial class SettingsDialog : Window
    {
        public int SelectedThemeIndex { get; private set; } = 0;
        public SettingsDialog(int currentThemeIndex)
        {
            InitializeComponent();
            ThemeComboBox.SelectedIndex = currentThemeIndex;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedThemeIndex = ThemeComboBox.SelectedIndex;
            DialogResult = true;
            Close();
        }
    }
}
