using System;
using System.Globalization;
using Xamarin.Forms;

namespace BantayanEmergencyApp.Converterss
{
    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status && status == "Reported")
            {
                return true; // Show the ReportReason label if status is "Reported"
            }

            return false; // Hide the ReportReason label for any other status
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
