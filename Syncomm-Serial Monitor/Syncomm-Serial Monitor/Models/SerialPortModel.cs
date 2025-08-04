using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Syncomm_Serial_Monitor.Models
{
    public class SerialPortModel : INotifyPropertyChanged
    {
        private string _selectedPort;
        private int _baudRate = 115200;
        private int _dataBits = 8;
        private Parity _parity = Parity.None;
        private StopBits _stopBits = StopBits.One;
        private bool _isConnected;
        private long _bytesReceived;
        private long _bytesSent;
        private string _connectionStatus = "Disconnected";

        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        public int DataBits
        {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        public Parity Parity
        {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        public StopBits StopBits
        {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public long BytesReceived
        {
            get => _bytesReceived;
            set => SetProperty(ref _bytesReceived, value);
        }

        public long BytesSent
        {
            get => _bytesSent;
            set => SetProperty(ref _bytesSent, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public List<string> AvailablePorts { get; set; } = new List<string>();
        public List<int> AvailableBaudRates { get; set; } = new List<int> { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
        public List<int> AvailableDataBits { get; set; } = new List<int> { 8, 7, 6, 5 };

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

        public void RefreshAvailablePorts()
        {
            AvailablePorts.Clear();
            string[] ports = SerialPort.GetPortNames();
            AvailablePorts.AddRange(ports);
            OnPropertyChanged(nameof(AvailablePorts));
        }

        public void ResetCounters()
        {
            BytesReceived = 0;
            BytesSent = 0;
        }
    }
} 