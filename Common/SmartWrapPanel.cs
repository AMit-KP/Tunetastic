using Windows.Foundation;

namespace Tunetastic.Common;

public class SmartWrapPanel : Panel
{
	public static readonly DependencyProperty TileWidthProperty =
		DependencyProperty.Register(nameof(TileWidth), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(160.0));

	public double TileWidth
	{
		get => (double)GetValue(TileWidthProperty);
		set => SetValue(TileWidthProperty, value);
	}

	public static readonly DependencyProperty TileHeightProperty =
		DependencyProperty.Register(nameof(TileHeight), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(100.0));

	public double TileHeight
	{
		get => (double)GetValue(TileHeightProperty);
		set => SetValue(TileHeightProperty, value);
	}

	public static readonly DependencyProperty MinGapProperty =
		DependencyProperty.Register(nameof(MinGap), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(8.0));

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

	public static readonly DependencyProperty RowGapProperty =
		DependencyProperty.Register(nameof(RowGap), typeof(double), typeof(SmartWrapPanel), new PropertyMetadata(12.0));

	public double RowGap
	{
		get => (double)GetValue(RowGapProperty);
		set => SetValue(RowGapProperty, value);
	}

	private FrameworkElement _listViewWidthSource;

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
	private double _calculatedGap = 8;

	protected override Size MeasureOverride(Size availableSize)
	{
		CalculateLayout(availableSize);
		foreach (UIElement child in Children)
			child.Measure(new Size(TileWidth, TileHeight));

		int rows = (int)Math.Ceiling((double)Children.Count / _columnCount);
		double totalHeight = rows > 0 ? rows * TileHeight + (rows - 1) * RowGap : 0;

		return new Size(_finalLayoutWidth, totalHeight);
	}

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
