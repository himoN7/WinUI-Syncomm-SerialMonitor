# Parsing Service Documentation

This document describes the parsing service implementation based on the QT reference app's parser functionality.

## Overview

The `ParsingService` is a comprehensive data parsing service that can extract numeric data, labels, and timestamps from serial data streams. It's designed to work with various data formats and provides real-time parsing capabilities.

## Features

### 1. Multiple Parsing Modes
- **Standard Parsing**: Extracts numeric data and labels from space/tab-separated values
- **CSV Parsing**: Handles comma-separated values with header detection
- **External Clock Support**: Can use external timestamps or system clock
- **Time Range Filtering**: Filter data based on time ranges

### 2. Data Extraction
- **Numeric Data**: Extracts floating-point numbers using regex patterns
- **Labels**: Automatically generates or extracts labels for data series
- **Timestamps**: Supports multiple time formats and external clock sources
- **Progress Tracking**: Real-time progress reporting for large datasets

### 3. Event-Driven Architecture
- **ParsingCompleted**: Fired when parsing is complete
- **ProgressUpdated**: Fired during parsing with progress percentage

## Implementation Details

### Core Components

#### ParsingService
```csharp
public class ParsingService
{
    // Main parsing methods
    public void Parse(string inputString, bool syncToSystemClock, bool useExternalClock, string externalClockLabel)
    public void ParseCSV(string inputString, bool useExternalLabel, string externalClockLabel)
    
    // Data access
    public List<double> GetListNumericValues()
    public List<string> GetStringListLabels()
    public List<long> GetListTimeStamp()
    
    // Control methods
    public void Abort()
    public void Clear()
    public void SetParsingTimeRange(TimeSpan minTime, TimeSpan maxTime)
}
```

#### ParsingModel
```csharp
public class ParsingModel : INotifyPropertyChanged
{
    // Configuration properties
    public bool SyncToSystemClock { get; set; }
    public bool UseExternalClock { get; set; }
    public string ExternalClockLabel { get; set; }
    public TimeSpan MinimumTime { get; set; }
    public TimeSpan MaximumTime { get; set; }
    
    // Results
    public List<string> Labels { get; set; }
    public List<double> NumericData { get; set; }
    public List<long> TimeStamps { get; set; }
    
    // UI binding
    public ObservableCollection<ParsedDataItem> ParsedDataItems { get; set; }
}
```

### Supported Time Formats
```csharp
private readonly List<string> _searchTimeFormats = new List<string>
{
    "HH:mm:ss:fff",  // 12:34:56:789
    "HH:mm:ss.fff",  // 12:34:56.789
    "HH:mm:ss.f",    // 12:34:56.7
    "HH:mm:ss"       // 12:34:56
};
```

### Regex Patterns
```csharp
private readonly Regex _mainSymbols = new Regex(@"^[+-]?\d*\.?\d+$");  // Numeric values
private readonly Regex _alphanumericSymbols = new Regex(@"^\w+$");       // Labels
private readonly Regex _sepSymbols = new Regex(@"[=,]");                 // Separators
```

## Usage Examples

### Basic Parsing
```csharp
var parsingService = new ParsingService();
parsingService.ParsingCompleted += OnParsingCompleted;

// Parse simple numeric data
parsingService.Parse("123.45 67.89 101.11", true, false, "");

// Parse with labels
parsingService.Parse("temp 23.5 pressure 101.3 humidity 45.2", true, false, "");
```

### CSV Parsing
```csharp
var csvData = "Time,Temperature,Pressure\n12:34:56,23.5,101.3\n12:34:57,23.6,101.4";
parsingService.ParseCSV(csvData, false, "");
```

### External Clock
```csharp
// Use external clock label
parsingService.Parse("clock 1234567890 temp 23.5", true, true, "clock");

// Use time format detection
parsingService.Parse("12:34:56.789 temp 23.5", true, true, "");
```

### Time Range Filtering
```csharp
parsingService.SetParsingTimeRange(
    TimeSpan.FromHours(12), 
    TimeSpan.FromHours(13)
);
```

## Integration with MVVM

### ViewModel Integration
```csharp
public class SerialMonitorViewModel : BaseViewModel
{
    private readonly ParsingService _parsingService;
    public ParsingModel ParsingModel { get; }

    public SerialMonitorViewModel()
    {
        _parsingService = new ParsingService();
        ParsingModel = new ParsingModel();
        
        // Subscribe to events
        _parsingService.ParsingCompleted += OnParsingCompleted;
        _parsingService.ProgressUpdated += OnParsingProgressUpdated;
    }

    private void OnDataReceived(object sender, DataReceivedEventArgs e)
    {
        // Parse incoming data
        _parsingService.Parse(e.Data, 
            ParsingModel.SyncToSystemClock, 
            ParsingModel.UseExternalClock, 
            ParsingModel.ExternalClockLabel);
    }
}
```

### UI Binding
```xml
<!-- Parsing configuration -->
<ToggleSwitch Header="Sync to System Clock" 
              IsOn="{Binding ParsingModel.SyncToSystemClock}"/>
<ToggleSwitch Header="Use External Clock" 
              IsOn="{Binding ParsingModel.UseExternalClock}"/>
<TextBox Header="External Clock Label" 
         Text="{Binding ParsingModel.ExternalClockLabel, Mode=TwoWay}"/>

<!-- Progress display -->
<ProgressBar Value="{Binding ParsingModel.ParsingProgress}" 
             Maximum="100"/>

<!-- Parsed data display -->
<ListView ItemsSource="{Binding ParsingModel.ParsedDataItems}">
    <ListView.ItemTemplate>
        <DataTemplate>
            <Grid>
                <TextBlock Text="{Binding Label}"/>
                <TextBlock Text="{Binding FormattedValue}"/>
                <TextBlock Text="{Binding FormattedTimeStamp}"/>
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

## Data Flow

1. **Serial Data Received** → `SerialPortService`
2. **Data Processing** → `DataProcessingService` (format conversion)
3. **Parsing** → `ParsingService` (extract numeric data, labels, timestamps)
4. **Model Update** → `ParsingModel` (store results)
5. **UI Update** → `ParsingView` (display parsed data)

## Benefits

### 1. Real-time Processing
- Processes data as it arrives
- Non-blocking async operations
- Progress reporting for large datasets

### 2. Flexible Configuration
- Multiple clock sources (system, external)
- Time range filtering
- Custom label extraction

### 3. Rich Data Extraction
- Automatic numeric detection
- Label-value pair recognition
- Multiple timestamp formats

### 4. MVVM Integration
- Clean separation of concerns
- Data binding support
- Observable collections for UI

### 5. Performance
- Efficient regex patterns
- Memory-conscious processing
- Abort capability for long operations

## Comparison with QT Reference

| Feature | QT Parser | WinUI ParsingService |
|---------|-----------|---------------------|
| Basic Parsing | ✅ | ✅ |
| CSV Parsing | ✅ | ✅ |
| External Clock | ✅ | ✅ |
| Time Range Filtering | ✅ | ✅ |
| Progress Reporting | ✅ | ✅ |
| Async Processing | ❌ | ✅ |
| MVVM Integration | ❌ | ✅ |
| Data Binding | ❌ | ✅ |
| Event-Driven | ✅ | ✅ |

## Future Enhancements

1. **Advanced Pattern Matching**: Support for custom regex patterns
2. **Data Validation**: Input validation and error reporting
3. **Batch Processing**: Process multiple files simultaneously
4. **Export Formats**: Support for JSON, XML, and other formats
5. **Machine Learning**: Automatic pattern detection and classification
6. **Real-time Visualization**: Direct integration with plotting services

## Testing

The parsing service can be tested with various data formats:

```csharp
// Test data examples
var testData = new[]
{
    "123.45 67.89 101.11",                    // Simple numeric
    "temp 23.5 pressure 101.3 humidity 45.2",  // Label-value pairs
    "12:34:56.789 23.5 101.3 45.2",          // With timestamp
    "clock 1234567890 temp 23.5",             // External clock
    "Time,Temp,Press\n12:34:56,23.5,101.3"   // CSV format
};
```

This parsing service provides a robust foundation for data analysis in the WinUI Serial Monitor application, maintaining compatibility with the QT reference implementation while adding modern .NET features and MVVM integration. 