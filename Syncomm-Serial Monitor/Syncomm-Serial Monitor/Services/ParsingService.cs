using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Syncomm_Serial_Monitor.Models;

namespace Syncomm_Serial_Monitor.Services
{
    public class ParsingService
    {
        private readonly List<string> _searchTimeFormats = new List<string>
        {
            "HH:mm:ss:fff",
            "HH:mm:ss.fff", 
            "HH:mm:ss.f",
            "HH:mm:ss"
        };

        private readonly Regex _mainSymbols = new Regex(@"^[+-]?\d*\.?\d+$");
        private readonly Regex _alphanumericSymbols = new Regex(@"^\w+$");
        private readonly Regex _sepSymbols = new Regex(@"[=,]");

        private bool _abortFlag = false;
        private bool _canReportProgress = false;
        private float _parsingProgressPercent = 0.0f;
        private int _lineCount = 0;

        // Storage for parsed data
        private List<double> _dataStorage = new List<double>();
        private List<double> _listNumericData = new List<double>();
        private List<long> _listTimeStamp = new List<long>();
        private List<long> _timeStampStorage = new List<long>();
        private List<string> _labelStorage = new List<string>();
        private List<string> _stringListNumericData = new List<string>();
        private List<string> _stringListLabels = new List<string>();
        private List<string> _textStorage = new List<string>();

        // Time tracking
        private DateTime _parserClock;
        private DateTime _latestTimeStamp;
        private TimeSpan _minimumTime;
        private TimeSpan _maximumTime;

        // Events
        public event EventHandler<ProgressEventArgs> ProgressUpdated;
        public event EventHandler<ParsingCompletedEventArgs> ParsingCompleted;

        public ParsingService()
        {
            _parserClock = DateTime.Now;
            _latestTimeStamp = DateTime.MinValue;
        }

        #region Public Methods

        public async Task ParseAsync(string inputString, bool syncToSystemClock = true, bool useExternalClock = false, string externalClockLabel = "")
        {
            await Task.Run(() => Parse(inputString, syncToSystemClock, useExternalClock, externalClockLabel));
        }

        public async Task ParseCSVAsync(string inputString, bool useExternalLabel = false, string externalClockLabel = "")
        {
            await Task.Run(() => ParseCSV(inputString, useExternalLabel, externalClockLabel));
        }

        public void Parse(string inputString, bool syncToSystemClock = true, bool useExternalClock = false, string externalClockLabel = "")
        {
            _listNumericData.Clear();
            _stringListLabels.Clear();
            _listTimeStamp.Clear();
            _lineCount = 0;

            if (string.IsNullOrEmpty(inputString)) return;

            var inputStringSplitArrayLines = inputString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _lineCount = inputStringSplitArrayLines.Length;

            for (int l = 0; l < inputStringSplitArrayLines.Length; l++)
            {
                _parsingProgressPercent = (float)l / inputStringSplitArrayLines.Length * 100.0f;
                
                if (l % 50 == 0 && _canReportProgress)
                {
                    ProgressUpdated?.Invoke(this, new ProgressEventArgs { Progress = _parsingProgressPercent });
                }

                if (_abortFlag)
                {
                    _abortFlag = false;
                    break;
                }

                var line = inputStringSplitArrayLines[l];
                var processedLine = _sepSymbols.Replace(line, " ");
                var inputStringSplitArray = processedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < inputStringSplitArray.Length; i++)
                {
                    // Find external time...
                    if (useExternalClock)
                    {
                        if (!string.IsNullOrEmpty(externalClockLabel) && inputStringSplitArray[i] == externalClockLabel)
                        {
                            if (i + 1 < inputStringSplitArray.Length && long.TryParse(inputStringSplitArray[i + 1], out long msecs))
                            {
                                _latestTimeStamp = DateTime.Today.AddMilliseconds(msecs);
                            }
                        }
                        else if (string.IsNullOrEmpty(externalClockLabel))
                        {
                            foreach (var timeFormat in _searchTimeFormats)
                            {
                                if (DateTime.TryParseExact(inputStringSplitArray[i], timeFormat, null, System.Globalization.DateTimeStyles.None, out DateTime parsedTime))
                                {
                                    _latestTimeStamp = parsedTime;
                                    break;
                                }
                            }

                            if (_minimumTime != TimeSpan.Zero && _maximumTime != TimeSpan.Zero)
                            {
                                var currentTime = _latestTimeStamp.TimeOfDay;
                                if (currentTime < _minimumTime || currentTime > _maximumTime)
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    // Parse numeric data and labels
                    if (i == 0 && _mainSymbols.IsMatch(inputStringSplitArray[0]))
                    {
                        if (double.TryParse(inputStringSplitArray[i], out double value))
                        {
                            _listNumericData.Add(value);
                            _stringListLabels.Add("Graph 0");
                        }
                    }
                    else if (i > 0 && _mainSymbols.IsMatch(inputStringSplitArray[i]) && _mainSymbols.IsMatch(inputStringSplitArray[i - 1]))
                    {
                        if (double.TryParse(inputStringSplitArray[i], out double value))
                        {
                            _listNumericData.Add(value);
                            _stringListLabels.Add($"Graph {i}");
                        }
                    }
                    else if (i > 0 && _mainSymbols.IsMatch(inputStringSplitArray[i]) && !_mainSymbols.IsMatch(inputStringSplitArray[i - 1]))
                    {
                        if (double.TryParse(inputStringSplitArray[i], out double value))
                        {
                            _listNumericData.Add(value);
                            _stringListLabels.Add(inputStringSplitArray[i - 1]);
                        }
                    }
                    else
                    {
                        continue; // Skip if no numeric data found
                    }

                    // Add timestamp
                    if (useExternalClock)
                    {
                        _listTimeStamp.Add((long)_latestTimeStamp.TimeOfDay.TotalMilliseconds);
                    }
                    else
                    {
                        if (syncToSystemClock)
                            _listTimeStamp.Add((long)DateTime.Now.TimeOfDay.TotalMilliseconds);
                        else
                            _listTimeStamp.Add((long)(DateTime.Now - _parserClock).TotalMilliseconds);
                    }
                }
            }

            ParsingCompleted?.Invoke(this, new ParsingCompletedEventArgs
            {
                Labels = _stringListLabels,
                NumericData = _listNumericData,
                TimeStamps = _listTimeStamp
            });
        }

        public void ParseCSV(string inputString, bool useExternalLabel = false, string externalClockLabel = "")
        {
            _listNumericData.Clear();
            _stringListLabels.Clear();
            _listTimeStamp.Clear();
            _lineCount = 0;

            if (string.IsNullOrEmpty(inputString)) return;

            var inputStringSplitArrayLines = inputString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _lineCount = inputStringSplitArrayLines.Length;

            var csvLabels = new List<string>();

            for (int l = 0; l < _lineCount; l++)
            {
                _parsingProgressPercent = (float)l / _lineCount * 100.0f;
                
                if (l % 50 == 0 && _canReportProgress)
                {
                    ProgressUpdated?.Invoke(this, new ProgressEventArgs { Progress = _parsingProgressPercent });
                }

                if (_abortFlag)
                {
                    _abortFlag = false;
                    break;
                }

                var line = inputStringSplitArrayLines[l];
                var processedLine = _sepSymbols.Replace(line, " ");
                processedLine = processedLine.Replace("\"", "");

                var inputStringSplitArray = processedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Look for labels in first line
                if (l == 0)
                {
                    for (int i = 0; i < inputStringSplitArray.Length; i++)
                    {
                        if (!_mainSymbols.IsMatch(inputStringSplitArray[i]))
                        {
                            if (!csvLabels.Contains(inputStringSplitArray[i]))
                                csvLabels.Add(inputStringSplitArray[i]);
                        }
                    }
                }

                // Look for time reference
                for (int i = 0; i < inputStringSplitArray.Length; i++)
                {
                    if (useExternalLabel && !string.IsNullOrEmpty(externalClockLabel) && i == csvLabels.IndexOf(externalClockLabel))
                    {
                        if (float.TryParse(inputStringSplitArray[i], out float msecs))
                        {
                            _latestTimeStamp = DateTime.Today.AddMilliseconds(msecs);
                        }
                        break;
                    }
                    else if (!useExternalLabel)
                    {
                        foreach (var timeFormat in _searchTimeFormats)
                        {
                            if (DateTime.TryParseExact(inputStringSplitArray[i], timeFormat, null, System.Globalization.DateTimeStyles.None, out DateTime parsedTime))
                            {
                                _latestTimeStamp = parsedTime;

                                if (_minimumTime != TimeSpan.Zero && _maximumTime != TimeSpan.Zero)
                                {
                                    var currentTime = _latestTimeStamp.TimeOfDay;
                                    if (currentTime < _minimumTime || currentTime > _maximumTime)
                                    {
                                        continue;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                // Look for data
                for (int i = 0; i < inputStringSplitArray.Length; i++)
                {
                    if (_mainSymbols.IsMatch(inputStringSplitArray[i]))
                    {
                        if (i >= csvLabels.Count)
                            continue; // TODO: ERROR REPORTING

                        if (double.TryParse(inputStringSplitArray[i], out double value))
                        {
                            _stringListLabels.Add(csvLabels[i]);
                            _listNumericData.Add(value);
                            _listTimeStamp.Add((long)_latestTimeStamp.TimeOfDay.TotalMilliseconds);
                        }
                    }
                }
            }

            ParsingCompleted?.Invoke(this, new ParsingCompletedEventArgs
            {
                Labels = _stringListLabels,
                NumericData = _listNumericData,
                TimeStamps = _listTimeStamp
            });
        }

        public void GetCSVReadyData(out List<string> columnNames, out List<List<double>> dataColumns)
        {
            var labelStorage = GetLabelStorage();
            var tempColumnNames = labelStorage.Distinct().ToList();
            var tempColumnsData = new List<List<double>>();
            var numericDataList = GetDataStorage();

            for (int i = 0; i < tempColumnNames.Count; i++)
            {
                tempColumnsData.Add(new List<double>());

                while (labelStorage.Contains(tempColumnNames[i]))
                {
                    int index = labelStorage.IndexOf(tempColumnNames[i]);
                    if (index < numericDataList.Count)
                    {
                        tempColumnsData[tempColumnsData.Count - 1].Add(numericDataList[index]);
                        numericDataList.RemoveAt(index);
                    }
                    labelStorage.RemoveAt(index);
                }
            }

            columnNames = tempColumnNames;
            dataColumns = tempColumnsData;
        }

        #endregion

        #region Data Access Methods

        public List<double> GetDataStorage() => _dataStorage;
        public List<double> GetListNumericValues() => _listNumericData;
        public List<long> GetListTimeStamp() => _listTimeStamp;
        public List<long> GetTimeStorage() => _timeStampStorage;
        public List<string> GetLabelStorage() => _labelStorage;
        public List<string> GetStringListLabels() => _stringListLabels;
        public List<string> GetStringListNumericData() => _stringListNumericData;
        public List<string> GetTextList() => _textStorage;

        #endregion

        #region Control Methods

        public void ClearExternalClock() => _latestTimeStamp = DateTime.MinValue;
        public void RestartChartTimer() => _parserClock = DateTime.Now;
        public void Abort() => _abortFlag = true;
        public void SetReportProgress(bool isEnabled) => _canReportProgress = isEnabled;

        public void ParserClockAddMSecs(int millis)
        {
            _parserClock = _parserClock.AddMilliseconds(millis);
        }

        public void AppendSetToMemory(List<string> newLabelList, List<double> newDataList, List<long> newTimeList, string text = "")
        {
            _labelStorage.AddRange(newLabelList);
            _dataStorage.AddRange(newDataList);
            _timeStampStorage.AddRange(newTimeList);

            if (!string.IsNullOrEmpty(text))
                _textStorage.Add(text);
        }

        public void ClearStorage()
        {
            _labelStorage.Clear();
            _dataStorage.Clear();
            _timeStampStorage.Clear();
            _textStorage.Clear();
        }

        public void Clear()
        {
            _stringListLabels.Clear();
            _stringListNumericData.Clear();
            _listTimeStamp.Clear();
        }

        public void SetParsingTimeRange(TimeSpan minTime, TimeSpan maxTime)
        {
            _minimumTime = minTime;
            _maximumTime = maxTime;
        }

        public void ResetTimeRange()
        {
            _minimumTime = TimeSpan.Zero;
            _maximumTime = TimeSpan.Zero;
        }

        #endregion

        #region Helper Methods

        public ParsedDataResult GetParsedData()
        {
            return new ParsedDataResult
            {
                Labels = _stringListLabels,
                NumericData = _listNumericData,
                TimeStamps = _listTimeStamp,
                Progress = _parsingProgressPercent
            };
        }

        public bool HasData()
        {
            return _listNumericData.Count > 0;
        }

        public int GetDataCount()
        {
            return _listNumericData.Count;
        }

        #endregion
    }

    #region Event Args Classes

    public class ProgressEventArgs : EventArgs
    {
        public float Progress { get; set; }
    }

    public class ParsingCompletedEventArgs : EventArgs
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<double> NumericData { get; set; } = new List<double>();
        public List<long> TimeStamps { get; set; } = new List<long>();
    }

    public class ParsedDataResult
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<double> NumericData { get; set; } = new List<double>();
        public List<long> TimeStamps { get; set; } = new List<long>();
        public float Progress { get; set; }
    }

    #endregion
} 