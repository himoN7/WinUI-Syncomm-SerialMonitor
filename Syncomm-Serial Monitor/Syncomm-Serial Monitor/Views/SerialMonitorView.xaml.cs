using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Syncomm_Serial_Monitor.Models;
using Syncomm_Serial_Monitor.ViewModels;
using System.Collections.ObjectModel;

namespace Syncomm_Serial_Monitor.Views
{
    public sealed partial class SerialMonitorView : Page
    {
        private SerialMonitorViewModel _viewModel;
        private Grid _dataGrid;
        private bool _dataGridInitialized = false;
        private int _maxColumns = 8;

        public SerialMonitorView()
        {
            this.InitializeComponent();
            _viewModel = new SerialMonitorViewModel();
            this.DataContext = _viewModel;
            
            // Subscribe to property changes for smooth updates
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SerialMonitorViewModel.DisplayRows):
                    UpdateDataGrid();
                    break;
                case nameof(SerialMonitorViewModel.TotalBytesReceived):
                case nameof(SerialMonitorViewModel.TotalRowsProcessed):
                case nameof(SerialMonitorViewModel.QueueCount):
                case nameof(SerialMonitorViewModel.DisplayRowCount):
                    // Statistics are automatically updated via binding
                    break;
            }
        }

        private void UpdateDataGrid()
        {
            if (_viewModel?.DisplayRows == null) return;

            if (!_dataGridInitialized)
            {
                InitializeDataGrid();
            }

            // Update the DataGrid content
            UpdateDataGridContent();
        }

        private void InitializeDataGrid()
        {
            if (_dataGrid != null) return;

            _dataGrid = new Grid();
            DataDataGrid.Children.Add(_dataGrid);

            // Create header row
            var headerRow = new RowDefinition { Height = new GridLength(40) };
            _dataGrid.RowDefinitions.Add(headerRow);

            // Create header cells
            CreateHeaderRow();

            _dataGridInitialized = true;
        }

        private void CreateHeaderRow()
        {
            // Clear existing column definitions
            _dataGrid.ColumnDefinitions.Clear();

            // Add timestamp column
            _dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            // Add data columns
            for (int i = 0; i < _maxColumns; i++)
            {
                _dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            }

            // Create header cells
            var timestampHeader = CreateHeaderCell("Timestamp", 0);
            Grid.SetColumn(timestampHeader, 0);
            Grid.SetRow(timestampHeader, 0);
            _dataGrid.Children.Add(timestampHeader);

            for (int i = 0; i < _maxColumns; i++)
            {
                var headerCell = CreateHeaderCell($"Value {i + 1}", i + 1);
                Grid.SetColumn(headerCell, i + 1);
                Grid.SetRow(headerCell, 0);
                _dataGrid.Children.Add(headerCell);
            }
        }

        private Border CreateHeaderCell(string text, int columnIndex)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 240, 240)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = textBlock;
            return border;
        }

        private void UpdateDataGridContent()
        {
            if (_viewModel?.DisplayRows == null) return;

            // Remove existing data rows (keep header)
            var childrenToRemove = new List<UIElement>();
            foreach (var child in _dataGrid.Children)
            {
                if (Grid.GetRow(child) > 0) // Keep header row
                {
                    childrenToRemove.Add(child);
                }
            }

            foreach (var child in childrenToRemove)
            {
                _dataGrid.Children.Remove(child);
            }

            // Update row definitions
            while (_dataGrid.RowDefinitions.Count > 1)
            {
                _dataGrid.RowDefinitions.RemoveAt(1);
            }

            // Add data rows
            for (int i = 0; i < _viewModel.DisplayRows.Count; i++)
            {
                var row = new RowDefinition { Height = new GridLength(30) };
                _dataGrid.RowDefinitions.Add(row);

                var dataRow = _viewModel.DisplayRows[i];
                CreateDataRow(dataRow, i + 1);
            }
        }

        private void CreateDataRow(DataGridRow dataRow, int rowIndex)
        {
            // Timestamp cell
            var timestampCell = CreateDataCell(dataRow.Timestamp, 0, rowIndex);
            _dataGrid.Children.Add(timestampCell);

            // Data cells
            for (int i = 0; i < _maxColumns; i++)
            {
                var value = i < dataRow.Values.Count ? dataRow.Values[i] : "";
                var dataCell = CreateDataCell(value, i + 1, rowIndex);
                _dataGrid.Children.Add(dataCell);
            }
        }

        private Border CreateDataCell(string text, int columnIndex, int rowIndex)
        {
            var border = new Border
            {
                Background = rowIndex % 2 == 0 ? 
                    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)) :
                    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 248, 248)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = textBlock;
            Grid.SetColumn(border, columnIndex);
            Grid.SetRow(border, rowIndex);

            return border;
        }
    }
} 