using System.Globalization;
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

/// <summary>
/// Converts a <see cref="DateTime"/> object into a string formatted according to the specified format string.
/// </summary>
/// <remarks>
/// The <c>DateFormatConverter</c> class is designed to format <see cref="DateTime"/> values into string representations
/// based on a specified format string. It supports customization by allowing the format string to be passed as a parameter.
/// If no format string is provided or if the provided format string is invalid, the conversion falls back to using the
/// system's short date pattern. Commonly used in XAML for data bindings, this class provides a flexible way to display
/// dates in various formats suited to the application's requirements.
/// It implements the <see cref="IValueConverter"/> interface to facilitate its use in XAML data binding scenarios.
/// The class does not support reverse conversions and will throw a <see cref="NotImplementedException"/> when attempting to
/// use the <see cref="ConvertBack"/> method.
/// </remarks>
public class DateFormatConverter : IValueConverter
{
	/// <summary>
	/// Converts a <see cref="DateTime"/> into a formatted string representation based on a specified format string.
	/// </summary>
	/// <param name="value">The value to be converted, expected to be a <see cref="DateTime"/> object.</param>
	/// <param name="targetType">The target type of the binding. This parameter is not used in the implementation.</param>
	/// <param name="parameter">An optional format string to define how the <see cref="DateTime"/> should be formatted.</param>
	/// <param name="language">A string representing the language information for the conversion. This parameter is not used in the implementation.</param>
	/// <returns>A formatted string representing the date, using either the specified format or the system's short date pattern if the format is not provided or invalid.</returns>
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is DateTime dt)
		{
			string format = parameter as string;

			if (string.IsNullOrEmpty(format))
				return dt.ToString("d", CultureInfo.CurrentCulture);

			try
			{
				return dt.ToString(format, CultureInfo.CurrentCulture);
			}
			catch
			{
				return dt.ToString("d", CultureInfo.CurrentCulture);
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// Throws a <see cref="NotImplementedException"/> as reverse conversion is not supported in this implementation.
	/// </summary>
	/// <param name="value">The value being passed for reverse conversion. Not used in this implementation.</param>
	/// <param name="targetType">The type of the binding target property. Not used in this implementation.</param>
	/// <param name="parameter">Additional parameter for the converter. Not used in this implementation.</param>
	/// <param name="language">The culture or language information for conversion. Not used in this implementation.</param>
	/// <returns>This method does not return a value as it throws a <see cref="NotImplementedException"/>.</returns>
	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}

/// <summary>
/// Converts a <see cref="DateTime"/> value into a relative time description such as "2 seconds ago" or "2 days ago".
/// </summary>
/// <remarks>
/// The <c>RelativeTimeConverter</c> class is designed for generating user-friendly, human-readable representations of time differences
/// between the current time and a given <see cref="DateTime"/> value. It provides formatted output such as "20 seconds ago",
/// "10 minutes ago", "2 hours ago", among others, depending on how much time has passed.
/// It implements the <see cref="IValueConverter"/> interface for use in XAML-based applications, making it easy to bind temporal data
/// and present it in a simplified manner. The class does not implement reverse conversions and will throw a
/// <see cref="NotImplementedException"/> if the <see cref="ConvertBack"/> method is invoked.
/// </remarks>
public class RelativeTimeConverter : IValueConverter
{
	/// <summary>
	/// Converts a <see cref="DateTime"/> value into a relative time description such as "10 seconds ago" or "2 days ago".
	/// </summary>
	/// <param name="value">An object representing the <see cref="DateTime"/> value to be converted into a relative time description.</param>
	/// <param name="targetType">The type of the binding target property. Not used in this implementation.</param>
	/// <param name="parameter">Additional parameter for the converter. Not used in this implementation.</param>
	/// <param name="language">The culture or language information for the conversion. Not used in this implementation.</param>
	/// <returns>A string containing a user-friendly, relative time description based on the difference between the current time and the provided <see cref="DateTime"/> value.</returns>
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not DateTime dateTime)
			return string.Empty;

		var now = DateTime.Now;
		var diff = now - dateTime;

		if (diff.TotalSeconds < 60)
			return $"{(int)diff.TotalSeconds} second{(diff.TotalSeconds >= 2 ? "s" : "")} ago";
		if (diff.TotalMinutes < 60)
			return $"{(int)diff.TotalMinutes} minute{(diff.TotalMinutes >= 2 ? "s" : "")} ago";
		if (diff.TotalHours < 24)
			return $"{(int)diff.TotalHours} hour{(diff.TotalHours >= 2 ? "s" : "")} ago";
		if (diff.TotalDays < 7)
			return $"{(int)diff.TotalDays} day{(diff.TotalDays >= 2 ? "s" : "")} ago";
		if (diff.TotalDays < 365)
			return $"{(int)(diff.TotalDays / 7)} week{(diff.TotalDays / 7 >= 2 ? "s" : "")} ago";

		return $"{(int)(diff.TotalDays / 365)} year{(diff.TotalDays / 365 >= 2 ? "s" : "")} ago";
	}

	/// <summary>
	/// This method is not implemented. It is designed to reverse the transformation of a value in a data-binding scenario, but
	/// it will always throw a <see cref="NotImplementedException"/> in this implementation.
	/// </summary>
	/// <param name="value">The binding target value, which would be converted back to the source. Not used in this implementation.</param>
	/// <param name="targetType">The data type to which the value would be converted. Not used in this implementation.</param>
	/// <param name="parameter">An optional parameter to be used during the conversion. Not used in this implementation.</param>
	/// <param name="language">The culture or language information for conversion. Not used in this implementation.</param>
	/// <returns>Throws a <see cref="NotImplementedException"/> because this method is not implemented.</returns>
	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}
