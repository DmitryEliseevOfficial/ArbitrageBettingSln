using ABShared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ABClient.Controllers
{
    /// <summary>
    /// Конвертер % вилки
    /// </summary>
    [ValueConversion(typeof(double), typeof(string))]
    class CalculatorConverterForkProfit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rez = ((double)value).ToString("#.#", culture);
            if ((double)value < 1 && (double)value > -1)
                rez = ((double)value).ToString("0.#", culture);
            return rez + "%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }


    /// <summary>
    /// Конвертер из флага в цвет
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Color))]
    class CalculatorConverterForkCheck : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var o = (bool)value;
            if (o == true)
                return Colors.Green;
            else
                return Colors.Red;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }


    /// <summary>
    /// Конвертер для 2х значного числа
    /// </summary>
    [ValueConversion(typeof(double), typeof(string))]
    class CalculatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rez = ((double)value).ToString("#.##", culture);
            if (((double)value) < 1 && ((double)value) > -1)
                rez = "0" + rez;
            return rez;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// Конвертер для 2х значного числа
    /// </summary>
    [ValueConversion(typeof(float), typeof(string))]
    class CalculatorConverterFloat : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rez = ((float)value).ToString("#.##", culture);
            if (((float)value) < 1 && ((float)value) > -1)
                rez = "0" + rez;
            return rez;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// Конвертирует число с нужными нулями в бух.Калькуляторе
    /// </summary>
    [ValueConversion(typeof(int), typeof(string))]
    class BookkeepConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string rez = ((int)value).ToString("D8");
            return rez;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }


    /// <summary>
    /// Конвертер вида спорта в шрифт иконки
    /// </summary>
    [ValueConversion(typeof(SportType), typeof(string))]
    class SportsToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sport = (SportType)Enum.Parse(typeof(SportType), value.ToString());
            switch (sport)
            {
                case SportType.Бадминтон:
                    {
                        return "";
                    }
                case SportType.Бейсбол:
                    {
                        return "";
                    }
                case SportType.Баскетбол:
                    {
                        return "";
                    }
                case SportType.Футбол:
                    {
                        return "";
                    }
                case SportType.Футзал:
                    {
                        return "";
                    }
                case SportType.Хоккей:
                    {
                        return "";
                    }
                case SportType.Хоккей_с_мячом:
                    {
                        return "";
                    }
                case SportType.Настольный_теннис:
                    {
                        return "";
                    }
                case SportType.Регби:
                    {
                        return "";
                    }
                case SportType.Снукер:
                    {
                        return "";
                    }
                case SportType.Теннис:
                    {
                        return "";
                    }
                case SportType.Волейбол:
                    {
                        return "";
                    }
                case SportType.Гандбол:
                    {
                        return "";
                    }
                case SportType.Водное_поло:
                    {
                        return "";
                    }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

}
