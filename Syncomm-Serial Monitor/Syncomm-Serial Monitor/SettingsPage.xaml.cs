using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace Syncomm_Serial_Monitor
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPage_Loaded;
            
            // Add event handlers for DataGrid appearance controls
            DataGridAlternatingRowsToggle.Checked += DataGridAlternatingRowsToggle_Changed;
            DataGridAlternatingRowsToggle.Unchecked += DataGridAlternatingRowsToggle_Changed;
            DataGridShowBordersToggle.Checked += DataGridShowBordersToggle_Changed;
            DataGridShowBordersToggle.Unchecked += DataGridShowBordersToggle_Changed;
            DataGridShowHoverEffectsToggle.Checked += DataGridShowHoverEffectsToggle_Changed;
            DataGridShowHoverEffectsToggle.Unchecked += DataGridShowHoverEffectsToggle_Changed;
            DataGridRightAlignValuesToggle.Checked += DataGridRightAlignValuesToggle_Changed;
            DataGridRightAlignValuesToggle.Unchecked += DataGridRightAlignValuesToggle_Changed;
            DataGridFontFamilyComboBox.SelectionChanged += DataGridFontFamilyComboBox_SelectionChanged;
            DataGridFontSizeNumberBox.ValueChanged += DataGridFontSizeNumberBox_ValueChanged;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadCurrentSettings();
            }
            catch (Exception ex)
            {
                // Handle any errors gracefully
                System.Diagnostics.Debug.WriteLine($"Settings load error: {ex.Message}");
            }
        }

        private void LoadCurrentSettings()
        {
            // Load settings (you can implement settings persistence here)
            AutoScrollToggle.IsChecked = true;
            TimestampToggle.IsChecked = true;
            AutoReconnectToggle.IsChecked = false;
            AutoClearToggle.IsChecked = false;
            SaveLogsToggle.IsChecked = true;
            
            // Set default backdrop to Mica Alt if no saved setting
            var savedBackdropIndex = Windows.Storage.ApplicationData.Current.LocalSettings.Values["BackdropStyleIndex"];
            if (savedBackdropIndex == null)
            {
                // Set default to Mica Alt (index 0)
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["BackdropStyleIndex"] = 0;
            }
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            // Reset to default settings
            AutoScrollToggle.IsChecked = true;
            TimestampToggle.IsChecked = true;
            AutoReconnectToggle.IsChecked = false;
            AutoClearToggle.IsChecked = false;
            SaveLogsToggle.IsChecked = true;
            
            ShowSuccess("Settings reset to defaults");
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Save settings logic here
            ShowSuccess("Settings saved successfully");
        }

        private void ShowInfo(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Information",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            _ = dialog.ShowAsync();
        }

        private void ShowSuccess(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            _ = dialog.ShowAsync();
        }

        // DataGrid appearance event handlers
        private void DataGridAlternatingRowsToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isChecked = DataGridAlternatingRowsToggle.IsChecked ?? true;
            ApplyDataGridSetting("AlternatingRows", isChecked);
        }

        private void DataGridShowBordersToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isChecked = DataGridShowBordersToggle.IsChecked ?? true;
            ApplyDataGridSetting("ShowBorders", isChecked);
        }

        private void DataGridShowHoverEffectsToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isChecked = DataGridShowHoverEffectsToggle.IsChecked ?? true;
            ApplyDataGridSetting("ShowHoverEffects", isChecked);
        }

        private void DataGridRightAlignValuesToggle_Changed(object sender, RoutedEventArgs e)
        {
            var isChecked = DataGridRightAlignValuesToggle.IsChecked ?? true;
            ApplyDataGridSetting("RightAlignValues", isChecked);
        }

        private void DataGridFontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridFontFamilyComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                ApplyDataGridSetting("FontFamily", selectedItem.Content.ToString());
            }
        }

        private void DataGridFontSizeNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                ApplyDataGridSetting("FontSize", args.NewValue);
            }
        }

        private void ApplyDataGridSetting(string settingName, object value)
        {
            try
            {
                // Get the SerialMonitorPage instance from the main window
                if (App.MainWindow?.Content is Frame frame && 
                    frame.Content is SerialMonitorPage serialMonitorPage)
                {
                    switch (settingName)
                    {
                        case "AlternatingRows":
                            serialMonitorPage.SetDataGridAlternatingRows((bool)value);
                            break;
                        case "ShowBorders":
                            serialMonitorPage.SetDataGridShowBorders((bool)value);
                            break;
                        case "ShowHoverEffects":
                            serialMonitorPage.SetDataGridShowHoverEffects((bool)value);
                            break;
                        case "RightAlignValues":
                            serialMonitorPage.SetDataGridRightAlignValues((bool)value);
                            break;
                        case "FontFamily":
                            serialMonitorPage.SetDataGridFontFamily((string)value);
                            break;
                        case "FontSize":
                            serialMonitorPage.SetDataGridFontSize((double)value);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying DataGrid setting: {ex.Message}");
            }
        }
    }
}