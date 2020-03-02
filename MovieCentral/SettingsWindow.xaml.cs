using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Configuration;

namespace MovieCentral
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static string RED_DEEP = "#642424";
        private static string RED_LIGHT = "#E7B7B7";
        private static string BLUE_DEEP = "#102540";
        private static string BLUE_LIGHT = "#8BB4E2";
        private static string GREEN_DEEP = "#4F6328";
        private static string GREEN_LIGHT = "#D7E3BF";
        private static string GREY_DEEP = "#000000";
        private static string GREY_LIGHT = "#D9D9D9";
        private static string PURPLE_DEEP = "#3F3152";
        private static string PURPLE_LIGHT = "#CDBFD8";
        private static string ORANGE_DEEP = "#984907";
        private static string ORANGE_LIGHT = "#FCD4B1";

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void btnMinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSaveTheme_Click(object sender, RoutedEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    switch (cmbTheme.SelectedValue.ToString())
                    {
                        case "Red" :
                            SetMainWindowElements( RED_LIGHT, RED_DEEP, window);
                            UpdateThemeSetting("theme", "Red");
                            break;

                        case "Green" :
                            SetMainWindowElements( GREEN_LIGHT, GREEN_DEEP, window);
                            UpdateThemeSetting("theme", "Green");
                            break;
                        
                        case "Blue":
                            SetMainWindowElements(BLUE_LIGHT, BLUE_DEEP, window);
                            UpdateThemeSetting("theme", "Blue");
                            break;

                        case "Grey":
                            SetMainWindowElements(GREY_LIGHT, GREY_DEEP, window);
                            UpdateThemeSetting("theme", "Grey");
                            break;
                        
                        case "Purple":
                            SetMainWindowElements(PURPLE_LIGHT, PURPLE_DEEP, window);
                            UpdateThemeSetting("theme", "Purple");
                            break;

                        case "Orange":
                            SetMainWindowElements(ORANGE_LIGHT, ORANGE_DEEP, window);
                            UpdateThemeSetting("theme", "Orange");
                            break;
                    }
                    
                }
            }            
        }

        private void UpdateThemeSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = value;
            configuration.Save(ConfigurationSaveMode.Modified);  

            ConfigurationManager.RefreshSection("appSettings");
        }

        

        private void SetMainWindowElements(string lightColor, string deepColor, Window window)
        {
            (window as MainWindow).txtTitle.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(lightColor));
            (window as MainWindow).txtTagline.Foreground = (window as MainWindow).txtTitle.Foreground;
            (window as MainWindow).txtRating.Foreground = (window as MainWindow).txtTitle.Foreground;
            (window as MainWindow).txtYear.Foreground = (window as MainWindow).txtTitle.Foreground;
            (window as MainWindow).txtOverview.Foreground = (window as MainWindow).txtTitle.Foreground;
            (window as MainWindow).tbCastLabel.Foreground = (window as MainWindow).txtTitle.Foreground;
            (window as MainWindow).tbDirectorLabel.Foreground = (window as MainWindow).txtTitle.Foreground;
            foreach (TextBlock element in (window as MainWindow).spGenre.Children)
            {
                element.Foreground = (window as MainWindow).txtTitle.Foreground;
            }
            foreach (TextBlock element in (window as MainWindow).wpCast.Children)
            {
                element.Foreground = (window as MainWindow).txtTitle.Foreground;
            }
            foreach (TextBlock element in (window as MainWindow).wpDirector.Children)
            {
                element.Foreground = (window as MainWindow).txtTitle.Foreground;
            }

            (window as MainWindow).rectTopLeft.Fill = (window as MainWindow).txtTitle.Foreground;
            (window as MainWindow).rectHeaderBar.Fill = (window as MainWindow).txtTitle.Foreground;

            (window as MainWindow).Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(deepColor));
            (window as MainWindow).txtSearchBox.Background = (window as MainWindow).Background;
            (window as MainWindow).rectSearchBar.Fill = (window as MainWindow).Background;
            (window as MainWindow).lblSearchOnline.Background = (window as MainWindow).Background;
        }
    }
}
