using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Win32;
using System;
using System.IO;

namespace ProjectIdeas
{
    public enum ThemeOption { System = 0, Light = 1, Dark = 2 }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private const string SettingsFile = "appsettings.json";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Ensure settings exist
            ThemeOption option = LoadThemeSetting();
            ApplyTheme(option);

            var main = new MainWindow();
            main.Show();
        }

        private ThemeOption LoadThemeSetting()
        {
            try
            {
                var p = ProjectIdeas.Properties.Settings.Default;
                int sel = p.ThemeSelection;
                if (sel >= 0 && sel <= 2) return (ThemeOption)sel;
                return ThemeOption.System;
            }
            catch { return ThemeOption.System; }
        }

        public void ApplyTheme(ThemeOption option)
        {
            // Remove any existing theme resources
            var dictToRemove = new System.Collections.Generic.List<ResourceDictionary>();
            foreach (var rd in Resources.MergedDictionaries)
            {
                if (rd.Source != null && rd.Source.OriginalString.Contains("Themes/"))
                    dictToRemove.Add(rd);
            }
            foreach (var rd in dictToRemove)
                Resources.MergedDictionaries.Remove(rd);

            string themeName = "Light.xaml";
            if (option == ThemeOption.Dark) themeName = "Dark.xaml";
            else if (option == ThemeOption.System)
            {
                bool isDark = IsSystemInDarkMode();
                themeName = isDark ? "Dark.xaml" : "Light.xaml";
            }

            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var uri = new Uri($"/" + assemblyName + ";component/Themes/" + themeName, UriKind.Relative);
            var themeDict = new ResourceDictionary() { Source = uri };
            Resources.MergedDictionaries.Add(themeDict);
        }

        private bool IsSystemInDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AppsUseLightTheme");
                        if (val is int intVal)
                        {
                            return intVal == 0; // 0 = dark, 1 = light
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public void SaveThemeSetting(ThemeOption option)
        {
            try
            {
                ProjectIdeas.Properties.Settings.Default.ThemeSelection = (int)option;
                ProjectIdeas.Properties.Settings.Default.Save();
            }
            catch { }
        }
    }
}
