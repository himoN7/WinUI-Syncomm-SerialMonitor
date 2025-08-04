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

        public ObservableCollection<DataRow> DataRows { get; set; } = new ObservableCollection<DataRow>();
        public ObservableCollection<DataGridRow> DataGridRows { get; set; } = new ObservableCollection<DataGridRow>();
        public List<DataGridRow> AllDataRows { get; set; } = new List<DataGridRow>();

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
            SendText = "";
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
} 