# MVVM Structure Documentation

This document describes the MVVM (Model-View-ViewModel) architecture implemented in the Syncomm Serial Monitor application.

## Architecture Overview

The application follows the MVVM pattern to separate concerns and improve maintainability:

### Models
Located in the `Models/` directory:
- **SerialPortModel**: Represents serial port configuration and state
- **DataModel**: Represents data display settings and collections
- **PlotModel**: Represents chart data and visualization settings

### ViewModels
Located in the `ViewModels/` directory:
- **BaseViewModel**: Abstract base class with common INotifyPropertyChanged implementation
- **SerialMonitorViewModel**: Main ViewModel handling business logic for serial communication

### Views
Located in the `Views/` directory:
- **SerialMonitorView**: MVVM-based view for the main serial monitor page

### Services
Located in the `Services/` directory:
- **SerialPortService**: Handles serial port communication
- **DataProcessingService**: Handles data processing and formatting
- **NotificationService**: Handles notifications and clipboard operations

### Converters
Located in the `Converters/` directory:
- **InverseBooleanConverter**: Converts boolean values to their inverse

## Key Features

### Data Binding
- All UI controls are bound to ViewModel properties
- Commands are used for user interactions
- Property change notifications update the UI automatically

### Separation of Concerns
- **Models**: Pure data objects with no UI dependencies
- **ViewModels**: Business logic and state management
- **Views**: UI presentation only
- **Services**: Reusable business logic

### Command Pattern
- All user actions are implemented as commands
- Commands can be enabled/disabled based on application state
- Commands support async operations

### Event-Driven Architecture
- Services raise events for data received, errors, and state changes
- ViewModels subscribe to service events
- UI updates automatically through data binding

## Migration from Original Code

The original `SerialMonitorPage.xaml.cs` contained over 2700 lines of code with mixed concerns:
- UI event handlers
- Serial port communication
- Data processing
- Business logic
- UI updates

This has been refactored into:

1. **SerialMonitorViewModel** (~400 lines): Business logic and state management
2. **SerialPortService** (~200 lines): Serial communication
3. **DataProcessingService** (~150 lines): Data formatting and export
4. **Models** (~100 lines each): Data structures
5. **SerialMonitorView** (~50 lines): UI event handling only

## Benefits

1. **Testability**: ViewModels can be unit tested independently
2. **Maintainability**: Clear separation of concerns
3. **Reusability**: Services can be reused across different views
4. **Scalability**: Easy to add new features without affecting existing code
5. **Debugging**: Issues can be isolated to specific layers

## Usage

To use the new MVVM structure:

1. The `SerialMonitorView` automatically creates its ViewModel
2. Data binding handles UI updates
3. Commands handle user interactions
4. Services handle external communication

The original `SerialMonitorPage` can be kept for reference or removed once the new structure is fully tested. 