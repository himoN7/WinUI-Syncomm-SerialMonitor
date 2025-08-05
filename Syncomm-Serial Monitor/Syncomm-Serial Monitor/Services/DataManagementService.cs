using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Syncomm_Serial_Monitor.Models;

namespace Syncomm_Serial_Monitor.Services
{
    public class DataManagementService : IDisposable
    {
        private readonly ConcurrentQueue<DataGridRow> _dataQueue = new ConcurrentQueue<DataGridRow>();
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task _processingTask;
        
        // UI Display Collections (limited for performance)
        public ObservableCollection<DataGridRow> DisplayRows { get; } = new ObservableCollection<DataGridRow>();
        
        // Complete Storage (for export)
        private readonly List<DataGridRow> _allDataRows = new List<DataGridRow>();
        private readonly object _allDataLock = new object();
        
        // Configuration
        private int _displayRowLimit = 50; // Show only last 50 rows in UI
        private int _batchSize = 50; // Larger batch for better performance under high load
        private int _updateIntervalMs = 50; // Faster processing to keep up with data rate
        
        // Performance monitoring
        private long _totalBytesReceived = 0;
        private long _totalRowsProcessed = 0;
        private DateTime _lastUpdateTime = DateTime.Now;
        
        public DataManagementService(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
            StartBackgroundProcessing();
        }
        
        public int DisplayRowLimit
        {
            get => _displayRowLimit;
            set
            {
                _displayRowLimit = value;
                UpdateDisplayRows();
            }
        }
        
        public int BatchSize
        {
            get => _batchSize;
            set => _batchSize = Math.Max(1, Math.Min(50, value));
        }
        
        public int UpdateIntervalMs
        {
            get => _updateIntervalMs;
            set => _updateIntervalMs = Math.Max(50, Math.Min(1000, value));
        }
        
        public long TotalBytesReceived => _totalBytesReceived;
        public long TotalRowsProcessed => _totalRowsProcessed;
        public int QueueCount => _dataQueue.Count;
        
        public void AddData(DataGridRow dataRow)
        {
            if (dataRow == null) return;
            
            // Add to processing queue (non-blocking)
            _dataQueue.Enqueue(dataRow);
            
            // Update statistics
            Interlocked.Increment(ref _totalRowsProcessed);
            
            // Handle queue overflow if needed
            HandleQueueOverflow();
        }
        
        public void AddData(string data, bool includeTimestamp = true)
        {
            if (string.IsNullOrEmpty(data)) return;
            
            var dataRow = new DataGridRow
            {
                Timestamp = includeTimestamp ? DateTime.Now.ToString("HH:mm:ss.fff") : "",
                Values = ParseDataValues(data)
            };
            
            // Add to processing queue (non-blocking)
            _dataQueue.Enqueue(dataRow);
            
            // Update statistics
            Interlocked.Increment(ref _totalRowsProcessed);
            
            // Handle queue overflow if needed
            HandleQueueOverflow();
        }
        
        private List<string> ParseDataValues(string data)
        {
            if (string.IsNullOrEmpty(data)) return new List<string>();
            
            // Split by spaces to get values (consistent with ParseLineToDataGridRow)
            var parts = data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var values = new List<string>();
            
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
        
        private void StartBackgroundProcessing()
        {
            _processingTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessDataBatch();
                        await Task.Delay(_updateIntervalMs, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing
                        System.Diagnostics.Debug.WriteLine($"Data processing error: {ex.Message}");
                    }
                }
            }, _cancellationTokenSource.Token);
        }
        
        private async Task ProcessDataBatch()
        {
            var batch = new List<DataGridRow>();
            
            // Collect data from queue with longer timeout to process more data
            var startTime = DateTime.Now;
            var maxProcessingTime = 50; // Increased from 10ms to 50ms
            
            while (batch.Count < _batchSize && _dataQueue.TryDequeue(out var dataRow))
            {
                batch.Add(dataRow);
                
                // Prevent infinite loop - timeout after 50ms
                if ((DateTime.Now - startTime).TotalMilliseconds > maxProcessingTime)
                    break;
            }
            
            if (batch.Count == 0) return;
            
            // Update complete storage on background thread
            lock (_allDataLock)
            {
                foreach (var row in batch)
                {
                    _allDataRows.Add(row);
                }
                
                // Memory management - limit total stored data to prevent memory bloat
                if (_allDataRows.Count > 50000) // Keep only last 50k rows
                {
                    int rowsToRemove = _allDataRows.Count - 50000;
                    _allDataRows.RemoveRange(0, rowsToRemove);
                    
                    // Update the processed count to reflect the actual number of rows
                    Interlocked.Add(ref _totalRowsProcessed, -rowsToRemove);
                }
            }
            
            // Update UI on the dispatcher thread with better error handling
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Add all rows at once to reduce UI updates
                    foreach (var row in batch)
                    {
                        if (row != null && !string.IsNullOrEmpty(row.Timestamp))
                        {
                            DisplayRows.Add(row);
                        }
                    }
                    
                    // Keep only the last N rows for display
                    while (DisplayRows.Count > _displayRowLimit)
                    {
                        DisplayRows.RemoveAt(0);
                    }
                    
                    _lastUpdateTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UI update error: {ex.Message}");
                }
            });
        }
        
        private void UpdateDisplayRows()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Keep only the last N rows
                    while (DisplayRows.Count > _displayRowLimit)
                    {
                        DisplayRows.RemoveAt(0);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Display update error: {ex.Message}");
                }
            });
        }
        
        public void ClearData()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                DisplayRows.Clear();
            });
            
            lock (_allDataLock)
            {
                _allDataRows.Clear();
            }
            
            // Clear queue
            while (_dataQueue.TryDequeue(out _)) { }
            
            // Reset statistics
            Interlocked.Exchange(ref _totalBytesReceived, 0);
            Interlocked.Exchange(ref _totalRowsProcessed, 0);
        }
        
        public void ClearQueueAndDisplay()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                DisplayRows.Clear();
            });
            
            // Clear queue but preserve stored data
            while (_dataQueue.TryDequeue(out _)) { }
        }
        
        public List<DataGridRow> GetAllData()
        {
            lock (_allDataLock)
            {
                return new List<DataGridRow>(_allDataRows);
            }
        }
        
        public int GetTotalRowCount()
        {
            lock (_allDataLock)
            {
                return _allDataRows.Count;
            }
        }
        
        public int GetDisplayRowCount()
        {
            return DisplayRows.Count;
        }
        
        public void UpdateBytesReceived(int bytes)
        {
            Interlocked.Add(ref _totalBytesReceived, bytes);
        }
        
        public void HandleQueueOverflow()
        {
            // If queue is getting too large, process more aggressively
            if (_dataQueue.Count > 1000)
            {
                // Increase batch size temporarily
                var originalBatchSize = _batchSize;
                _batchSize = Math.Min(100, _batchSize * 2);
                
                // Process a larger batch immediately
                Task.Run(async () => await ProcessDataBatch());
                
                // Reset batch size after processing
                _batchSize = originalBatchSize;
            }
        }
        
        public void ForceUIUpdate()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Force a UI refresh by triggering property change
                    // This ensures the UI updates even if no new data is added
                    var temp = DisplayRows.Count;
                    DisplayRows.Add(new DataGridRow { Timestamp = "", Values = new List<string> { "" } });
                    DisplayRows.RemoveAt(DisplayRows.Count - 1);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Force UI update error: {ex.Message}");
                }
            });
        }
        
        public void PopulateDisplayFromStorage()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Clear current display
                    DisplayRows.Clear();
                    
                    // Get the last N rows from storage and populate display
                    lock (_allDataLock)
                    {
                        var lastRows = _allDataRows.Skip(Math.Max(0, _allDataRows.Count - _displayRowLimit)).ToList();
                        foreach (var row in lastRows)
                        {
                            if (row != null && !string.IsNullOrEmpty(row.Timestamp))
                            {
                                DisplayRows.Add(row);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Populate display error: {ex.Message}");
                }
            });
        }
        
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _processingTask?.Wait(1000);
            _cancellationTokenSource.Dispose();
        }
    }
    
    // Extension method for DispatcherQueue
    public static class DispatcherQueueExtensions
    {
        public static Task EnqueueAsync(this DispatcherQueue dispatcherQueue, Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            if (!dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetCanceled();
            }
            
            return tcs.Task;
        }
    }
} 