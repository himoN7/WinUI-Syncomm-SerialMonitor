using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Syncomm_Serial_Monitor.Models;
using Syncomm_Serial_Monitor.Services;

namespace Syncomm_Serial_Monitor.ViewModels
{
    public class SerialMonitorViewModel : BaseViewModel
    {
        private readonly SerialPortService _serialPortService;
        private readonly DataProcessingService _dataProcessingService;
        private readonly NotificationService _notificationService;
        private readonly ParsingService _parsingService;
        private readonly DispatcherQueue _dispatcherQueue;

        private bool _isConnected;
        private string _connectionButtonText = "Connect";
        private string _connectionIcon = "\uE8B8"; // Connect icon
        private bool _isRefreshing;
        private string _statusMessage = "";
        private string _statusSeverity = "Informational";

        public SerialMonitorViewModel(DispatcherQueue? dispatcherQueue = null)
        {
            _serialPortService = new SerialPortService();
            _dataProcessingService = new DataProcessingService();
            _notificationService = new NotificationService();
            _parsingService = new ParsingService();
            _dispatcherQueue = dispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

            // Initialize commands
            ConnectCommand = new RelayCommand(Connect, CanConnect);
            RefreshPortsCommand = new RelayCommand(RefreshPorts, CanRefreshPorts);
            SendDataCommand = new RelayCommand(SendData, CanSendData);
            SendHexDataCommand = new RelayCommand(SendHexData, CanSendData);
            ClearDataCommand = new RelayCommand(ClearData);
            CopyDataCommand = new RelayCommand(CopyData);
            ExportDataCommand = new RelayCommand(ExportData);
            ExportPlotCommand = new RelayCommand(ExportPlot, CanExportPlot);
            ClearPlotCommand = new RelayCommand(ClearPlot, CanClearPlot);

            // Initialize models
            SerialPortModel = new SerialPortModel();
            DataModel = new DataModel();
            PlotModel = new PlotModel();
            ParsingModel = new ParsingModel();

            // Subscribe to events
            _serialPortService.DataReceived += OnDataReceived;
            _serialPortService.ErrorReceived += OnErrorReceived;
            _serialPortService.ConnectionStateChanged += OnConnectionStateChanged;
            _parsingService.ParsingCompleted += OnParsingCompleted;
            _parsingService.ProgressUpdated += OnParsingProgressUpdated;

            // Initialize
            RefreshPorts();
        }

        #region Properties

        public SerialPortModel SerialPortModel { get; }
        public DataModel DataModel { get; }
        public PlotModel PlotModel { get; }
        public ParsingModel ParsingModel { get; }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string ConnectionButtonText
        {
            get => _connectionButtonText;
            set => SetProperty(ref _connectionButtonText, value);
        }

        public string ConnectionIcon
        {
            get => _connectionIcon;
            set => SetProperty(ref _connectionIcon, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string StatusSeverity
        {
            get => _statusSeverity;
            set => SetProperty(ref _statusSeverity, value);
        }

        #endregion

        #region Commands

        public ICommand ConnectCommand { get; }
        public ICommand RefreshPortsCommand { get; }
        public ICommand SendDataCommand { get; }
        public ICommand SendHexDataCommand { get; }
        public ICommand ClearDataCommand { get; }
        public ICommand CopyDataCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ExportPlotCommand { get; }
        public ICommand ClearPlotCommand { get; }

        #endregion

        #region Command Implementations

        private async void Connect()
        {
            try
            {
                if (IsConnected)
                {
                    var success = await Disconnect();
                    if (!success)
                    {
                        ShowNotification("Failed to disconnect", "Error");
                    }
                }
                else
                {
                    var success = await ConnectToPort();
                    if (!success)
                    {
                        ShowNotification("Failed to connect", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Connection operation failed: {ex.Message}", "Error");
            }
        }

        private bool CanConnect()
        {
            return !IsRefreshing && !string.IsNullOrEmpty(SerialPortModel.SelectedPort);
        }

        private async void RefreshPorts()
        {
            try
            {
                IsRefreshing = true;
                await Task.Run(() => SerialPortModel.RefreshAvailablePorts());
                ShowNotification("Port list refreshed successfully", "Success");
            }
            catch (Exception ex)
            {
                ShowNotification($"Error refreshing ports: {ex.Message}", "Error");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private bool CanRefreshPorts()
        {
            return !IsRefreshing && !IsConnected;
        }

        private async void SendData()
        {
            if (string.IsNullOrEmpty(DataModel.SendText)) return;

            try
            {
                await _serialPortService.SendDataAsync(DataModel.SendText);
                SerialPortModel.BytesSent += DataModel.SendText.Length;
                DataModel.SendText = "";
                ShowNotification("Data sent successfully", "Success");
            }
            catch (Exception ex)
            {
                ShowNotification($"Failed to send data: {ex.Message}", "Error");
            }
        }

        private async void SendHexData()
        {
            if (string.IsNullOrEmpty(DataModel.SendText)) return;

            try
            {
                await _serialPortService.SendHexDataAsync(DataModel.SendText);
                DataModel.SendText = "";
                ShowNotification("Hex data sent successfully", "Success");
            }
            catch (Exception ex)
            {
                ShowNotification($"Failed to send hex data: {ex.Message}", "Error");
            }
        }

        private bool CanSendData()
        {
            return IsConnected && !string.IsNullOrEmpty(DataModel.SendText);
        }

        private void ClearData()
        {
            DataModel.ClearData();
            SerialPortModel.ResetCounters();
            ShowNotification("All data cleared", "Informational");
        }

        private async void CopyData()
        {
            try
            {
                var csvContent = _dataProcessingService.ExportToCSV(DataModel.AllDataGridRows);
                await _notificationService.CopyToClipboardAsync(csvContent);
                ShowNotification("Data copied to clipboard", "Success");
            }
            catch (Exception ex)
            {
                ShowNotification($"Failed to copy data: {ex.Message}", "Error");
            }
        }

        private async void ExportData()
        {
            try
            {
                var fileName = await _dataProcessingService.ExportToCSVFileAsync(DataModel.AllDataGridRows);
                ShowNotification($"Data exported to {fileName}", "Success");
            }
            catch (Exception ex)
            {
                ShowNotification($"Failed to export data: {ex.Message}", "Error");
            }
        }

        private async void ExportPlot()
        {
            try
            {
                var fileName = await _dataProcessingService.ExportPlotAsync(PlotModel);
                ShowNotification($"Plot exported to {fileName}", "Success");
            }
            catch (Exception ex)
            {
                ShowNotification($"Failed to export plot: {ex.Message}", "Error");
            }
        }

        private bool CanExportPlot()
        {
            return PlotModel.PlotInitialized;
        }

        private void ClearPlot()
        {
            PlotModel.ClearPlotData();
            ShowNotification("Plot data cleared", "Informational");
        }

        private bool CanClearPlot()
        {
            return PlotModel.PlotInitialized;
        }

        #endregion

        #region Private Methods

        private async Task<bool> ConnectToPort()
        {
            try
            {
                var config = new SerialPortConfiguration
                {
                    PortName = SerialPortModel.SelectedPort,
                    BaudRate = SerialPortModel.BaudRate,
                    DataBits = SerialPortModel.DataBits,
                    Parity = SerialPortModel.Parity,
                    StopBits = SerialPortModel.StopBits
                };

                await _serialPortService.ConnectAsync(config);
                return true;
            }
            catch (Exception ex)
            {
                ShowNotification($"Connection failed: {ex.Message}", "Error");
                return false;
            }
        }

        private async Task<bool> Disconnect()
        {
            try
            {
                await _serialPortService.DisconnectAsync();
                return true;
            }
            catch (Exception ex)
            {
                ShowNotification($"Disconnect error: {ex.Message}", "Error");
                return false;
            }
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            SerialPortModel.BytesReceived += e.Data.Length;
            
            var processedData = _dataProcessingService.ProcessData(e.Data, DataModel.CurrentDataFormat);
            var dataRow = _dataProcessingService.CreateDataRow(processedData, DataModel.TimestampEnabled);
            var dataGridRow = _dataProcessingService.CreateDataGridRow(processedData, DataModel.TimestampEnabled);
            
            // Update UI on UI thread using DispatcherQueue
            _dispatcherQueue.TryEnqueue(() =>
            {
                // Add to complete storage (for export)
                DataModel.AllDataRows.Add(dataRow);
                DataModel.AllDataGridRows.Add(dataGridRow);
                
                // Add to UI display collections
                DataModel.DataRows.Add(dataRow);
                DataModel.DataGridRows.Add(dataGridRow);
                
                // Update UI display based on RowLimit
                DataModel.UpdateUIDisplay();

                // Parse data using the parsing service
                _parsingService.Parse(processedData, ParsingModel.SyncToSystemClock, ParsingModel.UseExternalClock, ParsingModel.ExternalClockLabel);

                // Add to plot if enabled
                if (PlotModel.PlotInitialized && DataModel.ShowPlotEnabled)
                {
                    var numericValue = _dataProcessingService.ExtractNumericValue(processedData);
                    if (numericValue.HasValue)
                    {
                        PlotModel.AddDataPoint(numericValue.Value, false);
                    }
                }
            });
        }

        private void OnErrorReceived(object sender, ErrorReceivedEventArgs e)
        {
            ShowNotification($"Serial port error: {e.ErrorType}", "Error");
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            IsConnected = e.IsConnected;
            SerialPortModel.IsConnected = e.IsConnected;
            
            if (e.IsConnected)
            {
                ConnectionButtonText = "Disconnect";
                ConnectionIcon = "\uE8B7"; // Disconnect icon
                SerialPortModel.ConnectionStatus = $"Connected to {e.PortName}";
                ShowNotification($"Connected to {e.PortName}", "Success");
            }
            else
            {
                ConnectionButtonText = "Connect";
                ConnectionIcon = "\uE8B8"; // Connect icon
                SerialPortModel.ConnectionStatus = "Disconnected";
                ShowNotification("Serial port disconnected", "Informational");
            }
        }

        private void ShowNotification(string message, string severity)
        {
            StatusMessage = message;
            StatusSeverity = severity;
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(StatusSeverity));
        }

        private void OnParsingCompleted(object sender, ParsingCompletedEventArgs e)
        {
            // Update parsing model with results on UI thread
            _dispatcherQueue.TryEnqueue(() =>
            {
                ParsingModel.UpdateParsedData(e.Labels, e.NumericData, e.TimeStamps);
                
                // Create dynamic DataGridRow from parsed data
                if (e.Labels.Count > 0 && e.NumericData.Count > 0)
                {
                    var dataGridRow = new DataGridRow
                    {
                        Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                        Labels = e.Labels,
                        Values = e.NumericData.Select(d => d.ToString("F2")).ToList()
                    };
                    
                                         // Add to DataModel for display
                     DataModel.DataGridRows.Add(dataGridRow);
                     DataModel.AllDataGridRows.Add(dataGridRow);
                    
                    // Keep only last rows based on RowLimit for performance
                    while (DataModel.DataGridRows.Count > DataModel.RowLimit)
                    {
                        DataModel.DataGridRows.RemoveAt(0);
                    }
                }
                
                if (ParsingModel.HasData())
                {
                    ShowNotification($"Parsed {ParsingModel.GetDataCount()} data points", "Success");
                }
            });
        }

        private void OnParsingProgressUpdated(object sender, ProgressEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ParsingModel.ParsingProgress = e.Progress;
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _serialPortService?.Dispose();
        }

        #endregion
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
} 