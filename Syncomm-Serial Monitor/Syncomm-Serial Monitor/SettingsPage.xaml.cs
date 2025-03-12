using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI;
using WinRT.Interop;

namespace Syncomm_Serial_Monitor
{
    public sealed partial class SettingsPage : Page
    {
        private Window window;

        public SettingsPage()
        {
            this.InitializeComponent();
            window = App.MainWindow;
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            if (window?.SystemBackdrop is MicaBackdrop)
            {
                BackdropStyleComboBox.SelectedIndex = 2; // Mica
            }
            else if (window?.SystemBackdrop is DesktopAcrylicBackdrop)
            {
                BackdropStyleComboBox.SelectedIndex = 1; // Acrylic
            }
            else
            {
                BackdropStyleComboBox.SelectedIndex = 0; // None
            }
        }

        private void BackdropStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (window == null) return;

            var selectedStyle = BackdropStyleComboBox.SelectedItem as ComboBoxItem;
            if (selectedStyle == null) return;

            switch (selectedStyle.Content.ToString())
            {
                case "None":
                    window.SystemBackdrop = null;
                    break;
                case "Acrylic":
                    window.SystemBackdrop = new DesktopAcrylicBackdrop();
                    break;
                case "Mica":
                    window.SystemBackdrop = new MicaBackdrop();
                    break;
            }
        }
    }
}