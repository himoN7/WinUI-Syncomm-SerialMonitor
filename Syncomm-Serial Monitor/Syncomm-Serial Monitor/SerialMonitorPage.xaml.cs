using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using ScottPlot;
using ScottPlot.Plottables;
using Microsoft.UI.Xaml.Media;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Threading;
using System.Collections.Concurrent;
using Syncomm_Serial_Monitor.ViewModels;

namespace Syncomm_Serial_Monitor
{
    public sealed partial class SerialMonitorPage : Page
    {
        private SerialPort _serialPort;
        private StringBuilder dataBuffer = new StringBuilder();
        private bool isPaused = false;
        private bool isConnected = false;
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1;
        private long bytesReceived = 0;
        private long bytesSent = 0;
        private const int MAX_BUFFER_SIZE = 50000; // Reduced buffer size
        private const int MAX_LINES = 1000; // Maximum number of lines to keep
        
        // Thread safety for data updates
        private readonly object updateLock = new object();

        // Smooth processing architecture (from QtSerialMonitor)
        private readonly ConcurrentQueue<string> _dataQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _sendQueue = new ConcurrentQueue<string>();
        private DispatcherTimer _uiUpdateTimer;
        private StringBuilder _processingBuffer = new StringBuilder();
        private StringBuilder _displayBuffer = new StringBuilder();
        private bool _isProcessing = false;
        private bool _autoScrollEnabled = true;
        private bool _timestampEnabled = true;
        private bool _wrapTextEnabled = false;
        private bool _uiInteractionInProgress = false;

        // ScottPlot related fields
        private Plot plot;
        private Scatter receivedDataSeries;
        private Scatter sentDataSeries;
        private List<double> timeData = new List<double>();
        private List<double> receivedData = new List<double>();
        private List<double> sentData = new List<double>();
        private DateTime startTime = DateTime.Now;
        private bool plotInitialized = false;
        private Microsoft.UI.Xaml.Controls.Image plotImage;
        private bool _isRefreshing = false;

        // Advanced async processing for better performance
        private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentQueue<string> _transmissionQueue = new ConcurrentQueue<string>();
        private readonly CancellationTokenSource _processingCancellation = new CancellationTokenSource();
        private Task _processingTask;
        
        // Performance optimization: batch processing
        private const int BATCH_SIZE = 5; // Smaller batch for faster updates
        private const int UI_UPDATE_INTERVAL = 100; // Faster UI updates
        
        // Performance monitoring
        private DateTime _connectionStartTime;
        private bool _isFirstConnection = true;

        // Performance optimizations for first connection
        private bool _isFirstDataUpdate = true;
        private readonly object _textUpdateLock = new object();
        private string _lastDisplayedText = "";
        
        // Virtualized text updates for better performance
        private int _lastTextLength = 0;
        private const int TEXT_UPDATE_THRESHOLD = 100; // Only update if change is significant
        
        // Data models for comparison
        public class DataRow
        {
            public string Timestamp { get; set; } = "";
            public string Data { get; set; } = "";
        }

        public class DataGridRow
        {
            public string Timestamp { get; set; } = "";
            public List<string> Values { get; set; } = new List<string>();
            
            // For backward compatibility
            public string Value1 { get => Values.Count > 0 ? Values[0] : ""; set { if (Values.Count > 0) Values[0] = value; else Values.Add(value); } }
            public string Value2 { get => Values.Count > 1 ? Values[1] : ""; set { if (Values.Count > 1) Values[1] = value; else Values.Add(value); } }
            public string Value3 { get => Values.Count > 2 ? Values[2] : ""; set { if (Values.Count > 2) Values[2] = value; else Values.Add(value); } }
            public string Value4 { get => Values.Count > 3 ? Values[3] : ""; set { if (Values.Count > 3) Values[3] = value; else Values.Add(value); } }
            public string Value5 { get => Values.Count > 4 ? Values[4] : ""; set { if (Values.Count > 4) Values[4] = value; else Values.Add(value); } }
            public string Value6 { get => Values.Count > 5 ? Values[5] : ""; set { if (Values.Count > 5) Values[5] = value; else Values.Add(value); } }
            public string Value7 { get => Values.Count > 6 ? Values[6] : ""; set { if (Values.Count > 6) Values[6] = value; else Values.Add(value); } }
            public string Value8 { get => Values.Count > 7 ? Values[7] : ""; set { if (Values.Count > 7) Values[7] = value; else Values.Add(value); } }
        }

        private ObservableCollection<DataRow> _dataRows = new ObservableCollection<DataRow>();
        private ObservableCollection<DataGridRow> _dataGridRows = new ObservableCollection<DataGridRow>();
        private ObservableCollection<DataGridRow> _itemRepeaterRows = new ObservableCollection<DataGridRow>();
        
        // Complete data storage for export and disconnected viewing
        private List<DataGridRow> _allDataRows = new List<DataGridRow>();
        
        // Reference to ViewModel for data management
        private SerialMonitorViewModel _viewModel;
        private readonly object _allDataLock = new object();
        private bool _isConnected = false;
        
        private StringBuilder _textBlockContent = new StringBuilder();
        private bool _listViewInitialized = false;
        private bool _dataGridInitialized = false;
        private bool _itemRepeaterInitialized = false;
        private bool _dataGridAutoScrollEnabled = true;
        private int _maxColumns = 8; // Track maximum number of columns needed
        
        // DataGrid appearance settings
        private bool _dataGridAlternatingRows = true;
        private bool _dataGridShowBorders = true;
        private bool _dataGridShowGridLines = true;
        private string _dataGridFontFamily = "Consolas";
        private double _dataGridFontSize = 11.0;
        private bool _dataGridRightAlignValues = true;
        private bool _dataGridShowHoverEffects = true;

        public SerialMonitorPage()
        {
            this.InitializeComponent();
            
            // Initialize ViewModel
            _viewModel = new SerialMonitorViewModel();
            this.DataContext = _viewModel;
            
            // Initialize UI components
            InitializeListView();
            InitializeDataGrid();
            InitializeItemRepeater();
            
            // Pre-initialize resources for better performance
            PreInitializeResources();
            
            // Add event handlers
            this.Loaded += SerialMonitorPage_Loaded;
            this.Unloaded += SerialMonitorPage_Unloaded;
        }

        private void InitializeListView()
        {
            try
            {
                var listView = this.FindName("DataListView") as ListView;
                if (listView != null)
                {
                    // Initialize with empty collection
                    listView.ItemsSource = _dataRows;
                    _listViewInitialized = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ListView initialization error: {ex.Message}");
            }
        }

        private void InitializeDataGrid()
        {
            try
            {
                var dataGrid = this.FindName("DataDataGrid") as Grid;
                if (dataGrid != null)
                {
                    // Initialize with empty content
                    UpdateDataGridContent(dataGrid, _dataGridRows);
                    _dataGridInitialized = true;
                }

                // Initialize auto-scroll toggle
                var autoScrollToggle = this.FindName("DataGridAutoScrollToggle") as ToggleSwitch;
                if (autoScrollToggle != null)
                {
                    autoScrollToggle.IsOn = _dataGridAutoScrollEnabled;
                    autoScrollToggle.Toggled += DataGridAutoScrollToggle_Toggled;
                }

                // Initialize scroll viewer
                var scrollViewer = this.FindName("DataGridScrollViewer") as ScrollViewer;
                if (scrollViewer != null)
                {
                    scrollViewer.ViewChanged += DataGridScrollViewer_ViewChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataGrid initialization error: {ex.Message}");
            }
        }

        private void DataGridAutoScrollToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleSwitch;
            if (toggle != null)
            {
                _dataGridAutoScrollEnabled = toggle.IsOn;
                
                // If auto-scroll is enabled, scroll to bottom immediately
                if (_dataGridAutoScrollEnabled)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var scrollViewer = this.FindName("DataGridScrollViewer") as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                        }
                    });
                }
                
                // Show feedback to user
                ShowInfoBar($"Data grid auto-scroll: {(_dataGridAutoScrollEnabled ? "Enabled" : "Disabled")}", 
                    _dataGridAutoScrollEnabled ? InfoBarSeverity.Success : InfoBarSeverity.Informational);
            }
        }

        private void DataGridScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null && _dataGridAutoScrollEnabled)
            {
                // Check if user manually scrolled (not at bottom)
                if (scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight - 10)
                {
                    // User scrolled up, disable auto-scroll
                    _dataGridAutoScrollEnabled = false;
                    
                    // Update toggle switch on UI thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var toggle = this.FindName("DataGridAutoScrollToggle") as ToggleSwitch;
                        if (toggle != null)
                        {
                            toggle.IsOn = false;
                        }
                    });
                }
            }
        }

        private void InitializeItemRepeater()
        {
            try
            {
                var itemRepeater = this.FindName("DataItemRepeater") as ItemsRepeater;
                if (itemRepeater != null)
                {
                    // Initialize with empty collection
                    itemRepeater.ItemsSource = _itemRepeaterRows;
                    _itemRepeaterInitialized = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ItemRepeater initialization error: {ex.Message}");
            }
        }

        private void PreInitializeResources()
        {
            // Pre-allocate buffers to avoid allocation during first connection
            _processingBuffer = new StringBuilder(10000);
            _displayBuffer = new StringBuilder(10000);
            _serialInputBuffer = new StringBuilder(1000);
            
            // Pre-warm the background processing task
            StartBackgroundProcessing();
            
            // Pre-initialize UI components with better warming strategy
            DispatcherQueue.TryEnqueue(() =>
            {
                // Warm up the UI rendering pipeline more thoroughly
                var dataTextBox = this.FindName("DataTextBox") as TextBox;
                if (dataTextBox != null)
                {
                    // More thorough TextBox warming
                    dataTextBox.Text = "Initializing...";
                    dataTextBox.Text = "Ready";
                    dataTextBox.Text = "";
                    
                    // Force a layout pass to warm up rendering
                    dataTextBox.UpdateLayout();
                    
                    // Pre-warm the font rendering
                    dataTextBox.FontFamily = new FontFamily("Consolas");
                    dataTextBox.FontSize = 12;
                }
            });
        }

        private void SerialMonitorPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAvailablePorts();
            InitializeDefaultValues();
            UpdateConnectionState();
            InitializePlot();
            InitializeSmoothProcessing();
        }

        private void SerialMonitorPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Cancel background processing
            _processingCancellation.Cancel();
            
            // Wait for background task to complete
            _processingTask?.Wait(1000); // Wait up to 1 second
            
            // Clean up timers
            _uiUpdateTimer?.Stop();
        }

        private void InitializeSmoothProcessing()
        {
            // Use faster UI updates with async processing
            _uiUpdateTimer = new DispatcherTimer();
            _uiUpdateTimer.Interval = TimeSpan.FromMilliseconds(UI_UPDATE_INTERVAL);
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            _uiUpdateTimer.Start();

            // Background processing is already started in PreInitializeResources
        }

        private void StartBackgroundProcessing()
        {
            _processingTask = Task.Run(async () =>
            {
                while (!_processingCancellation.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Process queued transmissions in background
                        await ProcessQueuedTransmissionsAsync();
                        await Task.Delay(10, _processingCancellation.Token); // 10ms delay
                    }
                    catch (OperationCanceledException)
                    {
                        break;
            }
            catch (Exception ex)
            {
                        System.Diagnostics.Debug.WriteLine($"Background processing error: {ex.Message}");
                    }
                }
            }, _processingCancellation.Token);
        }

        private async Task ProcessQueuedTransmissionsAsync()
        {
            var transmissions = new List<string>();
            
            // Collect transmissions from queue
            while (_transmissionQueue.TryDequeue(out string transmission))
            {
                transmissions.Add(transmission);
            }
            
            if (transmissions.Count > 0)
            {
                // Process in background thread
                var processedData = new StringBuilder();
                foreach (string transmission in transmissions)
                {
                    string timestamp = _timestampEnabled ? $"{DateTime.Now:HH:mm:ss.fff} " : "";
                    string processed = ProcessAndFormatData(transmission);
                    processedData.Append($"{timestamp}{processed}\n");
                }
                
                // Update UI on main thread using DispatcherQueue
                DispatcherQueue.TryEnqueue(() =>
                {
                    lock (updateLock)
                    {
                        _processingBuffer.Append(processedData.ToString());
                    }
                });
            }
        }

        private void ProcessCompleteTransmission(string transmission)
        {
            if (string.IsNullOrEmpty(transmission.Trim())) return;
            
            // Queue for background processing instead of immediate processing
            _transmissionQueue.Enqueue(transmission);
        }

        private void UiUpdateTimer_Tick(object sender, object e)
        {
            // Only update if there's data to display
            if (_processingBuffer.Length > 0 || _displayBuffer.Length > 0)
            {
                // Update display from buffer on UI thread
                UpdateDisplayFromBuffer();
            }
            
            // Also check for any stuck data in the serial buffer
            CheckForStuckData();
        }

        private void UpdateDisplayFromBuffer()
        {
            if (isPaused) return;

            lock (updateLock)
            {
                if (_processingBuffer.Length > 0)
                {
                    // Append to display buffer
                    _displayBuffer.Append(_processingBuffer.ToString());
                    _processingBuffer.Clear();
                    
                    // Update UI on main thread with batching
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            UpdateDisplayText();
                        }
                        catch (Exception ex)
                        {
                            // Log error but don't block processing
                            System.Diagnostics.Debug.WriteLine($"UI update error: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void UpdateDisplayText()
        {
            // This method will be called from the UI thread
            string currentText = _displayBuffer.ToString();
            
            // Update enabled display methods for comparison
            if (IsTextBoxEnabled())
                UpdateTextBox(currentText);
            
            if (IsTextBlockEnabled())
                UpdateTextBlock(currentText);
            
            if (IsListViewEnabled())
                UpdateListView(currentText);
            
            if (IsDataGridEnabled())
                UpdateDataGrid(currentText);
            
            if (IsItemRepeaterEnabled())
                UpdateItemRepeater(currentText);
        }

        private bool IsTextBoxEnabled()
        {
            var checkbox = this.FindName("TextBoxEnabled") as CheckBox;
            return checkbox?.IsChecked == true;
        }

        private bool IsTextBlockEnabled()
        {
            var checkbox = this.FindName("TextBlockEnabled") as CheckBox;
            return checkbox?.IsChecked == true;
        }

        private bool IsListViewEnabled()
        {
            var checkbox = this.FindName("ListViewEnabled") as CheckBox;
            return checkbox?.IsChecked == true;
        }

        private bool IsDataGridEnabled()
        {
            var checkbox = this.FindName("DataGridEnabled") as CheckBox;
            return checkbox?.IsChecked == true;
        }

        private bool IsItemRepeaterEnabled()
        {
            var checkbox = this.FindName("ItemRepeaterEnabled") as CheckBox;
            return checkbox?.IsChecked == true;
        }

        private void UpdateTextBox(string currentText)
        {
            var dataTextBox = this.FindName("DataTextBox") as TextBox;
            if (dataTextBox != null)
            {
                lock (_textUpdateLock)
                {
                    if (currentText != _lastDisplayedText)
                    {
                        if (_isFirstDataUpdate)
                        {
                            dataTextBox.Text = "Ready for data...";
                            _isFirstDataUpdate = false;
                            
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                UpdateTextBoxContent(dataTextBox, currentText);
                            });
                        }
                        else
                        {
                            int textChange = Math.Abs(currentText.Length - _lastTextLength);
                            
                            if (textChange > TEXT_UPDATE_THRESHOLD || currentText.Length < _lastTextLength)
                            {
                                UpdateTextBoxContent(dataTextBox, currentText);
                            }
                            else
                            {
                                AppendToTextBox(dataTextBox, currentText);
                            }
                        }
                    }
                }
            }
        }

        private void UpdateRichTextBlock(string currentText)
        {
            var richTextBlock = this.FindName("DataRichTextBlock") as RichTextBlock;
            if (richTextBlock != null)
            {
                try
                {
                    // Create a new paragraph for each line
                    var paragraph = new Paragraph();
                    var lines = currentText.Split('\n');
                    
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            var run = new Run { Text = line + "\n" };
                            paragraph.Inlines.Add(run);
                        }
                    }
                    
                    richTextBlock.Blocks.Clear();
                    richTextBlock.Blocks.Add(paragraph);
            }
            catch (Exception ex)
            {
                    System.Diagnostics.Debug.WriteLine($"RichTextBlock update error: {ex.Message}");
                }
            }
        }

        private void UpdateTextBlock(string currentText)
        {
            var textBlock = this.FindName("DataTextBlock") as TextBlock;
            var scrollViewer = this.FindName("TextBlockScrollViewer") as ScrollViewer;
            if (textBlock != null)
            {
                try
                {
                    // Simple text update for TextBlock
                    textBlock.Text = currentText;
                    
                    // Auto-scroll to bottom
                    if (scrollViewer != null)
                    {
                        scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"TextBlock update error: {ex.Message}");
                }
            }
        }

        private void UpdateItemRepeater(string currentText)
        {
            var itemRepeater = this.FindName("DataItemRepeater") as ItemsRepeater;
            if (itemRepeater != null)
            {
                try
                {
                    // Parse the text into structured data for ItemRepeater
                    var lines = currentText.Split('\n');
                    var newRows = new List<DataGridRow>();
                    
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            // Parse line into timestamp and values
                            var dataGridRow = ParseLineToDataGridRow(line);
                            if (dataGridRow != null)
                            {
                                newRows.Add(dataGridRow);
                            }
                        }
                    }
                    
                    // Update ItemRepeater on UI thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            if (newRows.Count > 0)
                            {
                                // Initialize ItemRepeater if not done yet
                                if (!_itemRepeaterInitialized)
                                {
                                    itemRepeater.ItemsSource = _itemRepeaterRows;
                                    _itemRepeaterInitialized = true;
                                }
                                
                                // Add new rows to existing collection
                                foreach (var row in newRows)
                                {
                                    // Check if this row already exists to avoid duplicates
                                    if (!_itemRepeaterRows.Any(r => r.Timestamp == row.Timestamp))
                                    {
                                        _itemRepeaterRows.Add(row);
                                    }
                                }
                                
                                // Keep only the last 100 rows to prevent memory issues
                                while (_itemRepeaterRows.Count > 100)
                                {
                                    _itemRepeaterRows.RemoveAt(0);
                                }
                                
                                // Force refresh
                                itemRepeater.ItemsSource = null;
                                itemRepeater.ItemsSource = _itemRepeaterRows;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Silent error handling
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Silent error handling
                }
            }
        }

        private void UpdateListView(string currentText)
        {
            var listView = this.FindName("DataListView") as ListView;
            if (listView != null)
            {
                try
                {
                    // Parse the text into rows for ListView
                    var lines = currentText.Split('\n');
                    var newRows = new List<DataRow>();
                    
                    foreach (var line in lines)
                        {
                            if (!string.IsNullOrEmpty(line.Trim()))
                            {
                            // Extract timestamp if present
                            string timestamp = "";
                            string data = line;
                            
                            if (line.Contains(" "))
                            {
                                var parts = line.Split(new[] { ' ' }, 2);
                                if (parts.Length >= 2 && parts[0].Contains(":"))
                                {
                                    timestamp = parts[0];
                                    data = parts[1];
                                }
                            }
                            
                            newRows.Add(new DataRow 
                            { 
                                Timestamp = timestamp, 
                                Data = data 
                            });
                        }
                    }
                    
                    // Update ListView on UI thread with proper error handling
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            // Only update if we have new data
                            if (newRows.Count > 0)
                            {
                                // Initialize ListView if not done yet
                                if (!_listViewInitialized)
                                {
                                    listView.ItemsSource = _dataRows;
                                    _listViewInitialized = true;
                                }
                                
                                // Add new rows to existing collection
                                foreach (var row in newRows)
                                {
                                    // Check if this row already exists to avoid duplicates
                                    if (!_dataRows.Any(r => r.Timestamp == row.Timestamp && r.Data == row.Data))
                                    {
                                        _dataRows.Add(row);
                                    }
                                }
                                
                                // Keep only the last 100 rows to prevent memory issues
                                while (_dataRows.Count > 100)
                                {
                                    _dataRows.RemoveAt(0);
                                }
                                
                                // Force refresh
                                listView.ItemsSource = null;
                                listView.ItemsSource = _dataRows;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Silent error handling
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Silent error handling
                }
            }
        }

        private void UpdateDataGrid(string currentText)
        {
            if (!IsDataGridEnabled()) return;

            try
            {
                var dataGrid = this.FindName("DataDataGrid") as Grid;
                if (dataGrid == null) return;

                // Parse the current text into DataGrid rows
                var lines = currentText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var newRows = new List<DataGridRow>();

                foreach (var line in lines)
                {
                    var dataGridRow = ParseLineToDataGridRow(line);
                    if (dataGridRow != null)
                    {
                        newRows.Add(dataGridRow);
                    }
                }

                // Send data to ViewModel
                if (_viewModel != null)
                {
                    foreach (var row in newRows)
                    {
                        // Create ViewModel DataRow and DataGridRow
                        var dataRow = new Models.DataRow
                        {
                            Timestamp = row.Timestamp,
                            Data = string.Join(" ", row.Values)
                        };
                        
                        var viewModelDataGridRow = new Models.DataGridRow
                        {
                            Timestamp = row.Timestamp,
                            Values = new List<string>(row.Values)
                        };
                        
                        // Add to ViewModel's collections
                        _viewModel.DataModel.AllDataRows.Add(dataRow);
                        _viewModel.DataModel.AllDataGridRows.Add(viewModelDataGridRow);
                        _viewModel.DataModel.DataRows.Add(dataRow);
                        _viewModel.DataModel.DataGridRows.Add(viewModelDataGridRow);
                    }
                    
                    // Apply RowLimit to UI display
                    _viewModel.DataModel.UpdateUIDisplay();
                }

                // Update the collections
                lock (_dataGridRows)
                {
                    // Add new rows to display collection
                    foreach (var row in newRows)
                    {
                        // Check if this row already exists to avoid duplicates
                        if (!_dataGridRows.Any(existing => existing.Timestamp == row.Timestamp))
                        {
                            _dataGridRows.Add(row);
                        }
                    }

                    // Keep only the last rows based on ViewModel's RowLimit
                    while (_dataGridRows.Count > _viewModel.DataModel.RowLimit)
                    {
                        _dataGridRows.RemoveAt(0);
                    }
                }

                // Store ALL rows in complete data collection
                lock (_allDataLock)
                {
                    foreach (var row in newRows)
                    {
                        // Check if this row already exists to avoid duplicates
                        if (!_allDataRows.Any(existing => existing.Timestamp == row.Timestamp))
                        {
                            _allDataRows.Add(row);
                        }
                    }
                }

                // Update the grid content
                DispatcherQueue.TryEnqueue(() =>
                {
                    // Sync local collections with ViewModel
                    SyncCollectionsWithViewModel();
                    
                    UpdateDataGridContent(dataGrid, _dataGridRows);
                    
                    // Auto-scroll to bottom if enabled
                    if (_dataGridAutoScrollEnabled)
                    {
                        var scrollViewer = this.FindName("DataGridScrollViewer") as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            // Use a small delay to ensure the grid content is updated before scrolling
                            Task.Delay(10).ContinueWith(_ =>
                            {
                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                                });
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataGrid update error: {ex.Message}");
            }
        }

        private DataGridRow ParseLineToDataGridRow(string line)
        {
            try
            {
                var dataGridRow = new DataGridRow();
                
                // Split by spaces to get timestamp and values
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length >= 2) // timestamp + at least one value
                {
                    dataGridRow.Timestamp = parts[0];
                    
                    // Add all values to the dynamic list
                    for (int i = 1; i < parts.Length; i++)
                    {
                        dataGridRow.Values.Add(parts[i]);
                    }
                    
                    // Update maximum columns if needed
                    if (dataGridRow.Values.Count > _maxColumns)
                    {
                        _maxColumns = dataGridRow.Values.Count;
                    }
                }
                else if (parts.Length == 1)
                {
                    // Only timestamp, no values
                    dataGridRow.Timestamp = parts[0];
                }
                
                return dataGridRow;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private List<string> ParseLineToValues(string line)
        {
            var values = new List<string>();
            var parts = line.Split(new[] { ' ', '\t', '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                if (!string.IsNullOrEmpty(trimmedPart))
                {
                    values.Add(trimmedPart);
                }
            }
            
            return values;
        }

        private void SyncCollectionsWithViewModel()
        {
            try
            {
                // Sync local collections with ViewModel's collections
                _dataGridRows.Clear();
                _allDataRows.Clear();
                
                foreach (var viewModelRow in _viewModel.DataModel.DataGridRows)
                {
                    // Convert ViewModel DataGridRow to local DataGridRow
                    var localRow = new DataGridRow
                    {
                        Timestamp = viewModelRow.Timestamp,
                        Values = new List<string>(viewModelRow.Values)
                    };
                    _dataGridRows.Add(localRow);
                }
                
                foreach (var viewModelRow in _viewModel.DataModel.AllDataGridRows)
                {
                    // Convert ViewModel DataGridRow to local DataGridRow
                    var localRow = new DataGridRow
                    {
                        Timestamp = viewModelRow.Timestamp,
                        Values = new List<string>(viewModelRow.Values)
                    };
                    _allDataRows.Add(localRow);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing collections: {ex.Message}");
            }
        }

        private void UpdateDataGridContent(Grid dataGrid, ObservableCollection<DataGridRow> rows)
        {
            try
            {
                // Clear existing content
                dataGrid.Children.Clear();
                dataGrid.RowDefinitions.Clear();
                dataGrid.ColumnDefinitions.Clear();
                
                // Set up column definitions with better spacing
                dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) }); // Time column wider
                for (int i = 0; i < _maxColumns; i++)
                {
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80, GridUnitType.Pixel) });
                }
                
                // Add header row
                dataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // Create header with theme-aware styling
                var headerBorder = new Border
                {
                    Background = GetThemeAwareColor("HeaderBackground"),
                    BorderBrush = _dataGridShowBorders ? GetThemeAwareColor("BorderColor") : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderThickness = _dataGridShowBorders ? new Thickness(0, 0, 0, 1) : new Thickness(0),
                    Padding = new Thickness(8, 6, 8, 6)
                };
                
                var timeHeader = new TextBlock 
                { 
                    Text = "Time", 
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = _dataGridFontSize,
                    FontFamily = new FontFamily(_dataGridFontFamily),
                    Foreground = GetThemeAwareColor("HeaderForeground")
                };
                headerBorder.Child = timeHeader;
                Grid.SetColumn(headerBorder, 0);
                Grid.SetRow(headerBorder, 0);
                dataGrid.Children.Add(headerBorder);
                
                for (int i = 0; i < _maxColumns; i++)
                {
                    var valueHeaderBorder = new Border
                    {
                        Background = GetThemeAwareColor("HeaderBackground"),
                        BorderBrush = _dataGridShowBorders ? GetThemeAwareColor("BorderColor") : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        BorderThickness = _dataGridShowBorders ? new Thickness(0, 0, 0, 1) : new Thickness(0),
                        Padding = new Thickness(8, 6, 8, 6)
                    };
                    
                    var valueHeader = new TextBlock 
                    { 
                        Text = $"Value{i + 1}", 
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = _dataGridFontSize,
                        FontFamily = new FontFamily(_dataGridFontFamily),
                        Foreground = GetThemeAwareColor("HeaderForeground"),
                        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center
                    };
                    valueHeaderBorder.Child = valueHeader;
                    Grid.SetColumn(valueHeaderBorder, i + 1);
                    Grid.SetRow(valueHeaderBorder, 0);
                    dataGrid.Children.Add(valueHeaderBorder);
                }
                
                // Add data rows (last 20 for performance)
                var displayRows = rows.TakeLast(20).ToList();
                for (int rowIndex = 0; rowIndex < displayRows.Count; rowIndex++)
                {
                    var row = displayRows[rowIndex];
                    var gridRow = rowIndex + 1; // +1 because row 0 is header
                    
                    // Add row definition
                    dataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    
                    // Determine row background color (alternating with theme awareness)
                    var rowBackground = _dataGridAlternatingRows && (rowIndex % 2 == 0)
                        ? GetThemeAwareColor("RowBackground1")
                        : GetThemeAwareColor("RowBackground2");
                    
                    // Add timestamp cell with styling
                    var timeCellBorder = new Border
                    {
                        Background = rowBackground,
                        BorderBrush = _dataGridShowBorders ? GetThemeAwareColor("BorderColor") : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        BorderThickness = _dataGridShowBorders ? new Thickness(0, 0, 1, 0) : new Thickness(0),
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    
                    var timeCell = new TextBlock 
                    { 
                        Text = row.Timestamp, 
                        FontSize = _dataGridFontSize,
                        FontFamily = new FontFamily(_dataGridFontFamily),
                        Foreground = GetThemeAwareColor("CellForeground")
                    };
                    timeCellBorder.Child = timeCell;
                    Grid.SetColumn(timeCellBorder, 0);
                    Grid.SetRow(timeCellBorder, gridRow);
                    dataGrid.Children.Add(timeCellBorder);
                    
                    // Add hover effect to timestamp cell
                    AddHoverEffect(timeCellBorder);
                    
                    // Add values with styling
                    for (int i = 0; i < _maxColumns; i++)
                    {
                        var valueCellBorder = new Border
                        {
                            Background = rowBackground,
                            BorderBrush = _dataGridShowBorders ? GetThemeAwareColor("BorderColor") : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                            BorderThickness = _dataGridShowBorders ? new Thickness(0, 0, 1, 0) : new Thickness(0),
                            Padding = new Thickness(8, 4, 8, 4)
                        };
                        
                        var valueText = row.Values.Count > i ? row.Values[i] : "";
                        var valueCell = new TextBlock 
                        { 
                            Text = valueText, 
                            FontSize = _dataGridFontSize,
                            FontFamily = new FontFamily(_dataGridFontFamily),
                            Foreground = GetThemeAwareColor("CellForeground"),
                            HorizontalAlignment = _dataGridRightAlignValues ? Microsoft.UI.Xaml.HorizontalAlignment.Right : Microsoft.UI.Xaml.HorizontalAlignment.Left
                        };
                        valueCellBorder.Child = valueCell;
                        Grid.SetColumn(valueCellBorder, i + 1);
                        Grid.SetRow(valueCellBorder, gridRow);
                        dataGrid.Children.Add(valueCellBorder);
                        
                        // Add hover effect to value cell
                        AddHoverEffect(valueCellBorder);
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent error handling
            }
        }

        private SolidColorBrush GetThemeAwareColor(string colorName)
        {
            // Enhanced theme-aware color selection with better contrast
            var isDarkTheme = this.ActualTheme == ElementTheme.Dark;
            
            return colorName switch
            {
                "HeaderBackground" => isDarkTheme 
                    ? new SolidColorBrush(Microsoft.UI.Colors.DimGray) // Better contrast for dark theme
                    : new SolidColorBrush(Microsoft.UI.Colors.GhostWhite), // Very light for light theme
                "HeaderForeground" => isDarkTheme 
                    ? new SolidColorBrush(Microsoft.UI.Colors.White) // Pure white for dark theme
                    : new SolidColorBrush(Microsoft.UI.Colors.Black), // Pure black for light theme
                "RowBackground1" => isDarkTheme 
                    ? new SolidColorBrush(Microsoft.UI.Colors.Black) // Pure black for dark theme
                    : new SolidColorBrush(Microsoft.UI.Colors.White), // Pure white for light theme
                "RowBackground2" => isDarkTheme 
                    ? new SolidColorBrush(Microsoft.UI.Colors.DarkGray) // Dark gray for dark theme
                    : new SolidColorBrush(Microsoft.UI.Colors.Snow), // Very light for light theme
                "CellForeground" => isDarkTheme 
                    ? new SolidColorBrush(Microsoft.UI.Colors.LightGray) // Light gray for dark theme
                    : new SolidColorBrush(Microsoft.UI.Colors.Black), // Pure black for light theme
                "BorderColor" => isDarkTheme 
                    ? new SolidColorBrush(Microsoft.UI.Colors.Gray) // Medium gray for dark theme
                    : new SolidColorBrush(Microsoft.UI.Colors.LightGray), // Light gray for light theme
                "HoverBackground" => isDarkTheme 
                    ? new SolidColorBrush(Microsoft.UI.Colors.DarkBlue) // Dark blue for dark theme
                    : new SolidColorBrush(Microsoft.UI.Colors.LightBlue), // Light blue for light theme
                _ => new SolidColorBrush(Microsoft.UI.Colors.Black)
            };
        }

        // DataGrid appearance control methods
        public void SetDataGridAlternatingRows(bool enabled)
        {
            _dataGridAlternatingRows = enabled;
            UpdateDataGrid(_lastDisplayedText); // Refresh display
        }

        public void SetDataGridShowBorders(bool enabled)
        {
            _dataGridShowBorders = enabled;
            UpdateDataGrid(_lastDisplayedText); // Refresh display
        }

        public void SetDataGridShowGridLines(bool enabled)
        {
            _dataGridShowGridLines = enabled;
            UpdateDataGrid(_lastDisplayedText); // Refresh display
        }

        public void SetDataGridFontFamily(string fontFamily)
        {
            _dataGridFontFamily = fontFamily;
            UpdateDataGrid(_lastDisplayedText); // Refresh display
        }

        public void SetDataGridFontSize(double fontSize)
        {
            _dataGridFontSize = fontSize;
            UpdateDataGrid(_lastDisplayedText); // Refresh display
        }

        public void SetDataGridRightAlignValues(bool enabled)
        {
            _dataGridRightAlignValues = enabled;
            UpdateDataGrid(_lastDisplayedText); // Refresh display
        }

        public void SetDataGridShowHoverEffects(bool enabled)
        {
            _dataGridShowHoverEffects = enabled;
            UpdateDataGrid(_lastDisplayedText); // Refresh display
        }

        private void AddHoverEffect(Border border)
        {
            if (!_dataGridShowHoverEffects) return;

            border.PointerEntered += (sender, e) =>
            {
                border.Background = GetThemeAwareColor("HoverBackground");
            };

            border.PointerExited += (sender, e) =>
            {
                // Restore original background based on row index
                var rowIndex = Grid.GetRow(border);
                if (rowIndex > 0) // Skip header row
                {
                    var actualRowIndex = rowIndex - 1; // Adjust for header
                    var originalBackground = actualRowIndex % 2 == 0 
                        ? GetThemeAwareColor("RowBackground1")
                        : GetThemeAwareColor("RowBackground2");
                    border.Background = originalBackground;
                }
            };
        }

        private void UpdateTextBoxContent(TextBox textBox, string content)
        {
            textBox.Text = content;
            _lastDisplayedText = content;
            _lastTextLength = content.Length;
            
            // Gentle auto-scroll to end
                if (_autoScrollEnabled)
                {
                    try
                    {
                    textBox.SelectionStart = textBox.Text.Length;
                    textBox.SelectionLength = 0;
                }
                catch
                {
                    // Fallback if scrolling fails
                }
            }
        }

        private void AppendToTextBox(TextBox textBox, string newContent)
        {
            // Only append the new portion
            if (newContent.Length > _lastTextLength)
            {
                string newPortion = newContent.Substring(_lastTextLength);
                textBox.Text += newPortion;
                _lastDisplayedText = newContent;
                _lastTextLength = newContent.Length;
                
                // Gentle auto-scroll to end
                if (_autoScrollEnabled)
                {
                    try
                    {
                        textBox.SelectionStart = textBox.Text.Length;
                        textBox.SelectionLength = 0;
                    }
                    catch
                    {
                        // Fallback if scrolling fails
                    }
                }
            }
        }

        private void ManageProcessingBufferSize()
        {
            // If buffer is too large, remove oldest content
            if (_processingBuffer.Length > MAX_BUFFER_SIZE)
            {
                // Remove the first half of the buffer
                int removeLength = _processingBuffer.Length / 2;
                _processingBuffer.Remove(0, removeLength);
                
                // Also limit by number of lines if needed
                string[] lines = _processingBuffer.ToString().Split('\n');
                if (lines.Length > MAX_LINES)
                {
                    // Keep only the last MAX_LINES lines
                    string[] newLines = lines.Skip(lines.Length - MAX_LINES).ToArray();
                    _processingBuffer.Clear();
                    _processingBuffer.Append(string.Join("\n", newLines));
                }
            }
        }

        private void InitializeDefaultValues()
        {
            // Initialize baud rate options
            BaudRateComboBox.Items.Clear();
            int[] baudRates = { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
            foreach (int rate in baudRates)
            {
                BaudRateComboBox.Items.Add(rate.ToString());
            }
            BaudRateComboBox.SelectedIndex = 4; // 115200

            // Initialize data bits
            DataBitsComboBox.Items.Clear();
            int[] dataBits = { 8, 7, 6, 5 };
            foreach (int bits in dataBits)
            {
                DataBitsComboBox.Items.Add(bits.ToString());
            }
            DataBitsComboBox.SelectedIndex = 0; // 8
        }

        private void LoadAvailablePorts()
        {
            try
            {
                PortComboBox.Items.Clear();
                string[] ports = System.IO.Ports.SerialPort.GetPortNames();
                
                if (ports.Length == 0)
                {
                    ShowInfoBar("No COM ports found. Please check your device connections.", InfoBarSeverity.Warning);
                }
                else
                {
                    foreach (string port in ports)
                    {
                        PortComboBox.Items.Add(port);
                    }
                    
                    // Select the first port if available
                    if (PortComboBox.Items.Count > 0)
                    {
                        PortComboBox.SelectedIndex = 0;
                    }
                    
                    ShowInfoBar($"Found {ports.Length} COM port(s): {string.Join(", ", ports)}", InfoBarSeverity.Informational);
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Error loading ports: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async void RefreshPortsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRefreshing) return; // Prevent multiple rapid clicks
            
            try
            {
                _isRefreshing = true;
                RefreshPortsButton.IsEnabled = false;
                
                // Show refreshing message
                ShowInfoBar("Refreshing available ports...", InfoBarSeverity.Informational);
                
                // Run refresh on background thread
                await Task.Run(() =>
                {
                    // Small delay to ensure UI updates
                    Thread.Sleep(100);
                });
                
                // Refresh ports
                LoadAvailablePorts();
                
                // Show success message
                ShowInfoBar("Port list refreshed successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Error refreshing ports: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                _isRefreshing = false;
                RefreshPortsButton.IsEnabled = true;
            }
        }

        private void UpdateConnectionState()
        {
            if (isConnected)
            {
                ConnectionToggleButton.IsChecked = true;
                PortComboBox.IsEnabled = false;
                BaudRateComboBox.IsEnabled = false;
                DataBitsComboBox.IsEnabled = false;
                RefreshPortsButton.IsEnabled = false;
            }
            else
            {
                ConnectionToggleButton.IsChecked = false;
                PortComboBox.IsEnabled = true;
                BaudRateComboBox.IsEnabled = true;
                DataBitsComboBox.IsEnabled = true;
                RefreshPortsButton.IsEnabled = true;
            }
            
            UpdateConnectionToggleUI();
        }
        
        private void UpdateConnectionToggleUI()
        {
            if (isConnected)
            {
                ConnectionText.Text = "Disconnect";
                ConnectionIcon.Glyph = "\uE8B7"; // Disconnect icon
            }
            else
            {
                ConnectionText.Text = "Connect";
                ConnectionIcon.Glyph = "\uE8B8"; // Connect icon
            }
        }

        private bool TestPortAvailability(string portName)
        {
            try
            {
                using (var testPort = new SerialPort(portName))
                {
                    testPort.Open();
                    testPort.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async void ConnectionToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton == null) return;

            // Temporarily disable the toggle button to prevent rapid clicking
            toggleButton.IsEnabled = false;

            try
            {
                if (isConnected)
                {
                    // Disconnect
                    await DisconnectSerialPort();
                }
                else
                {
                    // Connect
                    var selectedPort = PortComboBox.SelectedItem?.ToString();
                    if (string.IsNullOrEmpty(selectedPort))
                    {
                        ShowInfoBar("Please select a port", InfoBarSeverity.Warning);
                        toggleButton.IsChecked = false;
                        return;
                    }

                    try
                    {
                        _serialPort = new SerialPort(selectedPort);
                        
                        // Configure port settings
                        if (BaudRateComboBox.SelectedItem != null)
                        {
                            var baudRate = int.Parse(BaudRateComboBox.SelectedItem.ToString());
                            _serialPort.BaudRate = baudRate;
                        }
                        else
                        {
                            _serialPort.BaudRate = 9600; // Default
                        }

                        if (DataBitsComboBox.SelectedItem != null)
                        {
                            var dataBits = int.Parse(DataBitsComboBox.SelectedItem.ToString());
                            _serialPort.DataBits = dataBits;
                        }
                        else
                        {
                            _serialPort.DataBits = 8; // Default
                        }

                        _serialPort.Parity = Parity.None;
                        _serialPort.StopBits = StopBits.One;
                        _serialPort.ReadTimeout = 1000;
                        _serialPort.WriteTimeout = 1000;

                        await Task.Run(() => _serialPort.Open());

                        _serialPort.DataReceived += SerialPort_DataReceived;
                        _serialPort.ErrorReceived += SerialPort_ErrorReceived;

                        isConnected = true;
                        _isConnected = true;
                        
                        // Reset display to show only recent data when connected
                        lock (_dataGridRows)
                        {
                            _dataGridRows.Clear();
                            // Add last 50 rows from complete data for display
                            lock (_allDataLock)
                            {
                                var recentRows = _allDataRows.TakeLast(50).ToList();
                                foreach (var row in recentRows)
                                {
                                    _dataGridRows.Add(row);
                                }
                            }
                        }

                        UpdateConnectionState();
                        ShowInfoBar($"Connected to {selectedPort}", InfoBarSeverity.Success);

                        // Optimize for first connection
                        if (_isFirstConnection)
                        {
                            await OptimizeForConnection();
                            _isFirstConnection = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowInfoBar($"Connection failed: {ex.Message}", InfoBarSeverity.Error);
                        isConnected = false;
                        _isConnected = false;
                        toggleButton.IsChecked = false;
                        UpdateConnectionState();
                    }
                }
            }
            finally
            {
                // Re-enable the toggle button
                toggleButton.IsEnabled = true;
            }
        }

        private async Task OptimizeForConnection()
        {
            _connectionStartTime = DateTime.Now;
            
            // Pre-warm the processing pipeline for first connection
            if (_processingTask == null || _processingTask.IsCompleted)
            {
                StartBackgroundProcessing();
                await Task.Delay(50); // Give background task time to start
            }
            
            // Pre-allocate additional buffers for first connection
            if (_processingBuffer.Capacity < 20000)
            {
                _processingBuffer = new StringBuilder(20000);
                _displayBuffer = new StringBuilder(20000);
            }
            
            // Warm up the UI update timer
            if (_uiUpdateTimer == null || !_uiUpdateTimer.IsEnabled)
            {
                InitializeSmoothProcessing();
            }
            
            // Log performance for first connection
            if (_isFirstConnection)
            {
                var warmupTime = DateTime.Now - _connectionStartTime;
                System.Diagnostics.Debug.WriteLine($"First connection warmup time: {warmupTime.TotalMilliseconds}ms");
                _isFirstConnection = false;
            }
        }



        private async Task DisconnectSerialPort()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
                }

                isConnected = false;
                _isConnected = false;
                
                // Show all data when disconnected
                ShowAllDataWhenDisconnected();
                
                UpdateConnectionState();
                ShowInfoBar("Serial port disconnected", InfoBarSeverity.Informational);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Error disconnecting: {ex.Message}", InfoBarSeverity.Error);
                // Ensure toggle button is unchecked on error
                ConnectionToggleButton.IsChecked = false;
                UpdateConnectionState();
            }
        }

        // Enhanced data break detection for board with 8 values
        private StringBuilder _serialInputBuffer = new StringBuilder();
        private readonly object _bufferLock = new object();
        private DateTime _lastDataReceived = DateTime.MinValue;
        private const int DATA_TIMEOUT_MS = 20; // 20ms timeout for complete transmission
        private const int EXPECTED_VALUES = 8; // Expected number of values per transmission

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                // Read data with timeout to prevent blocking
                string data = _serialPort.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                {
                    bytesReceived += data.Length;
                    _lastDataReceived = DateTime.Now;
                    
                    // Process data with enhanced break detection
                    ProcessIncomingDataWithBreakDetection(data);
                    
                    // Update statistics on UI thread
                    DispatcherQueue.TryEnqueue(() => UpdateStatistics());
                }
            }
            catch (Exception ex)
            {
                // Use DispatcherQueue for error display
                DispatcherQueue.TryEnqueue(() => ShowInfoBar($"Data receive error: {ex.Message}", InfoBarSeverity.Error));
            }
        }

        private void ProcessIncomingDataWithBreakDetection(string data)
        {
            lock (_bufferLock)
            {
                // Accumulate data
                _serialInputBuffer.Append(data);
                
                string bufferContent = _serialInputBuffer.ToString();
                
                // Method 1: Check for semicolon-separated complete transmissions
                if (bufferContent.Contains(";"))
                {
                    ProcessSemicolonSeparatedData(bufferContent);
                    return;
                }
                
                // Method 2: Check for newline-separated complete transmissions
                if (bufferContent.Contains("\n") || bufferContent.Contains("\r"))
                {
                    ProcessNewlineSeparatedData(bufferContent);
                    return;
                }
                
                // Method 3: Check for complete 8-value transmissions by counting
                if (IsCompleteTransmission(bufferContent))
                {
                    ProcessCompleteTransmission(bufferContent);
                    _serialInputBuffer.Clear();
                    return;
                }
                
                // Method 4: Check for timeout-based completion
                TimeSpan timeSinceLastData = DateTime.Now - _lastDataReceived;
                if (timeSinceLastData.TotalMilliseconds > DATA_TIMEOUT_MS && !string.IsNullOrEmpty(bufferContent.Trim()))
                {
                    ProcessCompleteTransmission(bufferContent);
                    _serialInputBuffer.Clear();
                }
            }
        }

        private bool IsCompleteTransmission(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;
            
            // Count numeric values in the data
            var numericValues = data.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(part => double.TryParse(part, out _))
                .Count();
            
            // Remove debug logging to prevent UI blocking
            // if (numericValues > 0)
            // {
            //     System.Diagnostics.Debug.WriteLine($"Found {numericValues} numeric values in: '{data}'");
            // }
            
            // Check if we have exactly the expected number of values
            return numericValues >= EXPECTED_VALUES;
        }

        private void ProcessSemicolonSeparatedData(string bufferContent)
        {
            string[] transmissions = bufferContent.Split(';');
            
            // Process all complete transmissions except the last one
            for (int i = 0; i < transmissions.Length - 1; i++)
            {
                string transmission = transmissions[i].Trim();
                if (!string.IsNullOrEmpty(transmission))
                {
                    ProcessCompleteTransmission(transmission);
                }
            }
            
            // Keep the last incomplete transmission in buffer
            string lastTransmission = transmissions[transmissions.Length - 1];
            _serialInputBuffer.Clear();
            _serialInputBuffer.Append(lastTransmission);
        }

        private void ProcessNewlineSeparatedData(string bufferContent)
        {
            string[] lines = bufferContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Process all complete lines except the last one
            for (int i = 0; i < lines.Length - 1; i++)
            {
                string line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    ProcessCompleteTransmission(line);
                }
            }
            
            // Keep the last incomplete line in buffer
            string lastLine = lines[lines.Length - 1];
            _serialInputBuffer.Clear();
            _serialInputBuffer.Append(lastLine);
        }

        private void CheckForStuckData()
        {
            lock (_bufferLock)
            {
                if (_serialInputBuffer.Length > 0)
                {
                    TimeSpan timeSinceLastData = DateTime.Now - _lastDataReceived;
                    
                    // Process stuck data if it's been more than 100ms (increased from 50ms)
                    if (timeSinceLastData.TotalMilliseconds > 100)
                    {
                        string stuckData = _serialInputBuffer.ToString().Trim();
                        if (!string.IsNullOrEmpty(stuckData))
                        {
                            ProcessCompleteTransmission(stuckData);
                            _serialInputBuffer.Clear();
                        }
                    }
                }
            }
        }

        private string ProcessAndFormatData(string data)
        {
            // Apply text processing
            if (_currentTextProcessing == "Trimmed")
            {
                data = data.Trim();
            }
            else if (_currentTextProcessing == "Simplified")
            {
                data = data.Replace("\r", "").Replace("\n", "").Trim();
                while (data.Contains("  "))
                {
                    data = data.Replace("  ", " ");
                }
            }
            
            // Remove trailing semicolon if present
            if (data.EndsWith(";"))
            {
                data = data.TrimEnd(';');
            }
            
            // Format numeric data if it's numeric
            if (IsNumericData(data))
            {
                return FormatNumericData(data);
            }
            
            return data;
        }

        private string GetSerialString(bool clearBuffer = true)
        {
            string output = _serialInputBuffer.ToString();
            
            if (clearBuffer)
            {
                _serialInputBuffer.Clear();
            }
            
            return output;
        }

        private string ProcessReceivedData(string data)
        {
            // Apply selected data format first
            string processed = FormatData(data, _currentDataFormat);
            
            // Apply text processing (like QtSerialMonitor)
            processed = ProcessTextFormatting(processed);
            
            // Handle multi-channel data format (1111; 2222; 333; 444; ... 888;)
            if (processed.Contains(";"))
            {
                // Split by semicolon and format each channel
                string[] channels = processed.Split(';');
                var formattedChannels = new List<string>();
                
                for (int i = 0; i < channels.Length; i++)
                {
                    string channel = channels[i].Trim();
                    if (!string.IsNullOrEmpty(channel))
                    {
                        // Clean up the channel data - remove control characters and extra spaces
                        channel = CleanChannelData(channel);
                        if (!string.IsNullOrEmpty(channel))
                        {
                            formattedChannels.Add(channel);
                        }
                    }
                }
                
                if (formattedChannels.Count > 0)
                {
                    return string.Join("\n", formattedChannels);
                }
            }
            
            // For numeric data (like your board's 8-value format), clean and format
            if (IsNumericData(processed))
            {
                return FormatNumericData(processed);
            }
            
            // For non-channel data, just clean it up
            processed = processed.Replace("\r", "").Replace("\n", "").Trim();
            
            return processed;
        }

        private bool IsNumericData(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;
            
            // Check if the data contains mostly numeric values
            var parts = data.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int numericCount = 0;
            
            foreach (string part in parts)
            {
                if (double.TryParse(part, out _))
                {
                    numericCount++;
                }
            }
            
            // If more than 50% are numeric, consider it numeric data
            return numericCount > 0 && (double)numericCount / parts.Length > 0.5;
        }

        private string FormatNumericData(string data)
        {
            if (string.IsNullOrEmpty(data)) return data;
            
            // Extract and format numeric values
            var numericValues = new List<double>();
            var parts = data.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string part in parts)
            {
                if (double.TryParse(part, out double value))
                {
                    numericValues.Add(value);
                }
            }
            
            // Return formatted numeric values with consistent spacing
            if (numericValues.Count > 0)
            {
                return string.Join(" ", numericValues.Select(v => v.ToString("F2")));
            }
            
            return data.Trim();
        }

        // Clean channel data for better display
        private string CleanChannelData(string channelData)
        {
            if (string.IsNullOrEmpty(channelData)) return channelData;
            
            // Remove control characters that shouldn't be displayed
            string cleaned = channelData
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", " ")
                .Trim();
            
            // Remove multiple spaces
            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }
            
            // Parse numeric values from the channel data
            var numericValues = new List<double>();
            var parts = cleaned.Split(new[] { ' ', '\t', '·' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string part in parts)
            {
                if (double.TryParse(part, out double value))
                {
                    numericValues.Add(value);
                }
            }
            
            // Return formatted numeric values
            if (numericValues.Count > 0)
            {
                return string.Join(" ", numericValues.Select(v => v.ToString("F2")));
            }
            
            return cleaned;
        }

        // QtSerialMonitor parsing techniques
        private void ParseDataUsingQtSerialMonitorTechniques(string inputString)
        {
            if (string.IsNullOrEmpty(inputString)) return;

            // Clear previous parsing results
            _parsedNumericData.Clear();
            _parsedLabels.Clear();
            _parsedTimestamps.Clear();

            // Split into lines (like QtSerialMonitor)
            string[] lines = inputString.Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                .Where(line => !string.IsNullOrEmpty(line.Trim()))
                .ToArray();
            
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line.Trim())) continue;

                // Process each line individually (like QtSerialMonitor)
                ParseSingleLine(line.Trim());
            }
        }

        private void ParseSingleLine(string line)
        {
            // Replace separators with spaces (like QtSerialMonitor)
            string processedLine = _sepSymbols.Replace(line, " ");
            
            // Split by whitespace
            string[] tokens = processedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.None)
                .Where(token => !string.IsNullOrEmpty(token))
                .ToArray();
            
            if (tokens.Length == 0) return;

            DateTime? lineTimestamp = null;
            var lineNumericData = new List<double>();
            var lineLabels = new List<string>();

            // Parse tokens (like QtSerialMonitor)
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];

                // Check for timestamp patterns (like QtSerialMonitor)
                if (lineTimestamp == null)
                {
                    lineTimestamp = ParseTimestamp(token);
                }

                // Check for numeric data (like QtSerialMonitor)
                if (_mainSymbols.IsMatch(token))
                {
                    if (double.TryParse(token, out double numericValue))
                    {
                        lineNumericData.Add(numericValue);
                        
                        // Check for label-value pairs (like QtSerialMonitor)
                        if (i > 0 && !_mainSymbols.IsMatch(tokens[i - 1]))
                        {
                            lineLabels.Add(tokens[i - 1]); // Use previous as label
                        }
                        else if (i == 0)
                        {
                            // Sequential numbers - assign generic labels
                            lineLabels.Add($"Graph {lineNumericData.Count - 1}");
                        }
                    }
                }
            }

            // Add parsed data to global lists
            if (lineNumericData.Count > 0)
            {
                _parsedNumericData.AddRange(lineNumericData);
                _parsedLabels.AddRange(lineLabels);
                
                if (lineTimestamp.HasValue)
                {
                    _parsedTimestamps.Add(lineTimestamp.Value);
                }
                else if (_syncToSystemClock)
                {
                    _parsedTimestamps.Add(DateTime.Now);
                }
            }
        }

        private DateTime? ParseTimestamp(string token)
        {
            // Try different time formats (like QtSerialMonitor)
            foreach (string format in _timeFormats)
            {
                if (DateTime.TryParseExact(token, format, null, System.Globalization.DateTimeStyles.None, out DateTime timestamp))
                {
                    return timestamp;
                }
            }

            // Check for external clock label (like QtSerialMonitor)
            if (_useExternalClock && !string.IsNullOrEmpty(_externalClockLabel) && token == _externalClockLabel)
            {
                // External clock handling would go here
                return DateTime.Now;
            }

            return null;
        }

        // Display parsed data (like QtSerialMonitor)
        private string FormatParsedData()
        {
            if (_parsedNumericData.Count == 0) return "";

            var result = new StringBuilder();
            
            for (int i = 0; i < _parsedNumericData.Count; i++)
            {
                string label = i < _parsedLabels.Count ? _parsedLabels[i] : $"Value {i + 1}";
                double value = _parsedNumericData[i];
                
                result.Append($"{label}: {value:F2}");
                
                if (i < _parsedNumericData.Count - 1)
                {
                    result.Append(" | ");
                }
            }

            return result.ToString();
        }

        // Get parsed data for plotting (like QtSerialMonitor)
        public (List<double> values, List<string> labels, List<DateTime> timestamps) GetParsedData()
        {
            return (_parsedNumericData, _parsedLabels, _parsedTimestamps);
        }

        // CSV parsing (like QtSerialMonitor)
        private void ParseCSV(string inputString, bool useExternalLabel = false, string externalClockLabel = "")
        {
            if (string.IsNullOrEmpty(inputString)) return;

            // Split into lines
            string[] lines = inputString.Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                .Where(line => !string.IsNullOrEmpty(line.Trim()))
                .ToArray();
            
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line.Trim())) continue;

                // Remove quotes (like QtSerialMonitor)
                string processedLine = line.Replace("\"", "");
                
                // Split by comma
                string[] columns = processedLine.Split(',');
                
                if (columns.Length == 0) continue;

                // Parse CSV structure
                for (int i = 0; i < columns.Length; i++)
                {
                    string column = columns[i].Trim();
                    
                    if (string.IsNullOrEmpty(column)) continue;

                    // Check for numeric data
                    if (_mainSymbols.IsMatch(column))
                    {
                        if (double.TryParse(column, out double value))
                        {
                            _parsedNumericData.Add(value);
                            _parsedLabels.Add($"Column {i + 1}");
                        }
                    }
                }

                // Add timestamp
                if (_syncToSystemClock)
                {
                    _parsedTimestamps.Add(DateTime.Now);
                }
            }
        }

        // Control character handling (like QtSerialMonitor)
        private string ControlCharactersVisibleConvert(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Handle \r\n combination first
            if (text.Contains("\r\n"))
            {
                text = text.Replace("\r\n", "\\r\\n\r\n");
            }
            
            // Handle individual control characters
            text = text.Replace("\r", "\\r\r");    // Carriage return
            text = text.Replace("\n", "\\n\n");    // Line feed
            text = text.Replace("\t", "\\t\t");    // Tab
            
            // Only replace spaces with middle dot if they're not part of numeric data
            // This prevents breaking up numbers like "1023.00"
            var parts = text.Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                // Only replace spaces that are not part of numeric data
                if (!_mainSymbols.IsMatch(parts[i]) && !parts[i].Contains("."))
                {
                    parts[i] = parts[i].Replace(" ", "·");
                }
            }
            text = string.Join(" ", parts);
            
            return text;
        }

        private void AddToDisplay(string text, bool isSent = false)
        {
            if (isPaused) return;

            // Enqueue data for processing
            _sendQueue.Enqueue(text);
        }
        


        private void ManageBufferSize()
        {
            // If buffer is too large, remove oldest content
            if (dataBuffer.Length > MAX_BUFFER_SIZE)
            {
                // Remove the first half of the buffer
                int removeLength = dataBuffer.Length / 2;
                dataBuffer.Remove(0, removeLength);
                
                // Also limit by number of lines if needed
                string[] lines = dataBuffer.ToString().Split('\n');
                if (lines.Length > MAX_LINES)
                {
                    // Keep only the last MAX_LINES lines
                    string[] newLines = lines.Skip(lines.Length - MAX_LINES).ToArray();
                    dataBuffer.Clear();
                    dataBuffer.Append(string.Join("\n", newLines));
                }
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ShowInfoBar($"Serial port error: {e.EventType}", InfoBarSeverity.Error);
            });
        }

        private void UpdateStatistics()
        {
            BytesReceivedText.Text = $"Bytes Received: {bytesReceived:N0}";
            BytesSentText.Text = $"Bytes Sent: {bytesSent:N0}";
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendData();
        }

        private void SendHexButton_Click(object sender, RoutedEventArgs e)
        {
            SendHexData();
        }

        private void SendData()
        {
            if (!isConnected || _serialPort == null)
            {
                ShowInfoBar("Not connected to serial port", InfoBarSeverity.Warning);
                return;
            }

            string data = SendTextBox.Text;
            if (string.IsNullOrEmpty(data)) return;

            try
            {
                _serialPort.Write(data);
                bytesSent += data.Length;
                
                // Enqueue sent data for processing (like QtSerialMonitor)
                _sendQueue.Enqueue(data);
                
                // Update statistics on UI thread
                DispatcherQueue.TryEnqueue(() => UpdateStatistics());
                
                // Add to history
                if (!commandHistory.Contains(data))
                {
                    commandHistory.Add(data);
                    if (commandHistory.Count > 100)
                        commandHistory.RemoveAt(0);
                }
                
                SendTextBox.Text = "";
                }
                catch (Exception ex)
                {
                ShowInfoBar($"Failed to send data: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void SendHexData()
        {
            if (!isConnected || _serialPort == null)
            {
                ShowInfoBar("Not connected to serial port", InfoBarSeverity.Warning);
                return;
            }

            string hexData = SendTextBox.Text.Replace(" ", "").Replace("-", "");
            if (string.IsNullOrEmpty(hexData)) return;

            try
            {
                byte[] bytes = new byte[hexData.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexData.Substring(i * 2, 2), 16);
                }
                
                _serialPort.Write(bytes, 0, bytes.Length);
                bytesSent += bytes.Length;
                
                // Enqueue sent hex data for processing
                _sendQueue.Enqueue($"[HEX] {BitConverter.ToString(bytes)}");
                
                // Update statistics on UI thread
                DispatcherQueue.TryEnqueue(() => UpdateStatistics());
                
                SendTextBox.Text = "";
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Failed to send hex data: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void ClearTextButton_Click(object sender, RoutedEventArgs e)
        {
            dataBuffer.Clear();
            //DataTextBox.Text = "";
        }

        private void ShowPlotToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (ShowPlotToggle.IsOn)
            {
                // Expand the plot view expander
                PlotViewExpander.IsExpanded = true;
                ShowInfoBar("Plot view enabled", InfoBarSeverity.Success);
            }
            else
            {
                // Collapse the plot view expander
                PlotViewExpander.IsExpanded = false;
                ShowInfoBar("Plot view disabled", InfoBarSeverity.Informational);
            }
        }

        private void ExportPlotButton_Click(object sender, RoutedEventArgs e)
        {
            if (!plotInitialized) return;

            try
            {
                // Get selected export format
                var selectedFormat = ExportFormatComboBox.SelectedItem as ComboBoxItem;
                string format = selectedFormat?.Content?.ToString() ?? "PNG";

                // Export plot
                string fileName = $"SerialData_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}";
                
                switch (format.ToUpper())
                {
                    case "PNG":
                        plot.SavePng(fileName, 800, 600);
                        break;
                    case "SVG":
                        plot.SaveSvg(fileName, 800, 600);
                        break;
                    default:
                        plot.SavePng(fileName, 800, 600);
                        break;
                }

                ShowInfoBar($"Plot exported as {fileName}", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Export failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void ClearPlotButton_Click(object sender, RoutedEventArgs e)
        {
            if (!plotInitialized) return;

            // Clear all data
            timeData.Clear();
            receivedData.Clear();
            sentData.Clear();

            // Reset start time
            startTime = DateTime.Now;

            // Update plot
            UpdatePlot();
            ShowInfoBar("Plot data cleared", InfoBarSeverity.Informational);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (!plotInitialized) return;

            // Zoom in by 1.5x
            plot.Axes.Zoom(1.5);
            UpdatePlotImage();
            ShowInfoBar("Zoomed in", InfoBarSeverity.Informational);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (!plotInitialized) return;

            // Zoom out by 1.5x
            plot.Axes.Zoom(1.0 / 1.5);
            UpdatePlotImage();
            ShowInfoBar("Zoomed out", InfoBarSeverity.Informational);
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!plotInitialized) return;

            // Reset zoom to auto-scale
            plot.Axes.AutoScale();
            UpdatePlotImage();
            ShowInfoBar("Zoom reset", InfoBarSeverity.Informational);
        }

        private void InitializePlot()
        {
            plot = new Plot();
            
            // Create data series
            receivedDataSeries = plot.Add.Scatter(timeData, receivedData);
            receivedDataSeries.Color = Colors.Blue;
            receivedDataSeries.LineWidth = 2;
            receivedDataSeries.MarkerSize = 0;
            receivedDataSeries.LegendText = "Received Data";

            sentDataSeries = plot.Add.Scatter(timeData, sentData);
            sentDataSeries.Color = Colors.Red;
            sentDataSeries.LineWidth = 2;
            sentDataSeries.MarkerSize = 0;
            sentDataSeries.LegendText = "Sent Data";

            // Configure plot
            plot.ShowLegend();

            // Create initial plot image
            UpdatePlotImage();
            plotInitialized = true;
        }

        private async void UpdatePlotImage()
        {
            if (!plotInitialized) return;

            try
            {
                // Generate plot as PNG image
                byte[] imageBytes = plot.GetImageBytes(800, 600);
                
                // Create a simple bitmap for display
                var writeableBitmap = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(800, 600);
                
                // For now, just show a placeholder
                var grid = PlotContainer.Child as Grid;
                if (grid != null)
                {
                    grid.Children.Clear();
                    grid.Children.Add(new TextBlock 
                    { 
                        Text = "ScottPlot initialized - Data plotting ready",
                        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                        FontSize = 16
                    });
                }
            }
            catch (Exception ex)
            {
                // Fallback to text if plotting fails
                var grid = PlotContainer.Child as Grid;
                if (grid != null)
                {
                    grid.Children.Clear();
                    grid.Children.Add(new TextBlock 
                    { 
                        Text = $"Plot Error: {ex.Message}",
                        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
                    });
                }
            }
        }

        private void AddDataPoint(double value, bool isSent = false)
        {
            if (!plotInitialized) return;

            double timeSeconds = (DateTime.Now - startTime).TotalSeconds;
            
            timeData.Add(timeSeconds);
            
            if (isSent)
            {
                sentData.Add(value);
                receivedData.Add(double.NaN); // No data for received series
            }
            else
            {
                receivedData.Add(value);
                sentData.Add(double.NaN); // No data for sent series
            }

            // Limit data points to prevent memory issues
            const int maxPoints = 1000;
            if (timeData.Count > maxPoints)
            {
                timeData.RemoveAt(0);
                receivedData.RemoveAt(0);
                sentData.RemoveAt(0);
            }

            // Update plot
            UpdatePlot();
        }

        private void UpdatePlot()
        {
            if (!plotInitialized) return;

            // Recreate data series with updated data
            plot.Clear();
            
            receivedDataSeries = plot.Add.Scatter(timeData.ToArray(), receivedData.ToArray());
            receivedDataSeries.Color = Colors.Blue;
            receivedDataSeries.LineWidth = 2;
            receivedDataSeries.MarkerSize = 0;
            receivedDataSeries.LegendText = "Received Data";

            sentDataSeries = plot.Add.Scatter(timeData.ToArray(), sentData.ToArray());
            sentDataSeries.Color = Colors.Red;
            sentDataSeries.LineWidth = 2;
            sentDataSeries.MarkerSize = 0;
            sentDataSeries.LegendText = "Sent Data";

            // Auto-scale axes
            plot.Axes.AutoScale();

            // Update the plot image
            UpdatePlotImage();
        }

        private void ShowInfoBar(string message, InfoBarSeverity severity)
        {
            try
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    // Find the InfoBar in the XAML
                    var infoBar = this.FindName("StatusInfoBar") as InfoBar;
                    if (infoBar != null)
                    {
                        // Update the InfoBar
                        infoBar.Title = severity.ToString();
                        infoBar.Message = message;
                        infoBar.Severity = severity;
                        infoBar.IsOpen = true;
                        
                        // Auto-hide after 5 seconds for non-error messages
                        if (severity != InfoBarSeverity.Error)
                        {
                            _ = Task.Delay(5000).ContinueWith(_ =>
                            {
                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    infoBar.IsOpen = false;
                                });
                            });
                        }
                    }
                    else
                    {
                        // Fallback to debug output if InfoBar not found
                        System.Diagnostics.Debug.WriteLine($"{severity}: {message}");
                    }
                });
            }
            catch (Exception ex)
            {
                // Fallback to console output if InfoBar fails
                System.Diagnostics.Debug.WriteLine($"{severity}: {message} - {ex.Message}");
            }
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear all buffers
            lock (updateLock)
            {
                _processingBuffer.Clear();
                _displayBuffer.Clear();
                _dataQueue.Clear();
                _sendQueue.Clear();
            }
            
            // Clear UI
            var dataTextBox = this.FindName("DataTextBox") as TextBox;
            if (dataTextBox != null)
            {
                dataTextBox.Text = string.Empty;
            }
            
            // Clear all data collections
            lock (_dataGridRows)
            {
                _dataGridRows.Clear();
            }
            
            lock (_allDataLock)
            {
                _allDataRows.Clear();
            }
            
            // Reset counters
            bytesReceived = 0;
            bytesSent = 0;
            UpdateStatistics();
            ShowInfoBar("All data cleared", InfoBarSeverity.Informational);
        }

        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            ExportAllDataToCSV();
        }

        private void CopyDataButton_Click(object sender, RoutedEventArgs e)
        {
            var dataTextBox = this.FindName("DataTextBox") as TextBox;
            if (dataTextBox != null && !string.IsNullOrEmpty(dataTextBox.Text))
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(dataTextBox.Text);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                ShowInfoBar("Data copied to clipboard", InfoBarSeverity.Success);
            }
            else
            {
                ShowInfoBar("No data to copy", InfoBarSeverity.Informational);
            }
        }

        // Show all data when disconnected
        private void ShowAllDataWhenDisconnected()
        {
            lock (_allDataLock)
            {
                if (_allDataRows.Count > 0)
                {
                    // Update the display collection with all data
                    lock (_dataGridRows)
                    {
                        _dataGridRows.Clear();
                        foreach (var row in _allDataRows)
                        {
                            _dataGridRows.Add(row);
                        }
                    }

                    // Update the grid to show all data on UI thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var dataGrid = this.FindName("DataDataGrid") as Grid;
                        if (dataGrid != null)
                        {
                            UpdateDataGridContent(dataGrid, _dataGridRows);
                        }

                        // Auto-scroll to bottom to show the latest data
                        var scrollViewer = this.FindName("DataGridScrollViewer") as ScrollViewer;
                        if (scrollViewer != null)
                        {
                            scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                        }
                    });

                    ShowInfoBar($"Showing all {_allDataRows.Count} rows of data", InfoBarSeverity.Informational);
                }
            }
        }

        // Export all data to CSV
        private async void ExportAllDataToCSV()
        {
            try
            {
                StringBuilder csvContent;
                int rowCount;
                
                lock (_allDataLock)
                {
                    if (_allDataRows.Count == 0)
                    {
                        ShowInfoBar("No data to export", InfoBarSeverity.Informational);
                        return;
                    }

                    rowCount = _allDataRows.Count;
                    csvContent = new StringBuilder();
                    
                    // Add header
                    csvContent.AppendLine("Time," + string.Join(",", Enumerable.Range(1, _maxColumns).Select(i => $"Value{i}")));
                    
                    // Add data rows
                    foreach (var row in _allDataRows)
                    {
                        var values = new List<string> { row.Timestamp };
                        for (int i = 0; i < _maxColumns; i++)
                        {
                            values.Add(row.Values.Count > i ? row.Values[i] : "");
                        }
                        csvContent.AppendLine(string.Join(",", values));
                    }
                }

                // Use .NET-native file saving approach with custom location option
                var fileName = $"SerialData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, fileName);
                
                // Write the CSV content to file
                await File.WriteAllTextAsync(filePath, csvContent.ToString());
                
                // Show enhanced message with bold text and browse option
                var message = $"Exported {rowCount} rows to **{fileName}** in **Documents** folder";
                ShowInfoBar(message, InfoBarSeverity.Success);
                
                // Open the Documents folder to show the file
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
                catch
                {
                    // If explorer fails, just open the Documents folder
                    System.Diagnostics.Process.Start("explorer.exe", documentsPath);
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar($"Export failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        // Get total row count
        public int GetTotalRowCount()
        {
            lock (_allDataLock)
            {
                return _allDataRows.Count;
            }
        }

        // Get display row count
        public int GetDisplayRowCount()
        {
            return _dataGridRows.Count;
        }

        // Toggle event handlers for smooth processing
        private void AutoScrollToggle_Toggled(object sender, RoutedEventArgs e)
        {
            _autoScrollEnabled = AutoScrollToggle.IsOn;
        }

        private void TimestampToggle_Toggled(object sender, RoutedEventArgs e)
        {
            _timestampEnabled = TimestampToggle.IsOn;
        }

        private void WrapTextToggle_Toggled(object sender, RoutedEventArgs e)
        {
            _wrapTextEnabled = WrapTextToggle.IsOn;
        }

        // Data format processing (like QtSerialMonitor)
        private string _currentDataFormat = "ASCII";
        private string _currentTextProcessing = "None"; // None, Trimmed, Simplified
        
        // Parsing fields (like QtSerialMonitor)
        private List<double> _parsedNumericData = new List<double>();
        private List<string> _parsedLabels = new List<string>();
        private List<DateTime> _parsedTimestamps = new List<DateTime>();
        private bool _useExternalClock = false;
        private string _externalClockLabel = "";
        private bool _syncToSystemClock = true;
        
        // Regex patterns (like QtSerialMonitor)
        private static readonly System.Text.RegularExpressions.Regex _mainSymbols = 
            new System.Text.RegularExpressions.Regex(@"[+-]?\d*\.?\d+"); // Float numbers
        private static readonly System.Text.RegularExpressions.Regex _alphanumericSymbols = 
            new System.Text.RegularExpressions.Regex(@"\w+"); // Alphanumeric
        private static readonly System.Text.RegularExpressions.Regex _sepSymbols = 
            new System.Text.RegularExpressions.Regex(@"[=,]"); // Separators
        
        // Time format patterns (like QtSerialMonitor)
        private static readonly string[] _timeFormats = {
            "HH:mm:ss:fff",    // HH:MM:SS:MS
            "HH:mm:ss.fff",    // HH:MM:SS.MS
            "HH:mm:ss.f",      // HH:MM:SS.S
            "HH:mm:ss"         // HH:MM:SS
        };
        
        private string FormatData(string data, string format)
        {
            switch (format?.ToUpper())
            {
                case "HEX":
                    return string.Join(" ", data.Select(c => Convert.ToString(c, 16).PadLeft(2, '0')));
                case "DEC":
                    return string.Join(" ", data.Select(c => ((int)c).ToString()));
                case "BIN":
                    return string.Join(" ", data.Select(c => Convert.ToString(c, 2).PadLeft(8, '0')));
                case "ASCII":
                default:
                    return data;
            }
        }

        private string ProcessTextFormatting(string text)
        {
            switch (_currentTextProcessing)
            {
                case "Trimmed":
                    return text.Trim();
                case "Simplified":
                    return text.Trim().Replace("  ", " "); // Simplify whitespace
                case "None":
                default:
                    return text;
            }
        }

        private void DataFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is RadioButtons radioButtons && radioButtons.SelectedItem is RadioButton selectedButton)
            {
                _currentDataFormat = selectedButton.Content.ToString();
            }
        }

        private void TextProcessing_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentTextProcessing = selectedItem.Content.ToString();
            }
        }

        private void SendTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendData();
                e.Handled = true;
            }
        }
    }
} 