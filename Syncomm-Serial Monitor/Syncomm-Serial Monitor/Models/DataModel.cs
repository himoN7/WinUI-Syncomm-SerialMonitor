using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Syncomm_Serial_Monitor.Models
{
    public class DataModel : INotifyPropertyChanged
    {
        private string _currentDataFormat = "ASCII";
        private string _currentTextProcessing = "None";
        private bool _timestampEnabled = true;
        private bool _wrapTextEnabled = false;
        private bool _autoScrollEnabled = true;
        private bool _showPlotEnabled = false;
        private string _sendText = "";
        private int _rowLimit = 100;

        public string CurrentDataFormat
        {
            get => _currentDataFormat;
            set => SetProperty(ref _currentDataFormat, value);
        }

        public string CurrentTextProcessing
        {
            get => _currentTextProcessing;
            set => SetProperty(ref _currentTextProcessing, value);
        }

        public bool TimestampEnabled
        {
            get => _timestampEnabled;
            set => SetProperty(ref _timestampEnabled, value);
        }

        public bool WrapTextEnabled
        {
            get => _wrapTextEnabled;
            set => SetProperty(ref _wrapTextEnabled, value);
        }

        public bool AutoScrollEnabled
        {
            get => _autoScrollEnabled;
            set => SetProperty(ref _autoScrollEnabled, value);
        }

        public bool ShowPlotEnabled
        {
            get => _showPlotEnabled;
            set => SetProperty(ref _showPlotEnabled, value);
        }

        public string SendText
        {
            get => _sendText;
            set => SetProperty(ref _sendText, value);
        }

        public int RowLimit
        {
            get => _rowLimit;
            set 
            {
                if (SetProperty(ref _rowLimit, value))
                {
                    // Update UI display when RowLimit changes
                    UpdateUIDisplay();
                }
            }
        }

        // UI Display Collections (limited by RowLimit)
        public ObservableCollection<DataRow> DataRows { get; set; } = new ObservableCollection<DataRow>();
        public ObservableCollection<DataGridRow> DataGridRows { get; set; } = new ObservableCollection<DataGridRow>();
        
        // Complete Storage Collections (for export)
        public List<DataRow> AllDataRows { get; set; } = new List<DataRow>();
        public List<DataGridRow> AllDataGridRows { get; set; } = new List<DataGridRow>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public void ClearData()
        {
            DataRows.Clear();
            DataGridRows.Clear();
            AllDataRows.Clear();
            AllDataGridRows.Clear();
            SendText = "";
        }

        public void UpdateUIDisplay()
        {
            // Update DataRows UI display
            while (DataRows.Count > RowLimit)
            {
                DataRows.RemoveAt(0);
            }
            
            // Update DataGridRows UI display
            while (DataGridRows.Count > RowLimit)
            {
                DataGridRows.RemoveAt(0);
            }
        }
    }

    public class DataRow
    {
        public string Timestamp { get; set; } = "";
        public string Data { get; set; } = "";
    }

    public class DataGridRow
    {
        public string Timestamp { get; set; } = "";
        public List<string> Values { get; set; } = new List<string>();
        public List<string> Labels { get; set; } = new List<string>();
        
        // Dynamic property access for backward compatibility
        public string GetValue(int index) => index < Values.Count ? Values[index] : "";
        public string GetLabel(int index) => index < Labels.Count ? Labels[index] : $"Column {index + 1}";
        
        // For backward compatibility - now dynamic
        public string Value1 { get => GetValue(0); set { SetValue(0, value); } }
        public string Value2 { get => GetValue(1); set { SetValue(1, value); } }
        public string Value3 { get => GetValue(2); set { SetValue(2, value); } }
        public string Value4 { get => GetValue(3); set { SetValue(3, value); } }
        public string Value5 { get => GetValue(4); set { SetValue(4, value); } }
        public string Value6 { get => GetValue(5); set { SetValue(5, value); } }
        public string Value7 { get => GetValue(6); set { SetValue(6, value); } }
        public string Value8 { get => GetValue(7); set { SetValue(7, value); } }
        
        private void SetValue(int index, string value)
        {
            while (Values.Count <= index)
            {
                Values.Add("");
            }
            Values[index] = value;
        }
        
        // Dynamic column count
        public int ColumnCount => Math.Max(Values.Count, Labels.Count);
    }
} 