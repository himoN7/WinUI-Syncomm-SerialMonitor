using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncomm_Serial_Monitor.ViewModels;
using System;

namespace Syncomm_Serial_Monitor.Views
{
    public sealed partial class ParsingView : Page
    {
        private SerialMonitorViewModel _viewModel;

        public ParsingView()
        {
            this.InitializeComponent();
            
            // Create ViewModel with proper dispatcher queue
            _viewModel = new SerialMonitorViewModel(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
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

        private void ExportParsedData_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.ParsingModel.HasData())
            {
                // For now, just show a simple message
                // In a real implementation, you would show a proper dialog
                return;
            }

            // Export logic would go here
            // This could use the DataProcessingService to export to CSV
        }
    }
} 