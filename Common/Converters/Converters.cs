using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Common.Converters;

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
				return string.Format("{0:00}:{1:00}:{2:00}", duration.Hours, duration.Minutes, duration.Seconds);
			}
			else
			{
				return string.Format("{0:00}:{1:00}", duration.Minutes, duration.Seconds);
			}
		}
		return string.Empty;
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
/// Converts a duration in seconds into a detailed, human-readable time format string.
/// </summary>
/// <remarks>
/// The <c>DurationToFullTimeConverter</c> class is aimed at transforming a numeric duration value, expressed in seconds,
/// into a time format string with greater detail. The result is formatted as "X hours Y mins Z secs",
/// "Y mins Z secs", or "Z secs" depending on the total duration. It emphasizes a clear verbal display of time durations
/// for enhanced readability and user understanding. This converter is primarily designed for use within XAML bindings.
/// It implements the <see cref="IValueConverter"/> interface, supporting forward conversions via the
/// <see cref="Convert"/> method. Reverse conversion through the <see cref="ConvertBack"/> method is not supported
/// and will throw a <see cref="NotImplementedException"/> if called.
/// </remarks>
public class DurationToFullTimeConverter : IValueConverter
{
	/// <summary>
	/// Converts a duration in seconds into a detailed, human-readable time format string.
	/// </summary>
	/// <param name="value">The duration in seconds to be converted, represented as an object, typically a <see cref="double"/>.</param>
	/// <param name="targetType">The type of the binding target property. Not used in this implementation.</param>
	/// <param name="parameter">Additional parameter for the converter. Not used in this implementation.</param>
	/// <param name="language">The culture or language information for conversion. Not used in this implementation.</param>
	/// <returns>A detailed time representation in the format "X hours Y mins Z secs", "Y mins Z secs", or "Z secs", depending on the duration length.</returns>
	public object Convert(object value, Type? targetType, object? parameter, string? language)
	{
		if (value is double seconds && seconds >= 0)
		{
			TimeSpan duration = TimeSpan.FromSeconds(seconds);
			if (duration.TotalHours >= 1)
			{
				return $"{duration.Hours} hour{(duration.Hours == 1 ? "" : "s")} " +
					   $"{duration.Minutes} min{(duration.Minutes == 1 ? "" : "s")} " +
					   $"{duration.Seconds} sec{(duration.Seconds == 1 ? "" : "s")}";
			}
			else if (duration.TotalMinutes >= 1)
			{
				return $"{duration.Minutes} min{(duration.Minutes == 1 ? "" : "s")} " +
					   $"{duration.Seconds} sec{(duration.Seconds == 1 ? "" : "s")}";
			}
			else
			{
				return $"{duration.Seconds} sec{(duration.Seconds == 1 ? "" : "s")}";
			}
		}
		return "0 sec";
	}

	/// <summary>
	/// Not implemented. Reverse conversion from a formatted duration string to the original duration value is not supported.
	/// </summary>
	/// <param name="value">The value produced by the binding target. This parameter is not used.</param>
	/// <param name="targetType">The type of the binding target property. This parameter is not used.</param>
	/// <param name="parameter">An optional parameter for the converter. This parameter is not used.</param>
	/// <param name="language">The culture or language information. This parameter is not used.</param>
	/// <returns>Throws a <see cref="NotImplementedException"/> as the method is not implemented.</returns>
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
	public object Convert(object value, Type? targetType, object parameter, string? language)
	{
		if (value is DateTime dt)
		{
			string? format = parameter as string;

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
	public object Convert(object value, Type? targetType, object? parameter, string? language)
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

/// <summary>
/// Converts a song's track number into its display text.
/// </summary>
/// <remarks>
/// The <c>TrackNumberConverter</c> class formats a nullable track number for display in XAML bindings. A <c>null</c>
/// value (no track number stored) converts to an empty string, while a stored value — including <c>0</c> — renders
/// as its number. When the binding passes <c>"Hash"</c> as the converter parameter, the number is prefixed with "#"
/// for header-less layouts such as the compact view. Reverse conversion through the <see cref="ConvertBack"/> method
/// is not supported and will throw a <see cref="NotImplementedException"/> if called.
/// </remarks>
public class TrackNumberConverter : IValueConverter
{
	/// <summary>
	/// Converts a nullable track number into its display text.
	/// </summary>
	/// <param name="value">The track number to convert, typically a boxed <see cref="int"/> or <c>null</c>.</param>
	/// <param name="targetType">The type of the binding target property. Not used in this implementation.</param>
	/// <param name="parameter">Pass <c>"Hash"</c> to prefix the number with "#"; any other value renders the bare number.</param>
	/// <param name="language">The culture or language information for conversion. Not used in this implementation.</param>
	/// <returns>An empty string when the value is null; otherwise the number, optionally prefixed with "#".</returns>
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not int track)
			return string.Empty;

		return parameter is "Hash" ? $"#{track}" : track.ToString();
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

/// <summary>
/// Turns a cover thumbnail file path into a cached, decode-sized <see cref="BitmapImage"/> for use in
/// virtualized lists and tile grids.
/// </summary>
/// <remarks>
/// Cover thumbnails are stored on disk as PNGs and bound into <c>Image.Source</c> by path. Without this
/// converter every container realization builds a fresh <see cref="BitmapImage"/> and re-decodes the file
/// from disk — and scrolling back re-decodes the same covers again. This converter:
/// <list type="bullet">
///   <item><description>caches one decoded <see cref="BitmapImage"/> per path, so realizations and scroll-back reuse it;</description></item>
///   <item><description>caps the decode size via <see cref="BitmapImage.DecodePixelWidth"/>/<see cref="BitmapImage.DecodePixelHeight"/> to the on-screen tile size, cutting decode cost and memory.</description></item>
/// </list>
/// The cache is process-wide and bounded: when it grows past <see cref="MaxCacheEntries"/> (a safety valve
/// for pathologically large libraries) it is cleared once, trading a rare re-decode burst for bounded
/// memory. Must be evaluated on the UI thread (as all bindings are).
/// </remarks>
public class CoverImageConverter : IValueConverter
{
	/// <summary>On-screen decode size (px) for the small thumbnails used in lists and grids.</summary>
	private const int DecodeSize = 200;

	/// <summary>Upper bound on cached entries before the cache is cleared as a memory safety valve.</summary>
	private const int MaxCacheEntries = 3000;

	private static readonly ConcurrentDictionary<string, BitmapImage> Cache = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Converts a cover path (or empty/null) into a cached, decode-sized <see cref="BitmapImage"/>.</summary>
	/// <param name="value">The cover file path. Empty/null yields <c>null</c> so the Image shows nothing.</param>
	/// <param name="targetType">Unused.</param>
	/// <param name="parameter">Unused.</param>
	/// <param name="language">Unused.</param>
	/// <returns>A cached <see cref="BitmapImage"/>, or <c>null</c> when there is no cover.</returns>
	public object? Convert(object? value, Type targetType, object? parameter, string language)
	{
		var path = value as string;
		if (string.IsNullOrEmpty(path))
			return null;

		if (Cache.TryGetValue(path, out var existing))
			return existing;

		if (Cache.Count >= MaxCacheEntries)
			Cache.Clear();

		return Cache.GetOrAdd(path, CreateImage);
	}

	/// <summary>Builds a decode-sized <see cref="BitmapImage"/> for the given path. Called at most once per path (until a cache clear).</summary>
	private static BitmapImage CreateImage(string path)
	{
		var image = new BitmapImage
		{
			DecodePixelWidth = DecodeSize,
			DecodePixelHeight = DecodeSize
		};
		image.UriSource = new Uri(path);
		return image;
	}

	/// <summary>Not supported — this is a one-way converter.</summary>
	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}
