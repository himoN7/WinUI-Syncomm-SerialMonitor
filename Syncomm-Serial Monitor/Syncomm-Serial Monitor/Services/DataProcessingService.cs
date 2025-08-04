using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Syncomm_Serial_Monitor.Models;
using ScottPlot;

namespace Syncomm_Serial_Monitor.Services
{
    public class DataProcessingService
    {
        private static readonly Regex _mainSymbols = new Regex(@"^[+-]?\d*\.?\d+$");
        private static readonly Regex _alphanumericSymbols = new Regex(@"^[a-zA-Z0-9]+$");
        private static readonly Regex _sepSymbols = new Regex(@"[,\t;]");

        public string ProcessData(string data, string format)
        {
            if (string.IsNullOrEmpty(data)) return data;

            // Apply format conversion
            string processed = FormatData(data, format);
            
            // Apply text processing
            processed = ProcessTextFormatting(processed);
            
            return processed;
        }

        public DataRow CreateDataRow(string data, bool includeTimestamp)
        {
            var dataRow = new DataRow();
            
            if (includeTimestamp)
            {
                dataRow.Timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                dataRow.Data = data;
            }
            else
            {
                dataRow.Data = data;
            }
            
            return dataRow;
        }

        public DataGridRow CreateDataGridRow(string data, bool includeTimestamp)
        {
            var dataGridRow = new DataGridRow();
            
            if (includeTimestamp)
            {
                dataGridRow.Timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            }
            
            // Parse data into values
            var parts = data.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                dataGridRow.Values.Add(part);
            }
            
            return dataGridRow;
        }

        public double? ExtractNumericValue(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;

            var parts = data.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                if (double.TryParse(part, out double value))
                {
                    return value;
                }
            }
            
            return null;
        }

        private string FormatData(string data, string format)
        {
            return format switch
            {
                "ASCII" => data,
                "Hex" => ConvertToHex(data),
                "Dec" => ConvertToDecimal(data),
                "Bin" => ConvertToBinary(data),
                _ => data
            };
        }

        private string ConvertToHex(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        private string ConvertToDecimal(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return string.Join(" ", bytes.Select(b => b.ToString()));
        }

        private string ConvertToBinary(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        }

        private string ProcessTextFormatting(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Handle control characters
            text = text.Replace("\r", "\\r\r");
            text = text.Replace("\n", "\\n\n");
            text = text.Replace("\t", "\\t\t");

            // Remove multiple spaces
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            return text.Trim();
        }

        public string ExportToCSV(List<DataGridRow> dataRows)
        {
            if (dataRows == null || dataRows.Count == 0)
                return "";

            var csv = new StringBuilder();
            
            // Find maximum number of columns
            int maxColumns = dataRows.Max(row => row.Values.Count);
            
            // Add header
            csv.Append("Time");
            for (int i = 1; i <= maxColumns; i++)
            {
                csv.Append($",Value{i}");
            }
            csv.AppendLine();
            
            // Add data rows
            foreach (var row in dataRows)
            {
                csv.Append($"\"{row.Timestamp}\"");
                for (int i = 0; i < maxColumns; i++)
                {
                    var value = row.Values.Count > i ? row.Values[i] : "";
                    csv.Append($",\"{value}\"");
                }
                csv.AppendLine();
            }
            
            return csv.ToString();
        }

        public async Task<string> ExportToCSVFileAsync(List<DataGridRow> dataRows)
        {
            var csvContent = ExportToCSV(dataRows);
            
            var fileName = $"SerialData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, fileName);
            
            await File.WriteAllTextAsync(filePath, csvContent);
            
            return fileName;
        }

        public async Task<string> ExportPlotAsync(PlotModel plotModel)
        {
            if (!plotModel.PlotInitialized)
                throw new InvalidOperationException("Plot is not initialized");

            var plot = new Plot();
            
            // Add data series
            var receivedSeries = plot.Add.Scatter(plotModel.TimeData.ToArray(), plotModel.ReceivedData.ToArray());
            receivedSeries.Color = Colors.Blue;
            receivedSeries.LineWidth = 2;
            receivedSeries.LegendText = "Received Data";

            var sentSeries = plot.Add.Scatter(plotModel.TimeData.ToArray(), plotModel.SentData.ToArray());
            sentSeries.Color = Colors.Red;
            sentSeries.LineWidth = 2;
            sentSeries.LegendText = "Sent Data";

            plot.ShowLegend();

            var fileName = $"SerialPlot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, fileName);
            
            plot.SavePng(filePath, 800, 600);
            
            return fileName;
        }
    }
} 