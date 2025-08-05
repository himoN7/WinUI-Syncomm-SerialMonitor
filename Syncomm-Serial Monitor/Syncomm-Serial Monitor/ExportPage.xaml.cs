using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Syncomm_Serial_Monitor.Models;


namespace Syncomm_Serial_Monitor
{
    public sealed partial class ExportPage : Page
    {
        private ObservableCollection<DataGridRow> _exportData;
        private List<DataGridRow> _originalData;
        private int _maxColumns = 8;

        public ExportPage()
        {
            this.InitializeComponent();
            _exportData = new ObservableCollection<DataGridRow>();
            ExportDataGrid.ItemsSource = _exportData;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (e.Parameter is List<DataGridRow> data)
            {
                LoadData(data);
            }
        }

        public void LoadData(List<DataGridRow> data)
        {
            _originalData = new List<DataGridRow>(data);
            _exportData.Clear();
            
            foreach (var row in _originalData)
            {
                _exportData.Add(new DataGridRow
                {
                    Timestamp = row.Timestamp,
                    Values = new List<string>(row.Values)
                });
            }

            UpdateRowCount();
        }

        private void UpdateRowCount()
        {
            RowCountTextBlock.Text = $"({_exportData.Count} rows)";
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            ExportDataGrid.SelectAll();
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            ExportDataGrid.SelectedItems.Clear();
        }

        private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ExportDataGrid.SelectedItems.Cast<DataGridRow>().ToList();
            
            foreach (var item in selectedItems)
            {
                _exportData.Remove(item);
            }

            UpdateRowCount();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_exportData.Count == 0)
            {
                ShowMessage("No data to export", "Please add some data before exporting.");
                return;
            }

            var format = (ExportFormatComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "CSV";
            
            try
            {
                string content = format switch
                {
                    "CSV" => ExportToCSV(),
                    "JSON" => ExportToJSON(),
                    "XML" => ExportToXML(),
                    _ => ExportToCSV()
                };

                await SaveFileAsync(content, format);
                ShowMessage("Export Successful", $"Data exported successfully as {format} file.");
            }
            catch (Exception ex)
            {
                ShowMessage("Export Failed", $"Error exporting data: {ex.Message}");
            }
        }

        private string ExportToCSV()
        {
            var csv = new StringBuilder();
            
            // Add header
            csv.AppendLine("Timestamp," + string.Join(",", Enumerable.Range(1, _maxColumns).Select(i => $"Value{i}")));
            
            // Add data rows
            foreach (var row in _exportData)
            {
                var values = new List<string> { row.Timestamp };
                for (int i = 0; i < _maxColumns; i++)
                {
                    values.Add(row.Values.Count > i ? row.Values[i] : "");
                }
                csv.AppendLine(string.Join(",", values));
            }
            
            return csv.ToString();
        }

        private string ExportToJSON()
        {
            var exportData = _exportData.Select(row => new
            {
                timestamp = row.Timestamp,
                values = row.Values.ToArray()
            }).ToList();

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        private string ExportToXML()
        {
            var xmlDoc = new XmlDocument();
            var root = xmlDoc.CreateElement("SerialData");
            xmlDoc.AppendChild(root);

            foreach (var row in _exportData)
            {
                var dataRow = xmlDoc.CreateElement("DataRow");
                
                var timestamp = xmlDoc.CreateElement("Timestamp");
                timestamp.InnerText = row.Timestamp;
                dataRow.AppendChild(timestamp);

                var values = xmlDoc.CreateElement("Values");
                for (int i = 0; i < row.Values.Count; i++)
                {
                    var value = xmlDoc.CreateElement($"Value{i + 1}");
                    value.InnerText = row.Values[i];
                    values.AppendChild(value);
                }
                dataRow.AppendChild(values);
                
                root.AppendChild(dataRow);
            }

            return xmlDoc.OuterXml;
        }

        private async Task SaveFileAsync(string content, string format)
        {
            try
            {
                // Use a simple approach without WinRT APIs
                var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                    $"SerialData_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}");
                
                // For now, save directly to the default path
                // In a real application, you might want to implement a custom file picker
                await File.WriteAllTextAsync(defaultPath, content);
                ShowMessage("Success", $"File saved successfully to:\n{defaultPath}");
            }
            catch (Exception ex)
            {
                ShowMessage("Save Error", $"Error saving file: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to the previous page
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void ShowMessage(string title, string message)
        {
            // Use a simple approach without WinRT APIs
            // In a real application, you might want to implement a custom message dialog
            System.Diagnostics.Debug.WriteLine($"{title}: {message}");
        }
    }
} 