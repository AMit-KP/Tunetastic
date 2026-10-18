using FFmpeg.AutoGen;

namespace Tunetastic.Common.Helpers;

public static unsafe class FFmpegProbe
{
	private static int _initialized;

	public static TimeSpan? GetDuration(string filePath)
	{
		// Must run BEFORE the very first call into any ffmpeg function
		if (Interlocked.Exchange(ref _initialized, 1) == 0)
			ffmpeg.RootPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FFmpeg");

		AVFormatContext* ctx = null;
		try
		{
			if (ffmpeg.avformat_open_input(&ctx, filePath, null, null) < 0 || ctx == null)
				return null;

			if (ffmpeg.avformat_find_stream_info(ctx, null) < 0)
				return null;

			// duration is in microseconds
			return ctx->duration > 0 ? TimeSpan.FromTicks(ctx->duration * 10) : null;
		}
		catch (DllNotFoundException)
		{
			return null; // let your existing TagLib value stand
		}
		finally
		{
			if (ctx != null)
				ffmpeg.avformat_close_input(&ctx);
		}
	}
}
