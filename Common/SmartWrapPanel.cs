using Windows.Foundation;

namespace Tunetastic.Common;

/// <summary>
/// Represents a custom panel that arranges its child elements in a wrapping layout with
/// configurable width, height, and spacing for each item.
/// </summary>
/// <remarks>
/// The <see cref="SmartWrapPanel"/> provides a dynamic layout system where items are
/// arranged in rows with wrapping, based on the specified tile dimensions. This panel
/// also supports customizable gaps between items and rows, making it ideal for scenarios
/// requiring responsive design or a grid-like appearance.
/// </remarks>
public class SmartWrapPanel : Panel
{
	/// <summary>
	/// Identifies the TileWidth dependency property, which specifies the width of each tile within the <see cref="SmartWrapPanel"/>.
	/// </summary>
	/// <remarks>
	/// This property determines the horizontal size, in pixels, of individual tiles in the panel.
	/// It is a dependency property designed to support data binding, inheritance, and dynamic runtime updates.
	/// </remarks>
	public static readonly DependencyProperty TileWidthProperty =
		DependencyProperty.Register(nameof(TileWidth), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(160.0));

	/// <summary>
	/// Identifies the TileWidth dependency property, which specifies the width of individual tiles within the <see cref="SmartWrapPanel"/>.
	/// </summary>
	/// <remarks>
	/// This property defines the width, in pixels, of each tile displayed in the panel.
	/// It is a dependency property that supports features such as data binding and property value inheritance.
	/// </remarks>
	public double TileWidth
	{
		get => (double)GetValue(TileWidthProperty);
		set => SetValue(TileWidthProperty, value);
	}

	/// <summary>
	/// Identifies the TileHeight dependency property, which specifies the height of individual tiles in the <see cref="SmartWrapPanel"/>.
	/// </summary>
	/// <remarks>
	/// This property determines the vertical size, in pixels, of each tile within the panel.
	/// It is a dependency property that supports data binding and dynamic updates.
	/// </remarks>
	public static readonly DependencyProperty TileHeightProperty =
		DependencyProperty.Register(nameof(TileHeight), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(100.0));

	/// <summary>
	/// Identifies the TileHeight dependency property, which determines the height of each tile in the <see cref="SmartWrapPanel"/>.
	/// </summary>
	/// <remarks>
	/// This property specifies the height, in pixels, of individual tiles within the panel.
	/// It is a dependency property that supports dynamic updates and data binding.
	/// </remarks>
	public double TileHeight
	{
		get => (double)GetValue(TileHeightProperty);
		set => SetValue(TileHeightProperty, value);
	}

	/// <summary>
	/// Identifies the MinGap dependency property, which specifies the minimum spacing between elements in the <see cref="SmartWrapPanel"/>.
	/// </summary>
	/// <remarks>
	/// This property defines the smallest allowable distance, in pixels, between adjacent elements within the panel.
	/// It is a dependency property that supports dynamic updates and data binding.
	/// </remarks>
	public static readonly DependencyProperty MinGapProperty =
		DependencyProperty.Register(nameof(MinGap), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(8.0));

	/// <summary>
	/// Identifies the MinGap dependency property, which defines the minimum spacing between tiles within the <see cref="SmartWrapPanel"/>.
	/// </summary>
	/// <remarks>
	/// This property specifies the smallest allowable gap, in pixels, between tiles when they are arranged within the panel.
	/// It is a dependency property that supports features such as data binding and property value inheritance.
	/// </remarks>
	public double MinGap
	{
		get => (double)GetValue(MinGapProperty);
		set => SetValue(MinGapProperty, value);
	}

	/*public static readonly DependencyProperty MaxGapProperty =
		DependencyProperty.Register(nameof(MaxGap), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(24.0));

	public double MaxGap
	{
		get => (double)GetValue(MaxGapProperty);
		set => SetValue(MaxGapProperty, value);
	}*/

	/// <summary>
	/// Identifies the RowGap dependency property, which determines the vertical spacing between rows in the <see cref="SmartWrapPanel"/>.
	/// </summary>
	/// <remarks>
	/// This property specifies the distance in pixels between consecutive rows of elements within the panel.
	/// It is a dependency property that allows for dynamic updating and data binding.
	/// </remarks>
	public static readonly DependencyProperty RowGapProperty =
		DependencyProperty.Register(nameof(RowGap), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(12.0));

	/// <summary>
	/// Gets or sets the vertical spacing between rows in the <see cref="SmartWrapPanel"/>.
	/// </summary>
	/// <remarks>
	/// Specifies the distance in pixels between consecutive rows of elements in the panel.
	/// This property supports dynamic updates and data binding.
	/// </remarks>
	public double RowGap
	{
		get => (double)GetValue(RowGapProperty);
		set => SetValue(RowGapProperty, value);
	}

	/// <summary>
	/// A private field used to store a reference to the framework element
	/// that determines the width of the ListView.
	/// </summary>
	/// <remarks>
	/// This field is indirectly modified through the public property <see cref="ListViewWidthSource"/>.
	/// It is assigned the FrameworkElement that acts as the source for measuring the width.
	/// The <see cref="FrameworkElement.SizeChanged"/> event is subscribed to in order
	/// to trigger re-measurement when the size of the source changes.
	/// </remarks>
	private FrameworkElement _listViewWidthSource;

	/// <summary>
	/// Gets or sets the FrameworkElement that serves as the source for determining the width of the ListView in the layout calculation.
	/// </summary>
	/// <remarks>
	/// This property is used in the layout logic to dynamically measure the available width for arranging elements in the <see cref="SmartWrapPanel"/>.
	/// The <see cref="FrameworkElement.SizeChanged"/> event is automatically subscribed to, ensuring that any changes to the size of the source will trigger a remeasurement of the panel.
	/// </remarks>
	public FrameworkElement ListViewWidthSource
	{
		get => _listViewWidthSource;
		set
		{
			if (_listViewWidthSource != value)
			{
				_listViewWidthSource = value;
				_listViewWidthSource.SizeChanged += (_, __) => InvalidateMeasure();
			}
		}
	}

	private double _finalLayoutWidth;
	private int _columnCount = 1;

	/// <summary>
	/// Represents the calculated horizontal gap between elements in the SmartWrapPanel layout.
	/// The value is determined dynamically based on the available width, the number of columns,
	/// and the minimum gap constraint.
	/// </summary>
	private double _calculatedGap = 8;

	/// <summary>
	/// Measures the child elements of the panel to determine the desired size of the panel.
	/// Calculates the layout based on available size and properties such as tile dimensions, gaps, and row spacing.
	/// </summary>
	/// <param name="availableSize">The available size provided by the parent element for the panel's layout.</param>
	/// <returns>The size that the panel determines it requires during the layout process.</returns>
	protected override Size MeasureOverride(Size availableSize)
	{
		CalculateLayout(availableSize);
		foreach (UIElement child in Children)
			child.Measure(new Size(TileWidth, TileHeight));

		int rows = (int)Math.Ceiling((double)Children.Count / _columnCount);
		double totalHeight = rows > 0 ? rows * TileHeight + (rows - 1) * RowGap : 0;

		return new Size(_finalLayoutWidth, totalHeight);
	}

	/// <summary>
	/// Arranges the child elements of the panel within the specified final size.
	/// Distributes the child elements into rows and columns based on the calculated layout,
	/// adjusting positions to account for tile dimensions, gaps, and row spacing.
	/// </summary>
	/// <param name="finalSize">The final area within the parent that the panel should use to arrange its children.</param>
	/// <returns>The actual size used by the panel after arranging the child elements.</returns>
	protected override Size ArrangeOverride(Size finalSize)
	{
		CalculateLayout(finalSize);

		int index = 0;
		foreach (UIElement child in Children)
		{
			int row = index / _columnCount;
			int col = index % _columnCount;

			double x = _calculatedGap + col * (TileWidth + _calculatedGap);
			double y = row * (TileHeight + RowGap);

			child.Arrange(new Rect(new Point(x, y), new Size(TileWidth, TileHeight)));
			index++;
		}

		int rows = (int)Math.Ceiling((double)Children.Count / _columnCount);
		double totalHeight = RowGap + rows * TileHeight + (rows - 1) * RowGap;

		return new Size(_finalLayoutWidth, totalHeight);
	}

	/// <summary>
	/// Calculates the layout for the child elements of the panel based on the available size.
	/// Determines the number of columns, spacing, and final layout width to optimize child positioning.
	/// </summary>
	/// <param name="size">The available size provided by the parent panel for layout calculations.</param>
	private void CalculateLayout(Size size)
	{
		double containerWidth = (ListViewWidthSource?.ActualWidth ?? size.Width) - GetListViewHorizontalPadding();
		_finalLayoutWidth = containerWidth;

		_columnCount = 1;
		_calculatedGap = MinGap;

		int bestColumnCount = 1;
		double bestGap = MinGap;

		for (int cols = 1; cols < 100; cols++)
		{
			double tileSpace = TileWidth * cols;
			double gapSpace = containerWidth - tileSpace;
			double testGap = gapSpace / (cols + 1);

			if (testGap < MinGap)
			{
				break;
			}

			bestColumnCount = cols;
			bestGap = testGap;
		}

		_columnCount = bestColumnCount;
		_calculatedGap = bestGap;

		/*bool foundIdeal = false;
		int fallbackColumnCount = 1;
		double fallbackGap = MinGap;

		for (int cols = 1; cols < 100; cols++)
		{
			double tileSpace = TileWidth * cols;
			double gapSpace = containerWidth - tileSpace;
			double testGap = gapSpace / (cols + 1);

			if (testGap >= MinGap && testGap <= MaxGap)
			{
				_columnCount = cols;
				_calculatedGap = testGap;
				foundIdeal = true;
			}
			else if (!foundIdeal && testGap >= MinGap)
			{
				fallbackColumnCount = cols;
				fallbackGap = testGap;
			}
			else if (testGap < MinGap)
			{
				break;
			}
		}

		if (!foundIdeal && fallbackGap >= MinGap)
		{
			_columnCount = fallbackColumnCount;
			_calculatedGap = fallbackGap;
		}*/
	}

	/// <summary>
	/// Retrieves the total horizontal padding of the ListViewWidthSource, combining the left and right padding values.
	/// Ensures adjustments for child element layout within the panel based on the desired spacing and alignment.
	/// </summary>
	/// <returns>The combined left and right padding value of the ListViewWidthSource if it is a control; otherwise, 0.</returns>
	private double GetListViewHorizontalPadding()
	{
		if (ListViewWidthSource is Control control)
		{
			var padding = control.Padding;
			return padding.Left + padding.Right;
		}
		return 0;
	}
}
