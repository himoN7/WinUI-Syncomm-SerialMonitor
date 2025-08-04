using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncomm_Serial_Monitor.ViewModels;

namespace Syncomm_Serial_Monitor.Views
{
    public sealed partial class ParsingView : Page
    {
        private SerialMonitorViewModel _viewModel;

        public ParsingView()
        {
            this.InitializeComponent();
            
            // Get the ViewModel from the parent (assuming it's passed or accessible)
            // For now, we'll create a new one for demonstration
            _viewModel = new SerialMonitorViewModel();
            this.DataContext = _viewModel;
        }

        private void ResetTimeRange_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ParsingModel.ResetTimeRange();
        }

        private void ClearParsedData_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ParsingModel.ClearData();
        }

        private async void ExportParsedData_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.ParsingModel.HasData())
            {
                var dialog = new ContentDialog
                {
                    Title = "No Data",
                    Content = "No parsed data to export.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            // Export logic would go here
            // For now, just show a message
            var exportDialog = new ContentDialog
            {
                Title = "Export Parsed Data",
                Content = $"Export {_viewModel.ParsingModel.GetDataCount()} data points?",
                PrimaryButtonText = "Export",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await exportDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // Export implementation would go here
                // This could use the DataProcessingService to export to CSV
            }
        }
    }
} 