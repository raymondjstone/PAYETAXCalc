using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace PAYETAXCalc.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool boolValue = value is bool b && b;
            if (parameter is string s && s == "Invert")
                boolValue = !boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool result = value is Visibility v && v == Visibility.Visible;
            if (parameter is string s && s == "Invert")
                result = !result;
            return result;
        }
    }

    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal d)
                return $"£{d:N2}";
            if (value is double dbl)
                return $"£{dbl:N2}";
            return "£0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class OverUnderPaymentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal d)
            {
                if (d > 0)
                    return $"Underpaid: £{d:N2}";
                else if (d < 0)
                    return $"Overpaid (refund due): £{Math.Abs(d):N2}";
                else
                    return "Exactly correct";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
