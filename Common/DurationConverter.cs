using Microsoft.UI.Xaml.Data;

namespace Tunetastic.Common;

/// <summary>
/// Converts a duration in seconds to a human-readable formatted string representation.
/// </summary>
/// <remarks>
/// The <c>DurationConverter</c> class is designed to transform a numeric duration value in seconds into a string that represents
/// the duration in a readable time format. It formats the output as "HH:mm:ss" for durations longer than or equal to one hour,
/// and as "mm:ss" for shorter durations. Commonly used in XAML for data bindings, this class enables the seamless formatting
/// of duration values for display purposes. It implements the <see cref="IValueConverter"/> interface to support the conversion logic
/// required in XAML applications. The class does not support reverse conversions and will throw a <see cref="NotImplementedException"/>
/// when attempting to use the <see cref="ConvertBack"/> method.
/// </remarks>
public class DurationConverter : IValueConverter
{
    /// <summary>
    /// Converts a duration in seconds into a formatted, human-readable string representation.
    /// </summary>
    /// <param name="value">The duration in seconds to be converted, represented as an object, typically a <see cref="double"/>.</param>
    /// <param name="targetType">The type of the binding target property. Not used in this implementation.</param>
    /// <param name="parameter">Additional parameter for the converter. Not used in this implementation.</param>
    /// <param name="language">The culture or language information for conversion. Not used in this implementation.</param>
    /// <returns>A formatted duration string in the "HH:mm:ss" or "mm:ss" format, depending on the duration length.</returns>
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

    /// <summary>
    /// Attempts to convert a formatted, human-readable duration string back into its numeric representation in seconds.
    /// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="value">The duration string to convert back, typically a <see cref="string"/>.</param>
    /// <param name="targetType">The type of the binding target property. Not used in this implementation.</param>
    /// <param name="parameter">Additional parameter for the converter. Not used in this implementation.</param>
    /// <param name="language">The culture or language information for the conversion. Not used in this implementation.</param>
    /// <returns>This method does not return successfully as it throws an exception.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

