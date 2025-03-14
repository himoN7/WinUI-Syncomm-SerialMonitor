using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;

namespace Syncomm_Serial_Monitor
{
    /// <summary>
    /// Main application window with NavigationView sidebar for navigation.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        bool IsWindows11()
        {
            var version = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(version);
            uint majorVersion = (uint)((v & 0xFFFF000000000000L) >> 48);
            return majorVersion >= 10 && Environment.OSVersion.Version.Build >= 22000; // Windows 11 Build 22000+
        }
        private readonly Dictionary<string, Type> _pages = new()
        {

            { "SerialMonitorPage", typeof(SerialMonitorPage) },
            { "SettingsPage", typeof(SettingsPage) }
        };

        public MainWindow()
        {
           
            this.InitializeComponent();
            App.MainWindow = this;
            
            if (IsWindows11()) {
            this.ExtendsContentIntoTitleBar = true;
               

            }

            // Set Mica backdrop if supported
            if (MicaController.IsSupported())
            {
                this.SystemBackdrop = new MicaBackdrop
                {
                    Kind = MicaKind.BaseAlt
                }; 
            }

            // Register navigation events
            ContentFrame.Navigated += ContentFrame_Navigated;
            
            // Navigate to default page
            ContentFrame.Navigate(typeof(SerialMonitorPage));
        }
   

   
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {

            if (args.SelectedItemContainer is NavigationViewItem selectedItem)
            {
                string pageTag = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(pageTag) && _pages.TryGetValue(pageTag, out Type pageType))
                {
                    LoadingIndicator.IsActive = true;
                    ContentFrame.Navigate(pageType);
                }
                else if (args.IsSettingsSelected) // Detect if Settings is selected
                {
                    ContentFrame.Navigate(typeof(SettingsPage)); // Navigate to the SettingsPage
                }

            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            LoadingIndicator.IsActive = false;
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            // Update selected nav item
            if (e.SourcePageType != null)
            {
                string tagToSelect = _pages.FirstOrDefault(p => p.Value == e.SourcePageType).Key;
                foreach (NavigationViewItem item in NavView.MenuItems.OfType<NavigationViewItem>())
                {
                    if (item.Tag?.ToString() == tagToSelect)
                    {
                        NavView.SelectedItem = item;
                        break;
                    }
                }
            }
        }
    }
}