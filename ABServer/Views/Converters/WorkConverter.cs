using System;
using System.Globalization;
using System.Windows.Data;
using ABServer.Model;

namespace ABServer.Views.Converters
{
    [ValueConversion(typeof(WorkStatus), typeof(string))]
    class WorkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (WorkStatus)Enum.Parse(typeof(WorkStatus), value.ToString());
            switch (status)
            {
                case WorkStatus.Error:
                    return "Ошибка";
                case WorkStatus.Stop:
                    return "Остановлен";
                case WorkStatus.Work:
                    return "Работает";
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
