Getting Started with WinUI Cartesian Chart (SfCartesianChart)
2 Jan 202517 minutes to read

This section explains how to populate the Cartesian chart with data, a header, data labels, a legend and tooltips, as well as the essential aspects for getting started with the chart.

Creating an application with WinUI Chart
Create a WinUI 3 desktop app for C# and .NET 5.
Add reference to Syncfusion.Chart.WinUI NuGet.
To initialize the control, import the control namespace Syncfusion.UI.Xaml.Charts in XAML or C#.
Initialize SfCartesianChart control.

XAML
 
C#

<Window
    . . .
       
    xmlns:chart="using:Syncfusion.UI.Xaml.Charts">
       
    <Grid x:Name="grid">
        <chart:SfCartesianChart/>
    </Grid>
</Window>
Initialize View Model
Now, let us define a simple data model that represents a data point in chart.

C#

public class Person   
{   
    public string Name { get; set; }

    public double Height { get; set; }
}
Next, create a view model class and initialize a list of Person objects as follows.

C#

public class ViewModel
{
    public List<Person> Data { get; set; }
    public ViewModel()
    {
        Data = new List<Person>()
        {
            new Person { Name = "David", Height = 170 },
            new Person { Name = "Michael", Height = 96 },
            new Person { Name = "Steve", Height = 65 },
            new Person { Name = "Joel", Height = 182 },
            new Person { Name = "Bob", Height = 134 }
        };
    }
}
Set the ViewModel instance as the DataContext of our chart; this is done to bind properties of ViewModel to the chart.

NOTE

Add namespace of ViewModel class to your XAML Page if you prefer to set DataContext in XAML.
XAML
 
C#

<Window
    x:Class="SfChart_GettingStarted.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:model="using:SfChart_GettingStarted"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:chart="using:Syncfusion.UI.Xaml.Charts"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d" Height="350" Width="525"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid x:Name="grid">
	    <Grid.DataContext>
	        <model:ViewModel/>
	    </Grid.DataContext>
	</Grid>	
	
</Window>
Initialize Chart Axis
ChartAxis is used to locate the data points inside the chart area. The XAxes and YAxes collection of the chart is used to initialize the axis for the chart.

XAML
 
C#

<chart:SfCartesianChart>

    <chart:SfCartesianChart.XAxes>
        <chart:CategoryAxis/>
    </chart:SfCartesianChart.XAxes>

    <chart:SfCartesianChart.YAxes>
        <chart:NumericalAxis/>
    </chart:SfCartesianChart.YAxes>
    
</chart:SfCartesianChart>
Run the project and check if you get following output to make sure you have configured your project properly to add chart.

Initializing axis for WinUI Chart

Populate Chart with Data
As we are going to visualize the comparison of heights in the data model, add ColumnSeries to Series property of chart, and then bind the Data property of the above ViewModel to the ColumnSeries.ItemsSource as follows.

NOTE

You need to set XBindingPath and YBindingPath properties, so that chart would fetch values from the respective properties in the data model to plot the series.
XAML
 
C#

<chart:SfCartesianChart>
    <chart:SfCartesianChart.XAxes>
        <chart:CategoryAxis Header="Name"/>
    </chart:SfCartesianChart.XAxes>
    <chart:SfCartesianChart.YAxes>
        <chart:NumericalAxis Header="Height(in cm)"/>
    </chart:SfCartesianChart.YAxes>
    <chart:ColumnSeries ItemsSource="{Binding Data}"
                        XBindingPath="Name" 
                        YBindingPath="Height">
    </chart:ColumnSeries>
</chart:SfCartesianChart>
Add Title
The title of the chart provide quick information to the user about the data being plotted in the chart. The Header property is used to set title for the chart as follows.

XAML
 
C#

<Grid>
   <chart:SfCartesianChart Header="Height Comparison"> 
   </chart:SfCartesianChart>
</Grid>
Enable Data Labels
The ShowDataLabels property of series can be used to enable the data labels to improve the readability of the chart. The label visibility is set to False by default.

XAML
 
C#

<chart:SfCartesianChart>
    . . . 
    <chart:ColumnSeries ShowDataLabels="True">
    </chart:ColumnSeries>

</chart:SfCartesianChart>
Enable Legend
The legend provides information about the data point displayed in the chart. The Legend property of the chart was used to enable it.

XAML
 
C#

<chart:SfCartesianChart >
    . . .
    <chart:SfCartesianChart.Legend>
        <chart:ChartLegend/>
    </chart:SfCartesianChart.Legend>
    . . .
</chart:SfCartesianChart>
NOTE

Additionally, set label for each series using the Label property of chart series, which will be displayed in corresponding legend.
XAML
 
C#

<chart:SfCartesianChart>
. . .
    <chart:ColumnSeries Label="Heights"
                        ItemsSource="{Binding Data}"
                        XBindingPath="Name" 
                        YBindingPath="Height">
    </chart:ColumnSeries>
</chart:SfCartesianChart>
Enable Tooltip
Tooltips are used to show information about the segment, when hovers on the segment. Enable tooltip by setting series EnableTooltip property to true.

XAML
 
C#

<chart:SfCartesianChart>
	...
   <chart:ColumnSeries
				EnableTooltip="True" 
			    ItemsSource="{Binding Data}" 
			    XBindingPath="Name" 
			    YBindingPath="Height"/>
	...
</chart:SfCartesianChart>
The following code example gives you the complete code of above configurations.

XAML
 
C#

<Window
    x:Class="SfChart_GettingStarted.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:chart="using:Syncfusion.UI.Xaml.Charts"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d" Height="350" Width="525"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <chart:SfCartesianChart Header="Height Comparison">
        <chart:SfCartesianChart.Legend>
            <chart:ChartLegend/>
        </chart:SfCartesianChart.Legend>
        <chart:SfCartesianChart.DataContext>
            <Model:ChartViewModel/>
        </chart:SfCartesianChart.DataContext>
        
        <!--Initialize the axis for chart-->
        <chart:SfCartesianChart.XAxes>
            <chart:CategoryAxis Header="Names"/>
        </chart:SfCartesianChart.XAxes>
        <chart:SfCartesianChart.YAxes>
            <chart:NumericalAxis Header="Height(in cm)"/>
        </chart:SfCartesianChart.YAxes>

        <!--Initialize the series for chart-->
        <chart:ColumnSeries 
						Label="Heights" 
							EnableTooltip="True"
                            ShowDataLabels="True"
                            ItemsSource="{Binding Data}"
                            XBindingPath="Name" 
                            YBindingPath="Height">
            <chart:ColumnSeries.DataLabelSettings>
                <chart:CartesianDataLabelSettings Position="Inner"/>
            </chart:ColumnSeries.DataLabelSettings>
        </chart:ColumnSeries>
    </chart:SfCartesianChart>
</Window>
The following chart is created as a result of the previous codes.

Chart Area in WinUI Chart (SfCartesianChart)
2 Jan 20252 minutes to read

Chart area represents the entire chart and all its elements. It’s a virtual rectangular area that includes all the chart elements like title, axis, legends, series, etc.

Customization
Chart provides the properties like PlotAreaBorderBrush, PlotAreaBorderThickness and PlotAreaBackground for customizing the plot area.

XAML
 
C#

<chart:SfCartesianChart Header="Chart Area Header" 
                        PlotAreaBackground="LightCyan" 
                        Background="LightBlue"
                        PlotAreaBorderBrush="Blue" 
                        PlotAreaBorderThickness="3">
. . .
    <chart:SfCartesianChart.XAxes>
        <chart:CategoryAxis/>
    </chart:SfCartesianChart.XAxes>

    <chart:SfCartesianChart.YAxes>
        <chart:NumericalAxis/>
    </chart:SfCartesianChart.YAxes>

    <chart:SfCartesianChart.Legend>
        <chart:ChartLegend/>
    </chart:SfCartesianChart.Legend>

    <chart:SfCartesianChart.Series>
        <chart:ColumnSeries ItemsSource="{Binding Data}" 
                            XBindingPath="Demand" 
                            YBindingPath="Year2010" 
                            Label="Year 2010">
        </chart:ColumnSeries>
    </chart:SfCartesianChart.Series>

</chart:SfCartesianChart>Chart Title in WinUI Chart (SfCartesianChart)
2 Jan 20254 minutes to read

Header property is used to define the title for the chart.

XAML
 
C#

<chart:SfCartesianChart x:Name="chart" Header="Chart Area Header">
 . . .           
</chart:SfCartesianChart>
Title support in WinUI chart

Customization
Chart provides support to add any UIElement as a title. The following code example defines TextBlock as chart header.

XAML
 
C#

<chart:SfCartesianChart>

    <chart:SfCartesianChart.Header>
        <Border BorderThickness="2" 
                BorderBrush="Black" 
                Margin="10" 
                CornerRadius="5">
            <TextBlock FontSize="14"
					   Text="Chart Area Header"
					   Margin="5"/>
        </Border>
    </chart:SfCartesianChart.Header>
            
</chart:SfCartesianChart>
Title customization support in WinUI chart

Alignment
The title text content can be aligned horizontally to the left, center or right of the chart using the HorizontalHeaderAlignment property of the SfCartesianChart.

XAML
 
C#

<chart:SfCartesianChart x:Name="chart" 
						HorizontalHeaderAlignment="Left">
. . .
    <chart:SfCartesianChart.Header>
        <Border BorderThickness="2" 
                BorderBrush="Black" 
                Margin="0, 0, 0, 10" 
                CornerRadius="5">
            <TextBlock FontSize="14" 
					   Text="Chart Area Header"
					   Margin="5"/>
        </Border>
    </chart:SfCartesianChart.Header>
. . . 
</chart:SfCartesianChart>
Getting Started with WinUI Cartesian Chart (SfCartesianChart)
2 Jan 202517 minutes to read

This section explains how to populate the Cartesian chart with data, a header, data labels, a legend and tooltips, as well as the essential aspects for getting started with the chart.

Creating an application with WinUI Chart
Create a WinUI 3 desktop app for C# and .NET 5.
Add reference to Syncfusion.Chart.WinUI NuGet.
To initialize the control, import the control namespace Syncfusion.UI.Xaml.Charts in XAML or C#.
Initialize SfCartesianChart control.

XAML
 
C#

using Syncfusion.UI.Xaml.Charts;
   
namespace SfChart_GettingStarted
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
               
            SfCartesianChart chart = new SfCartesianChart();      
            grid.Children.Add(chart);
        }
    }   
}
Initialize View Model
Now, let us define a simple data model that represents a data point in chart.

C#

public class Person   
{   
    public string Name { get; set; }

    public double Height { get; set; }
}
Next, create a view model class and initialize a list of Person objects as follows.

C#

public class ViewModel
{
    public List<Person> Data { get; set; }
    public ViewModel()
    {
        Data = new List<Person>()
        {
            new Person { Name = "David", Height = 170 },
            new Person { Name = "Michael", Height = 96 },
            new Person { Name = "Steve", Height = 65 },
            new Person { Name = "Joel", Height = 182 },
            new Person { Name = "Bob", Height = 134 }
        };
    }
}
Set the ViewModel instance as the DataContext of our chart; this is done to bind properties of ViewModel to the chart.

NOTE

Add namespace of ViewModel class to your XAML Page if you prefer to set DataContext in XAML.
XAML
 
C#

grid.DataContext = new ViewModel();
Initialize Chart Axis
ChartAxis is used to locate the data points inside the chart area. The XAxes and YAxes collection of the chart is used to initialize the axis for the chart.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
CategoryAxis xAxis = new CategoryAxis();
chart.XAxes.Add(xAxis);
NumericalAxis yAxis = new NumericalAxis();
chart.YAxes.Add(yAxis);
Run the project and check if you get following output to make sure you have configured your project properly to add chart.

Initializing axis for WinUI Chart

Populate Chart with Data
As we are going to visualize the comparison of heights in the data model, add ColumnSeries to Series property of chart, and then bind the Data property of the above ViewModel to the ColumnSeries.ItemsSource as follows.

NOTE

You need to set XBindingPath and YBindingPath properties, so that chart would fetch values from the respective properties in the data model to plot the series.
XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();

//Adding horizontal axis to the chart 
CategoryAxis xAxis = new CategoryAxis();
xAxis.Header = "Name";   
chart.XAxes.Add(xAxis);

//Adding vertical axis to the chart 
NumericalAxis yAxis = new NumericalAxis();
yAxis.Header = "Height(in cm)";  
chart.YAxes.Add(yAxis);

//Initialize the two series for SfChart
ColumnSeries series = new ColumnSeries();

series.ItemsSource = (new ViewModel()).Data;
series.XBindingPath = "Name";            
series.YBindingPath = "Height";         
            
//Adding Series to the Chart Series Collection
chart.Series.Add(series);
Add Title
The title of the chart provide quick information to the user about the data being plotted in the chart. The Header property is used to set title for the chart as follows.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
chart.Header = "Height Comparison";
Enable Data Labels
The ShowDataLabels property of series can be used to enable the data labels to improve the readability of the chart. The label visibility is set to False by default.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
. . .
ColumnSeries series = new ColumnSeries();
series.ShowDataLabels = true;
chart.Series.Add(series);
Enable Legend
The legend provides information about the data point displayed in the chart. The Legend property of the chart was used to enable it.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
chart.Legend = new ChartLegend ();
NOTE

Additionally, set label for each series using the Label property of chart series, which will be displayed in corresponding legend.
XAML
 
C#

ColumnSeries series = new ColumnSeries(); 
series.ItemsSource = (new ViewModel()).Data;
series.XBindingPath = "Name"; 
series.YBindingPath = "Height"; 
series.Label = "Heights";
Enable Tooltip
Tooltips are used to show information about the segment, when hovers on the segment. Enable tooltip by setting series EnableTooltip property to true.

XAML
 
C#

ColumnSeries series = new ColumnSeries();
series.ItemsSource = (new ViewModel()).Data;
series.XBindingPath = "Name";          
series.YBindingPath = "Height";
series.EnableTooltip = true;
The following code example gives you the complete code of above configurations.

XAML
 
C#

using Syncfusion.UI.Xaml.Charts;

namespace SfChart_GettingStarted
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            SfCartesianChart chart = new SfCartesianChart() 
			{ 
				Header = "Height Comparison", 
				Height = 300, 
				Width = 500 
			};

            //Adding horizontal axis to the chart 
            CategoryAxis xAxis = new CategoryAxis();
            xAxis.Header = "Name";
            xAxis.FontSize = 14;
            chart.XAxes.Add(xAxis);

            //Adding vertical axis to the chart 
            NumericalAxis yAxis = new NumericalAxis();
            yAxis.Header = "Height(in cm)";
            yAxis.FontSize = 14;
            chart.YAxes.Add(yAxis);

            //Adding legend for the chart
            ChartLegend legend = new ChartLegend();
            chart.Legend = legend;

            //Initializing column series
            ColumnSeries series = new ColumnSeries();
            series.ItemsSource = (new ViewModel()).Data;
            series.XBindingPath = "Name";            
            series.YBindingPath = "Height";
            series.EnableTooltip = true;
            series.Label = "Heights"; 
            series.ShowDataLabels = true;
            series.DataLabelSettings = new CartesianDataLabelSettings()
            {
                Position = DataLabelPosition.Inner,
            };

            //Adding series to the chart series collection
            chart.Series.Add(series);
            this.Content = chart;
        }
    }   
}
The following chart is created as a result of the previous codes.Legend in WinUI Chart (SfCartesianChart)
13 Jun 202417 minutes to read

The legend contains a list of series data points in the chart. The information provided in each legend item helps you to identify the corresponding series in the chart. This allows us to specify the Label for each series which is to be displayed in legend label.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
. . .
ChartLegend legend = new ChartLegend();
chart.Legend = legend;

SplineSeries series = new SplineSeries();
series.ItemsSource = (new ViewModel()).Data;
series.XBindingPath = "Year";
series.YBindingPath = "India";
series.Label = "Spline";
chart.Series.Add(series);
this.Content = chart;
Legend support in WinUI Chart

Title
Cartesian chart provides support to add any UIElement as a title for legend. Header property of ChartLegend is used to define the title for legend as the following code example.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
ChartLegend legend = new ChartLegend();
. . .
TextBlock textBlock = new TextBlock()
{
    Text = "Foods",
    HorizontalTextAlignment = TextAlignment.Center,
    Foreground = new SolidColorBrush(Colors.Blue),
    FontWeight = FontWeights.Bold,
};

legend.Header = textBlock;
chart.Legend = legend;
. . .
this.Content = chart;
Legend title in WinUI Chart

Icon
Legend icon represents a symbol associated with the each legend item. LegendIcon property of series is used to set the icon type for legend item. By default, the legend icon is SeriesType.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
. . .
ChartLegend legend = new ChartLegend();
chart.Legend = legend;

SplineSeries series = new SplineSeries();
series.ItemsSource = (new ViewModel()).Data;
series.XBindingPath = "Year";
series.YBindingPath = "India";
series.Label = "Gold";
series.LegendIcon = ChartLegendIcon.Circle;
chart.Series.Add(series);
this.Content = chart;
Legend icon in WinUI Chart

The appearance of the legend icon can be customized using the below properties.

IconWidth - Gets or sets the double value that represents the legend icon(s) width.
IconHeight - Gets or sets the double value that represents the legend icon(s) height.
IconVisibility - Gets or sets the visibility of the legend icon.
XAML
 
C#

chart.Legend = new ChartLegend()
{
    IconWidth = 15,
    IconHeight = 15,
    IconVisibility = Visibility.Visible,
};
Legend icon in WinUI Chart

Custom Icon
Cartesian chart provides support to add custom icon for the legend using LegendIconTemplate property of series as in below example.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
chart.Legend = new ChartLegend();
. . .
ColumnSeries series = new ColumnSeries()
{
    ItemsSource = new ViewModel().Data,
    XBindingPath = "Year",
    YBindingPath = "India",
    IconTemplate = chart.Resources["iconTemplate"] as DataTemplate
    Label = "Gold";
};

chart.Series.Add(series);
this.Content = chart;
Custom legend icon in WinUI Chart

Legend Visibility
The IsVisibleOnLegend property of series is used to enable/disable the visibility of legend as shown in below example.

XAML
 
C#

SfCartesianChart chart = new SfCartesianChart();
 . . .
chart.Legend = new ChartLegend();

ColumnSeries columnSeries = new ColumnSeries()
{
    Label = "Gold",
    ItemsSource = new ViewModel().Data,
    XBindingPath = "Year",
    YBindingPath = "India",
    IsVisibleOnLegend = true
};
SplineSeries splineSeries = new SplineSeries()
{
    Label = "Silver",
    ItemsSource = new ViewModel().Data,
    XBindingPath = "Year",
    YBindingPath = "America",
    IsVisibleOnLegend = false
};

chart.Series.Add(splineSeries);
chart.Series.Add(columnSeries);
this.Content = chart;
Legend icon visibility support in WinUI Chart

Item spacing
ItemMargin property of the ChartLegend is used to provide spacing between each legend items.

XAML
 
C#

chart.Legend = new ChartLegend()
{
    ItemMargin = new Thickness(10)
};
Legend item spacing support in WinUI Chart

Checkbox for Legend
Cartesian chart provides support to enable the checkbox for each legend item to visible or collapse the associated series. By default, the value of CheckBoxVisibility property is Collapsed.

XAML
 
C#

chart.Legend = new ChartLegend()
{
   CheckBoxVisibility = Visibility.Visible
};
Checkbox support for legend in WinUI Chart

The series can be collapsed by unchecking the checkbox as below:

Checkbox support for legend in WinUI Chart

Toggle Series Visibility
The visibility of the series can be control by tapping the legend item by enabling the ToggleSeriesVisibility property. By default, the value of ToggleSeriesVisibility property is False.

XAML
 
C#

chart.Legend = new ChartLegend()
{
   ToggleSeriesVisibility = true
};
ToggleSeriesVisibility support for legend in WinUI Chart

By clicking on disabled legend item, we can view the associated series,

ToggleSeriesVisibility support for legend in WinUI Chart

Placement
Legends can be docked left, right, and top or bottom around the chart area using Placement property. By default, the chart legend is docked at the top of the chart as mentioned earlier.

To display the legend at the bottom, set the Placement as Bottom as in below code snippet.

XAML
 
C#

chart.Legend = new ChartLegend()
{
   Placement = LegendPlacement.Bottom
};
Positioning the legend at right in WinUI Chart

Background customization
The legend background appearance can be customized by using the following properties:

BorderThickness - used to change the stroke width of the legend.
BorderBrush - used to change the stroke color of the legend.
Background - used to change the background color of the legend.
CornerRadius - used to change the corner radius of the legend.

XAML
 
C#

chart.Legend = new ChartLegend()
{
    Background = new SolidColorBrush(Colors.LightGray),
    BorderBrush = new SolidColorBrush(Colors.Black),
    BorderThickness = new Thickness(1),
    CornerRadius = new CornerRadius(5)
};
Template
Customize each legend item by using the ItemTemplate property in ChartLegend, as shown in the following code sample.

XAML
 
C#

chart.Legend = new ChartLegend()
{
   ItemTemplate = chart.Resources["itemTemplate"] as DataTemplate
};
