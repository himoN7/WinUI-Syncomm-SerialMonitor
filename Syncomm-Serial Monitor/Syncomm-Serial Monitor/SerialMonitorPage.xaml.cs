using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO.Ports;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using System.Text;

namespace Syncomm_Serial_Monitor
{
    public sealed partial class SerialMonitorPage : Page
    {
        private SerialPort _serialPort;
        private StringBuilder dataBuffer = new StringBuilder();
        private bool isPaused = false;

        public SerialMonitorPage()
        {
            this.InitializeComponent();
            LoadAvailablePorts();
            InitializeDefaultValues();
        }

        private void InitializeDefaultValues()
        {
            BaudRateSelector.SelectedIndex = 4; // 115200 by default
            DataFormatSelector.SelectedIndex = 0; // Text by default
            LineEndingSelector.SelectedIndex = 1; // New Line by default
            UpdateDisplayBasedOnFormat();
        }

        private void LoadAvailablePorts()
        {
            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                PortSelector.Items.Add(port);
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (PortSelector.SelectedItem != null && BaudRateSelector.SelectedItem != null)
            {
                _serialPort = new SerialPort(PortSelector.SelectedItem.ToString(), int.Parse((BaudRateSelector.SelectedItem as ComboBoxItem).Content.ToString()));
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            isPaused = true;
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            isPaused = false;
            if (_serialPort != null)
            {
                _serialPort.DataReceived += SerialPort_DataReceived;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var data = _serialPort.ReadExisting();
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var displayText = $"[{timestamp}] {data}";

            DispatcherQueue.TryEnqueue(() =>
            {
                DataDisplay.Text += displayText + "\n";
                var scrollViewer = GetScrollViewer(DataDisplay);
                if (scrollViewer != null)
                {
                    scrollViewer.ChangeView(null, scrollViewer.ExtentHeight, null, true);
                }
            });
        }

        private void MyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox != null)
            {
                var scrollViewer = GetScrollViewer(textBox);

                if (scrollViewer != null)
                {
                    scrollViewer.ChangeView(null, scrollViewer.ExtentHeight, null, true);
                }
            }
        }

        private ScrollViewer GetScrollViewer(TextBox textBox)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(textBox); i++)
            {
                var child = VisualTreeHelper.GetChild(textBox, i);

                if (child is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
            }
            return null;
        }

        private void DataFormatSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDisplayBasedOnFormat();
        }

        private void UpdateDisplayBasedOnFormat()
        {
            if (DataDisplay == null || dataBuffer == null) return;

            string rawData = dataBuffer.ToString();
            string formattedData = FormatData(rawData, GetCurrentDataFormat());
            DataDisplay.Text = formattedData;
        }

        private string FormatData(string data, DataFormat format)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            switch (format)
            {
                case DataFormat.Hexadecimal:
                    return BitConverter.ToString(Encoding.UTF8.GetBytes(data)).Replace("-", " ");
                case DataFormat.Binary:
                    StringBuilder binary = new StringBuilder();
                    foreach (byte b in Encoding.UTF8.GetBytes(data))
                    {
                        binary.Append(Convert.ToString(b, 2).PadLeft(8, '0')).Append(" ");
                    }
                    return binary.ToString();
                case DataFormat.Decimal:
                    StringBuilder decimal_ = new StringBuilder();
                    foreach (byte b in Encoding.UTF8.GetBytes(data))
                    {
                        decimal_.Append(b.ToString()).Append(" ");
                    }
                    return decimal_.ToString();
                case DataFormat.Text:
                default:
                    return data;
            }
        }

        private DataFormat GetCurrentDataFormat()
        {
            if (DataFormatSelector == null) return DataFormat.Text;
            return (DataFormat)DataFormatSelector.SelectedIndex;
        }

        private void ClearPlotButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the plot data
            // TODO: Implement plot clearing
        }

        private void ExportPlotButton_Click(object sender, RoutedEventArgs e)
        {
            // Export the plot
            // TODO: Implement plot export
        }
    }

    public enum DataFormat
    {
        Text,
        Hexadecimal,
        Binary,
        Decimal
    }
} 