using Windows.Foundation;

namespace Tunetastic.Common.Controls;

/// <summary>
/// A virtualizing layout that arranges items in a smart wrapping grid with configurable tile sizes and gaps.
/// </summary>
public class SmartWrapVirtualizingLayout : VirtualizingLayout
{
	/// <summary>
	/// Gets or sets the width of each tile in the layout.
	/// </summary>
	public double TileWidth { get; set; } = 160;

	/// <summary>
	/// Gets or sets the height of each tile in the layout.
	/// </summary>
	public double TileHeight { get; set; } = 100;

	/// <summary>
	/// Gets or sets the minimum gap between tiles.
	/// </summary>
	public double MinGap { get; set; } = 8;

	/// <summary>
	/// Gets or sets the vertical gap between rows of tiles.
	/// </summary>
	public double RowGap { get; set; } = 12;

	private double _calculatedGap = 8;
	private int _columnCount = 1;
	private double _finalLayoutWidth;

	/// <summary>
	/// Initializes the layout context for use with this layout.
	/// </summary>
	/// <param name="context">The virtualizing layout context to initialize.</param>
	protected override void InitializeForContextCore(VirtualizingLayoutContext context)
	{
		base.InitializeForContextCore(context);
		context.LayoutState = new LayoutState();
	}

	/// <summary>
	/// Measures the layout elements based on the available size.
	/// </summary>
	/// <param name="context">The virtualizing layout context.</param>
	/// <param name="availableSize">The available size for the layout.</param>
	/// <returns>The desired size of the layout.</returns>
	protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
	{
		CalculateLayout(context, availableSize);
		int itemCount = context.ItemCount;

		if (itemCount == 0)
		{
			return new Size(_finalLayoutWidth, 0);
		}

		for (int i = 0; i < itemCount; i++)
		{
			var element = context.GetOrCreateElementAt(i);
			element.Measure(new Size(TileWidth, TileHeight));
		}

		int rows = (int)Math.Ceiling((double)itemCount / _columnCount);
		double totalHeight = rows * TileHeight + (rows - 1) * RowGap;

		return new Size(_finalLayoutWidth, totalHeight);
	}

	/// <summary>
	/// Arranges the layout elements in the final size.
	/// </summary>
	/// <param name="context">The virtualizing layout context.</param>
	/// <param name="finalSize">The final size for the layout.</param>
	/// <returns>The actual size used by the layout.</returns>
	protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
	{
		CalculateLayout(context, finalSize);
		int itemCount = context.ItemCount;
		if (itemCount == 0)
		{
			return new Size(_finalLayoutWidth, 0);
		}

		for (int index = 0; index < itemCount; index++)
		{
			int row = index / _columnCount;
			int col = index % _columnCount;

			double x = _calculatedGap + col * (TileWidth + _calculatedGap);
			double y = row * (TileHeight + RowGap);

			var rect = new Rect(new Point(x, y), new Size(TileWidth, TileHeight));
			var element = context.GetOrCreateElementAt(index);
			element.Arrange(rect);
		}

		int rows = (int)Math.Ceiling((double)itemCount / _columnCount);
		double totalHeight = rows * TileHeight + (rows - 1) * RowGap;


		return new Size(_finalLayoutWidth, totalHeight);
	}

	/// <summary>
	/// Calculates the layout parameters based on the available size.
	/// </summary>
	/// <param name="context">The virtualizing layout context.</param>
	/// <param name="size">The available size for calculation.</param>
	private void CalculateLayout(VirtualizingLayoutContext context, Size size)
	{
		double containerWidth = size.Width;
		_finalLayoutWidth = containerWidth;

		_finalLayoutWidth = containerWidth;

		int bestColumnCount = 1;
		double bestGap = MinGap;

		for (int cols = 1; cols < 100; cols++)
		{
			double tileSpace = TileWidth * cols;
			double gapSpace = containerWidth - tileSpace;
			double testGap = gapSpace / (cols + 1);

			if (testGap < MinGap)
				break;

			bestColumnCount = cols;
			bestGap = testGap;
		}

		_columnCount = bestColumnCount;
		_calculatedGap = bestGap;
	}

	/// <summary>
	/// Gets the horizontal padding of a framework element.
	/// </summary>
	/// <param name="element">The framework element to get padding from.</param>
	/// <returns>The sum of left and right padding values.</returns>
	private double GetHorizontalPadding(FrameworkElement element)
	{
		if (element is Control control)
		{
			var padding = control.Padding;
			return padding.Left + padding.Right;
		}
		return 0;
	}

	/// <summary>
	/// Represents the state of the layout for caching purposes.
	/// </summary>
	private class LayoutState { } // Reserved for future layout caching, if needed
}
