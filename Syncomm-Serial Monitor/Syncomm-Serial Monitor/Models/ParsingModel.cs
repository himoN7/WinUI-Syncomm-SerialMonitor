using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Syncomm_Serial_Monitor.Models
{
    public class ParsingModel : INotifyPropertyChanged
    {
        private bool _syncToSystemClock = true;
        private bool _useExternalClock = false;
        private string _externalClockLabel = "";
        private bool _useExternalLabel = false;
        private TimeSpan _minimumTime = TimeSpan.Zero;
        private TimeSpan _maximumTime = TimeSpan.Zero;
        private bool _canReportProgress = false;
        private float _parsingProgress = 0.0f;
        private bool _isParsing = false;

        // Parsing results
        private List<string> _labels = new List<string>();
        private List<double> _numericData = new List<double>();
        private List<long> _timeStamps = new List<long>();
        private List<string> _textStorage = new List<string>();

        // Observable collections for UI binding
        public ObservableCollection<ParsedDataItem> ParsedDataItems { get; set; } = new ObservableCollection<ParsedDataItem>();

        #region Properties

        public bool SyncToSystemClock
        {
            get => _syncToSystemClock;
            set => SetProperty(ref _syncToSystemClock, value);
        }

        public bool UseExternalClock
        {
            get => _useExternalClock;
            set => SetProperty(ref _useExternalClock, value);
        }

        public string ExternalClockLabel
        {
            get => _externalClockLabel;
            set => SetProperty(ref _externalClockLabel, value);
        }

        public bool UseExternalLabel
        {
            get => _useExternalLabel;
            set => SetProperty(ref _useExternalLabel, value);
        }

        public TimeSpan MinimumTime
        {
            get => _minimumTime;
            set => SetProperty(ref _minimumTime, value);
        }

        public TimeSpan MaximumTime
        {
            get => _maximumTime;
            set => SetProperty(ref _maximumTime, value);
        }

        public bool CanReportProgress
        {
            get => _canReportProgress;
            set => SetProperty(ref _canReportProgress, value);
        }

        public float ParsingProgress
        {
            get => _parsingProgress;
            set => SetProperty(ref _parsingProgress, value);
        }

        public bool IsParsing
        {
            get => _isParsing;
            set => SetProperty(ref _isParsing, value);
        }

        #endregion

        #region Data Access

        public List<string> Labels
        {
            get => _labels;
            set => SetProperty(ref _labels, value);
        }

        public List<double> NumericData
        {
            get => _numericData;
            set => SetProperty(ref _numericData, value);
        }

        public List<long> TimeStamps
        {
            get => _timeStamps;
            set => SetProperty(ref _timeStamps, value);
        }

        public List<string> TextStorage
        {
            get => _textStorage;
            set => SetProperty(ref _textStorage, value);
        }

        #endregion

        #region Methods

        public void UpdateParsedData(List<string> labels, List<double> numericData, List<long> timeStamps)
        {
            Labels = labels ?? new List<string>();
            NumericData = numericData ?? new List<double>();
            TimeStamps = timeStamps ?? new List<long>();

            // Update observable collection for UI
            UpdateParsedDataItems();
        }

        public void UpdateParsedDataItems()
        {
            ParsedDataItems.Clear();
            
            for (int i = 0; i < Math.Min(Labels.Count, NumericData.Count); i++)
            {
                var item = new ParsedDataItem
                {
                    Label = i < Labels.Count ? Labels[i] : $"Data {i}",
                    Value = i < NumericData.Count ? NumericData[i] : 0,
                    TimeStamp = i < TimeStamps.Count ? TimeStamps[i] : 0,
                    Index = i
                };
                
                ParsedDataItems.Add(item);
            }
        }

        public void ClearData()
        {
            Labels.Clear();
            NumericData.Clear();
            TimeStamps.Clear();
            TextStorage.Clear();
            ParsedDataItems.Clear();
            ParsingProgress = 0.0f;
            IsParsing = false;
        }

        public void SetTimeRange(TimeSpan minTime, TimeSpan maxTime)
        {
            MinimumTime = minTime;
            MaximumTime = maxTime;
        }

        public void ResetTimeRange()
        {
            MinimumTime = TimeSpan.Zero;
            MaximumTime = TimeSpan.Zero;
        }

        public bool HasData()
        {
            return NumericData.Count > 0;
        }

        public int GetDataCount()
        {
            return NumericData.Count;
        }

        public ParsedDataResult GetParsedDataResult()
        {
            return new ParsedDataResult
            {
                Labels = Labels,
                NumericData = NumericData,
                TimeStamps = TimeStamps,
                Progress = ParsingProgress
            };
        }

        #endregion

        #region INotifyPropertyChanged

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

        #endregion
    }

    public class ParsedDataItem : INotifyPropertyChanged
    {
        private string _label = "";
        private double _value = 0;
        private long _timeStamp = 0;
        private int _index = 0;

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public long TimeStamp
        {
            get => _timeStamp;
            set => SetProperty(ref _timeStamp, value);
        }

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        public string FormattedTimeStamp
        {
            get
            {
                var timeSpan = TimeSpan.FromMilliseconds(TimeStamp);
                return timeSpan.ToString(@"hh\:mm\:ss\.fff");
            }
        }

        public string FormattedValue
        {
            get => Value.ToString("F2");
        }

        #region INotifyPropertyChanged

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

        #endregion
    }

    public class ParsedDataResult
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<double> NumericData { get; set; } = new List<double>();
        public List<long> TimeStamps { get; set; } = new List<long>();
        public float Progress { get; set; }
    }
} 