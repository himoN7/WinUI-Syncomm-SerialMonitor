using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Foundation;

namespace Syncomm_Serial_Monitor
{
    public sealed partial class PlotPage : Page
    {
        private ObservableCollection<DataPoint> _dataPoints;
        private List<string> _parsedData;

        public PlotPage()
        {
            this.InitializeComponent();
            InitializeData();
        }

        private void InitializeData()
        {
            _dataPoints = new ObservableCollection<DataPoint>();
            _parsedData = new List<string>();
        }

        private void ClearPlotButton_Click(object sender, RoutedEventArgs e)
        {
            _dataPoints.Clear();
            _parsedData.Clear();
            PlotStatusText.Text = "Plot cleared";
        }

        private void ExportPlotButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement plot export functionality
            ShowInfo("Plot export functionality coming soon");
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement save image functionality
            ShowInfo("Save image functionality coming soon");
        }

        private void RefreshDataButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement data refresh from SerialMonitorPage
            ShowInfo("Data refresh functionality coming soon");
        }

        private void ExportTableButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement table export functionality
            ShowInfo("Table export functionality coming soon");
        }

        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement data export functionality
            ShowInfo("Data export functionality coming soon");
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement zoom in functionality
            ShowInfo("Zoom in functionality coming soon");
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement zoom out functionality
            ShowInfo("Zoom out functionality coming soon");
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement reset zoom functionality
            ShowInfo("Reset zoom functionality coming soon");
        }

        public void AddDataPoint(DateTime timestamp, double value, string label = "")
        {
            var dataPoint = new DataPoint
            {
                Timestamp = timestamp,
                Value = value,
                Label = string.IsNullOrEmpty(label) ? $"Data {_dataPoints.Count}" : label
            };

            _dataPoints.Add(dataPoint);
            PlotStatusText.Text = $"Data points: {_dataPoints.Count}";
        }

        public void AddParsedData(string data)
        {
            _parsedData.Add(data);
            // TODO: Parse data and add to plot
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
    }

    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string Label { get; set; }
    }
} 