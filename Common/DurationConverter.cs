using Microsoft.UI.Xaml.Data;

namespace Tunetastic.Common;
public class DurationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double durationInSeconds)
        {
            TimeSpan duration = TimeSpan.FromSeconds(durationInSeconds);

            if (duration.TotalHours >= 1)
            {
                return string.Format("{0:00}:{1:00}:{2:00}", (int)duration.TotalHours, duration.Minutes, duration.Seconds);
            }
            else
            {
                return string.Format("{0:00}:{1:00}", duration.Minutes, duration.Seconds);
            }
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

