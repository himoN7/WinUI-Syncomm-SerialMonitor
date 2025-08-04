using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncomm_Serial_Monitor.ViewModels;
using System.Threading.Tasks;

namespace Syncomm_Serial_Monitor.Views
{
    public sealed partial class SerialMonitorView : Page
    {
        private SerialMonitorViewModel _viewModel;

        public SerialMonitorView()
        {
            this.InitializeComponent();
            
            // Initialize ViewModel
            _viewModel = new SerialMonitorViewModel();
            this.DataContext = _viewModel;
            
            // Subscribe to property changes for InfoBar updates
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.StatusMessage) || e.PropertyName == nameof(_viewModel.StatusSeverity))
            {
                UpdateInfoBar();
            }
        }

        private void UpdateInfoBar()
        {
            if (!string.IsNullOrEmpty(_viewModel.StatusMessage))
            {
                StatusInfoBar.Title = _viewModel.StatusSeverity;
                StatusInfoBar.Message = _viewModel.StatusMessage;
                StatusInfoBar.Severity = GetInfoBarSeverity(_viewModel.StatusSeverity);
                StatusInfoBar.IsOpen = true;
                
                // Auto-hide after 5 seconds for non-error messages
                if (_viewModel.StatusSeverity != "Error")
                {
                    _ = Task.Delay(5000).ContinueWith(_ =>
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            StatusInfoBar.IsOpen = false;
                        });
                    });
                }
            }
        }

        private Microsoft.UI.Xaml.Controls.InfoBarSeverity GetInfoBarSeverity(string severity)
        {
            return severity switch
            {
                "Error" => Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error,
                "Warning" => Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                "Success" => Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success,
                _ => Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational
            };
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            
            // Clean up ViewModel
            _viewModel?.Dispose();
        }
    }
} 