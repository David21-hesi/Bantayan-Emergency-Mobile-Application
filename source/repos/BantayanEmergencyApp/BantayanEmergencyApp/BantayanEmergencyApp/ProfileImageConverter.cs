using System;
using System.Globalization;
using System.IO;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BantayanEmergencyApp
{
    public class ProfileImageConverter : IValueConverter
    {
        private const string ProfileImageKey = "ProfileImagePath";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string email && !string.IsNullOrEmpty(email))
            {
                // Check if a profile image path exists in preferences for this user
                var imageKey = $"{ProfileImageKey}_{email}";
                if (Preferences.ContainsKey(imageKey))
                {
                    var imagePath = Preferences.Get(imageKey, string.Empty);
                    if (File.Exists(imagePath))
                    {
                        return ImageSource.FromFile(imagePath);
                    }
                }
            }

            // Return default user.png if no profile image is found
            return "user.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}