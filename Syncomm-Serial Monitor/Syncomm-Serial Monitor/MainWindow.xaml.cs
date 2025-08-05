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
using Microsoft.UI.Composition;
using WinRT;
using System.Runtime.InteropServices;

namespace Syncomm_Serial_Monitor
{
    /// <summary>
    /// Main application window with modern title bar and navigation.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly Dictionary<string, Type> _pages = new()
        {
            { "SerialMonitorPage", typeof(Views.SerialMonitorView) },
            { "DataAnalysisPage", typeof(PlotPage) },
            { "LogsPage", typeof(Views.SerialMonitorView) }, // Placeholder
            { "SettingsPage", typeof(SettingsPage) }
        };

        private DesktopAcrylicController acrylicController;
        private SystemBackdropConfiguration configurationSource;
        private WindowsSystemDispatcherQueueHelper wsdqHelper;

        public MainWindow()
        {
            this.InitializeComponent();
            
            // Check for saved backdrop setting first
            var savedBackdropIndex = Windows.Storage.ApplicationData.Current.LocalSettings.Values["BackdropStyleIndex"];
            if (savedBackdropIndex != null)
            {
                // Use saved setting
                int index = (int)savedBackdropIndex;
                // Apply the saved backdrop setting
                ApplyBackdropFromIndex(index);
            }
            else
            {
                // Default to Mica Alt instead of Mica Win11
                var micaBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                this.SystemBackdrop = micaBackdrop;
            }
            
            // Set up navigation
            NavView.ItemInvoked += NavView_ItemInvoked;
            NavView.BackRequested += NavView_BackRequested;
            NavView.SelectionChanged += NavView_SelectionChanged;
            ContentFrame.Navigated += ContentFrame_Navigated;
            
            // Navigate to the default page
            ContentFrame.Navigate(typeof(Views.SerialMonitorView));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is NavigationViewItem selectedItem)
            {
                string pageTag = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(pageTag) && _pages.TryGetValue(pageTag, out Type pageType))
                {
                    ContentFrame.Navigate(pageType);
                }
                else if (args.IsSettingsSelected) // Detect if Settings is selected
                {
                    ContentFrame.Navigate(typeof(SettingsPage));
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

        // Public navigation method for other pages
        public void NavigateToPage(Type pageType)
        {
            ContentFrame.Navigate(pageType);
        }

        // Backdrop handling methods
        private void BackdropStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedStyle = sender as ComboBox;
            if (selectedStyle?.SelectedItem is ComboBoxItem selectedItem)
            {
                var content = selectedItem.Content.ToString();
                
                // Save the selected index immediately
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["BackdropStyleIndex"] = selectedStyle.SelectedIndex;
                
                try
                {
                    if (content.StartsWith("Mica Alt"))
                    {
                        var micaBaseAlt = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                        this.SystemBackdrop = micaBaseAlt;
                        DisposeAcrylicController();
                    }
                    else if (content.StartsWith("Mica Win11"))
                    {
                        var micaBase = new MicaBackdrop { Kind = MicaKind.Base };
                        this.SystemBackdrop = micaBase;
                        DisposeAcrylicController();
                    }
                    else if (content.StartsWith("Acrylic Thin"))
                    {
                        if (!TrySetAcrylicBackdrop(true)) // true for thin acrylic
                        {
                            // Fallback to Mica if Acrylic is not supported
                            var micaBase = new MicaBackdrop { Kind = MicaKind.Base };
                            this.SystemBackdrop = micaBase;
                            DisposeAcrylicController();
                        }
                    }
                    else if (content.StartsWith("Acrylic Base"))
                    {
                        if (!TrySetAcrylicBackdrop(false)) // false for base acrylic
                        {
                            // Fallback to Mica if Acrylic is not supported
                            var micaBase = new MicaBackdrop { Kind = MicaKind.Base };
                            this.SystemBackdrop = micaBase;
                            DisposeAcrylicController();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle any errors gracefully
                    System.Diagnostics.Debug.WriteLine($"Backdrop change error: {ex.Message}");
                }
            }
        }

        private bool TrySetAcrylicBackdrop(bool useAcrylicThin)
        {
            if (DesktopAcrylicController.IsSupported())
            {
                DisposeAcrylicController(); // Clean up any existing controller

                wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Create the policy object
                configurationSource = new SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state
                configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                acrylicController = new DesktopAcrylicController();
                acrylicController.Kind = useAcrylicThin ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base;

                // Enable the system backdrop
                acrylicController.AddSystemBackdropTarget(As<ICompositionSupportsSystemBackdrop>(this));
                acrylicController.SetSystemBackdropConfiguration(configurationSource);
                return true; // Succeeded
            }

            return false; // Acrylic is not supported on this system
        }

        private void DisposeAcrylicController()
        {
            if (acrylicController != null)
            {
                acrylicController.Dispose();
                acrylicController = null;
            }
            if (this != null)
            {
                this.Activated -= Window_Activated;
                this.Closed -= Window_Closed;
                if (this.Content is FrameworkElement content)
                {
                    content.ActualThemeChanged -= Window_ThemeChanged;
                }
            }
            configurationSource = null;
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (configurationSource != null)
            {
                configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            DisposeAcrylicController();
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            if (configurationSource != null && this?.Content is FrameworkElement content)
            {
                switch (content.ActualTheme)
                {
                    case ElementTheme.Dark: configurationSource.Theme = SystemBackdropTheme.Dark; break;
                    case ElementTheme.Light: configurationSource.Theme = SystemBackdropTheme.Light; break;
                    case ElementTheme.Default: configurationSource.Theme = SystemBackdropTheme.Default; break;
                }
            }
        }

        private T As<T>(object obj) where T : class
        {
            return obj as T;
        }

        private void ApplyBackdropFromIndex(int index)
        {
            try
            {
                switch (index)
                {
                    case 0: // Mica Alt
                        var micaAlt = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                        this.SystemBackdrop = micaAlt;
                        DisposeAcrylicController();
                        break;
                    case 1: // Mica Win11
                        var micaWin11 = new MicaBackdrop { Kind = MicaKind.Base };
                        this.SystemBackdrop = micaWin11;
                        DisposeAcrylicController();
                        break;
                    case 2: // Acrylic Thin
                        if (!TrySetAcrylicBackdrop(true))
                        {
                            var fallbackMica = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                            this.SystemBackdrop = fallbackMica;
                            DisposeAcrylicController();
                        }
                        break;
                    case 3: // Acrylic Base
                        if (!TrySetAcrylicBackdrop(false))
                        {
                            var fallbackMica = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                            this.SystemBackdrop = fallbackMica;
                            DisposeAcrylicController();
                        }
                        break;
                    default:
                        var defaultMica = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                        this.SystemBackdrop = defaultMica;
                        DisposeAcrylicController();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backdrop application error: {ex.Message}");
            }
        }

        // Navigation event handlers
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
                return;
            }

            if (args.InvokedItemContainer != null)
            {
                var tag = args.InvokedItemContainer.Tag?.ToString();
                if (tag != null && _pages.ContainsKey(tag))
                {
                    ContentFrame.Navigate(_pages[tag]);
                }
            }
        }

        // Window Control Event Handlers
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Minimize the window
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Minimize();
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle maximize/restore
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                if (presenter.IsMaximizable)
                {
                    if (presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
                    {
                        presenter.Restore();
                    }
                    else
                    {
                        presenter.Maximize();
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the application
            Application.Current.Exit();
        }
    }

    // Helper class for Windows System Dispatcher Queue
    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
}