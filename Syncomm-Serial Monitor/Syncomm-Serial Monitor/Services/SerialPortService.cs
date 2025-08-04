using System;
using System.IO.Ports;
using System.Threading.Tasks;
using Syncomm_Serial_Monitor.Models;

namespace Syncomm_Serial_Monitor.Services
{
    public class SerialPortService : IDisposable
    {
        private SerialPort _serialPort;
        private bool _disposed = false;

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ErrorReceivedEventArgs> ErrorReceived;
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        public async Task ConnectAsync(SerialPortConfiguration config)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                throw new InvalidOperationException("Already connected to a serial port");
            }

            _serialPort = new SerialPort
            {
                PortName = config.PortName,
                BaudRate = config.BaudRate,
                DataBits = config.DataBits,
                Parity = config.Parity,
                StopBits = config.StopBits,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            try
            {
                await Task.Run(() => _serialPort.Open());
                
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.ErrorReceived += SerialPort_ErrorReceived;
                
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
                {
                    IsConnected = true,
                    PortName = config.PortName
                });
            }
            catch (Exception)
            {
                _serialPort?.Dispose();
                _serialPort = null;
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                var portName = _serialPort.PortName;
                
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.ErrorReceived -= SerialPort_ErrorReceived;
                
                await Task.Run(() =>
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                });
                
                _serialPort = null;
                
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
                {
                    IsConnected = false,
                    PortName = portName
                });
            }
        }

        public async Task SendDataAsync(string data)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                throw new InvalidOperationException("Not connected to serial port");
            }

            await Task.Run(() => _serialPort.Write(data));
        }

        public async Task SendHexDataAsync(string hexData)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                throw new InvalidOperationException("Not connected to serial port");
            }

            // Remove spaces and dashes
            hexData = hexData.Replace(" ", "").Replace("-", "");
            
            if (string.IsNullOrEmpty(hexData))
            {
                throw new ArgumentException("Hex data is empty");
            }

            try
            {
                byte[] bytes = new byte[hexData.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(hexData.Substring(i * 2, 2), 16);
                }
                
                await Task.Run(() => _serialPort.Write(bytes, 0, bytes.Length));
            }
            catch (Exception)
            {
                throw new ArgumentException("Invalid hex data format");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    string data = _serialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        DataReceived?.Invoke(this, new DataReceivedEventArgs { Data = data });
                    }
                }
                catch (Exception ex)
                {
                    ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs 
                    { 
                        ErrorType = SerialError.RXOver,
                        ErrorMessage = ex.Message 
                    });
                }
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs 
            { 
                ErrorType = e.EventType,
                ErrorMessage = $"Serial port error: {e.EventType}"
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _serialPort?.Dispose();
                _disposed = true;
            }
        }
    }

    public class SerialPortConfiguration
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
    }

    public class DataReceivedEventArgs : EventArgs
    {
        public string Data { get; set; }
    }

    public class ErrorReceivedEventArgs : EventArgs
    {
        public SerialError ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string PortName { get; set; }
    }
} 