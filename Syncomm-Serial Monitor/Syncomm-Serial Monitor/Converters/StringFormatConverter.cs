using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace Syncomm_Serial_Monitor.Converters
{
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null)
                return string.Empty;

            string format = parameter.ToString();
            
            try
            {
                if (value is IFormattable formattable)
                {
                    return formattable.ToString(format, CultureInfo.CurrentCulture);
                }
                
                return string.Format(format, value);
            }
            catch
            {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
} 