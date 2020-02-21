using System;
using System.Globalization;
using System.Windows.Data;

namespace ABServer.Views.Converters
{
    [ValueConversion(typeof(DateTime), typeof(String))]
    class AccountStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dt = DateTime.Parse(value.ToString());
            if ((dt - DateTime.Now).TotalSeconds < 0)
                return "Не оплачен";
            else
                return "Работает";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
