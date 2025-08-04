using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ScottPlot;

namespace Syncomm_Serial_Monitor.Models
{
    public class PlotModel : INotifyPropertyChanged
    {
        private string _selectedDataSeries = "All Data";
        private double _updateInterval = 100;
        private string _exportFormat = "PNG";
        private bool _plotInitialized = false;
        private List<double> _timeData = new List<double>();
        private List<double> _receivedData = new List<double>();
        private List<double> _sentData = new List<double>();
        private DateTime _startTime = DateTime.Now;

        public string SelectedDataSeries
        {
            get => _selectedDataSeries;
            set => SetProperty(ref _selectedDataSeries, value);
        }

        public double UpdateInterval
        {
            get => _updateInterval;
            set => SetProperty(ref _updateInterval, value);
        }

        public string ExportFormat
        {
            get => _exportFormat;
            set => SetProperty(ref _exportFormat, value);
        }

        public bool PlotInitialized
        {
            get => _plotInitialized;
            set => SetProperty(ref _plotInitialized, value);
        }

        public List<double> TimeData
        {
            get => _timeData;
            set => SetProperty(ref _timeData, value);
        }

        public List<double> ReceivedData
        {
            get => _receivedData;
            set => SetProperty(ref _receivedData, value);
        }

        public List<double> SentData
        {
            get => _sentData;
            set => SetProperty(ref _sentData, value);
        }

        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public List<string> AvailableDataSeries { get; set; } = new List<string> 
        { 
            "All Data", 
            "Received Data", 
            "Sent Data" 
        };

        public List<string> AvailableExportFormats { get; set; } = new List<string> 
        { 
            "PNG", 
            "JPG", 
            "SVG" 
        };

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

        public void AddDataPoint(double value, bool isSent = false)
        {
            if (!PlotInitialized) return;

            double timeSeconds = (DateTime.Now - StartTime).TotalSeconds;
            
            TimeData.Add(timeSeconds);
            
            if (isSent)
            {
                SentData.Add(value);
                ReceivedData.Add(double.NaN); // No data for received series
            }
            else
            {
                ReceivedData.Add(value);
                SentData.Add(double.NaN); // No data for sent series
            }

            // Limit data points to prevent memory issues
            const int maxPoints = 1000;
            if (TimeData.Count > maxPoints)
            {
                TimeData.RemoveAt(0);
                ReceivedData.RemoveAt(0);
                SentData.RemoveAt(0);
            }
        }

        public void ClearPlotData()
        {
            TimeData.Clear();
            ReceivedData.Clear();
            SentData.Clear();
            StartTime = DateTime.Now;
        }

        public void ResetPlot()
        {
            PlotInitialized = false;
            ClearPlotData();
        }
    }
} 