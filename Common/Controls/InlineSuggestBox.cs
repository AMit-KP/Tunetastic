using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Tunetastic.Common.Controls;

public enum SuggestionAcceptKey { RightArrow, Tab, Both }

[TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartGhostBlock, Type = typeof(TextBlock))]
public sealed class InlineSuggestBox : Control
{
	private const string PartTextBox = "PART_TextBox";
	private const string PartGhostBlock = "PART_GhostBlock";
	private const string PartGhostTypedRun = "PART_GhostTypedRun";
	private const string PartGhostRemainderRun = "PART_GhostRemainderRun";

	private TextBox? _textBox;
	private TextBlock? _ghostBlock;
	private Run? _ghostTypedRun;
	private Run? _ghostRemainderRun;

	private List<string> _matches = new();
	private int _matchIndex = -1;
	private string _currentFullMatch = string.Empty;

	// ── Dependency Properties ────────────────────────────────────────────

	public static readonly DependencyProperty SuggestionSourceProperty =
		DependencyProperty.Register(nameof(SuggestionSource), typeof(IList<string>),
			typeof(InlineSuggestBox), new PropertyMetadata(null,
				(d, _) => ((InlineSuggestBox)d).RefreshSuggestion()));

	public IList<string>? SuggestionSource
	{
		get => (IList<string>?)GetValue(SuggestionSourceProperty);
		set => SetValue(SuggestionSourceProperty, value);
	}

	public static readonly DependencyProperty SuggestionSuffixProperty =
		DependencyProperty.Register(nameof(SuggestionSuffix), typeof(string),
			typeof(InlineSuggestBox), new PropertyMetadata(string.Empty,
				(d, _) => ((InlineSuggestBox)d).UpdateGhost()));

	public string SuggestionSuffix
	{
		get => (string)GetValue(SuggestionSuffixProperty);
		set => SetValue(SuggestionSuffixProperty, value);
	}

	public static readonly DependencyProperty CaseSensitiveSuggestionProperty =
		DependencyProperty.Register(nameof(CaseSensitiveSuggestion), typeof(bool),
			typeof(InlineSuggestBox), new PropertyMetadata(false,
				(d, _) => ((InlineSuggestBox)d).RefreshSuggestion()));

	public bool CaseSensitiveSuggestion
	{
		get => (bool)GetValue(CaseSensitiveSuggestionProperty);
		set => SetValue(CaseSensitiveSuggestionProperty, value);
	}

	public static readonly DependencyProperty AcceptKeyProperty =
		DependencyProperty.Register(nameof(AcceptKey), typeof(SuggestionAcceptKey),
			typeof(InlineSuggestBox), new PropertyMetadata(SuggestionAcceptKey.RightArrow));

	public SuggestionAcceptKey AcceptKey
	{
		get => (SuggestionAcceptKey)GetValue(AcceptKeyProperty);
		set => SetValue(AcceptKeyProperty, value);
	}

	public static readonly DependencyProperty TextProperty =
		DependencyProperty.Register(nameof(Text), typeof(string),
			typeof(InlineSuggestBox), new PropertyMetadata(string.Empty));

	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public static readonly DependencyProperty PlaceholderTextProperty =
		DependencyProperty.Register(nameof(PlaceholderText), typeof(string),
			typeof(InlineSuggestBox), new PropertyMetadata(string.Empty));

	public string PlaceholderText
	{
		get => (string)GetValue(PlaceholderTextProperty);
		set => SetValue(PlaceholderTextProperty, value);
	}

	public static readonly DependencyProperty TextWrappingProperty =
		DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping),
			typeof(InlineSuggestBox), new PropertyMetadata(TextWrapping.NoWrap));

	public TextWrapping TextWrapping
	{
		get => (TextWrapping)GetValue(TextWrappingProperty);
		set => SetValue(TextWrappingProperty, value);
	}

	public static readonly DependencyProperty AcceptsReturnProperty =
		DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool),
			typeof(InlineSuggestBox), new PropertyMetadata(false));

	public bool AcceptsReturn
	{
		get => (bool)GetValue(AcceptsReturnProperty);
		set => SetValue(AcceptsReturnProperty, value);
	}

	public static readonly DependencyProperty IsReadOnlyProperty =
		DependencyProperty.Register(nameof(IsReadOnly), typeof(bool),
			typeof(InlineSuggestBox), new PropertyMetadata(false));

	public bool IsReadOnly
	{
		get => (bool)GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	public static readonly DependencyProperty MaxLengthProperty =
		DependencyProperty.Register(nameof(MaxLength), typeof(int),
			typeof(InlineSuggestBox), new PropertyMetadata(0));

	public int MaxLength
	{
		get => (int)GetValue(MaxLengthProperty);
		set => SetValue(MaxLengthProperty, value);
	}

	// ── Events ───────────────────────────────────────────────────────────

	public event TextChangedEventHandler? TextChanged;
	public event EventHandler<string>? SuggestionAccepted;

	// ── Constructor ──────────────────────────────────────────────────────

	public InlineSuggestBox()
	{
		DefaultStyleKey = typeof(InlineSuggestBox);
	}

	// ── Template ─────────────────────────────────────────────────────────

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (_textBox is not null)
		{
			_textBox.TextChanged -= TextBox_TextChanged;
			_textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
			_textBox.KeyDown -= TextBox_KeyDown;
			_textBox.LayoutUpdated -= TextBox_LayoutUpdated;
		}

		_textBox = GetTemplateChild(PartTextBox) as TextBox;
		_ghostBlock = null;

		if (_textBox is not null)
		{
			_textBox.TextChanged += TextBox_TextChanged;
			_textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			_textBox.AddHandler(
				UIElement.KeyDownEvent,
				new KeyEventHandler(TextBox_KeyDown),
				handledEventsToo: true);

			// PART_GhostBlock is inside the TextBox's own ControlTemplate.
			// Its visual tree is not built until after the first layout pass.
			// LayoutUpdated fires after every layout pass — we hook it once
			// and unhook as soon as we find the element.
			_textBox.LayoutUpdated += TextBox_LayoutUpdated;

			if (!string.IsNullOrEmpty(Text))
				_textBox.Text = Text;
		}
	}

	private void TextBox_LayoutUpdated(object? sender, object e)
	{
		if (_ghostBlock is not null)
		{
			// Already found — unhook to avoid repeated searches every frame
			_textBox!.LayoutUpdated -= TextBox_LayoutUpdated;
			return;
		}

		var found = FindDescendantByName<TextBlock>(_textBox!, PartGhostBlock);
		if (found is null) return; // not in tree yet, wait for next pass

		// Found it — unhook, apply colour, and trigger any pending suggestion
		_textBox!.LayoutUpdated -= TextBox_LayoutUpdated;
		_ghostBlock = found;

		// Get Runs directly from Inlines — VisualTreeHelper cannot find them
		_ghostTypedRun = _ghostBlock.Inlines.OfType<Run>()
			.FirstOrDefault(r => r.Name == PartGhostTypedRun);
		_ghostRemainderRun = _ghostBlock.Inlines.OfType<Run>()
			.FirstOrDefault(r => r.Name == PartGhostRemainderRun);

		// Apply theme disabled-text colour.
		// ThemeResource/TemplateBinding both silently fail on elements inside
		// a nested ControlTemplate in WinUI 3, so we set it in code instead.
		if (Application.Current.Resources.TryGetValue(
		"TextFillColorTertiaryBrush", out var res) && res is Brush brush)
		{
			_ghostBlock.Foreground = brush; // keep as fallback
		}

		// Replay any suggestion that was already computed before ghost was ready
		if (!string.IsNullOrEmpty(_currentFullMatch))
			UpdateGhost();
	}

	// ── Event handlers ───────────────────────────────────────────────────

	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		Text = _textBox!.Text;
		RefreshSuggestion();
		TextChanged?.Invoke(this, e);
	}

	private void TextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
	{
		bool hasSuggestion = !string.IsNullOrEmpty(_currentFullMatch);

		switch (e.Key)
		{
			case Windows.System.VirtualKey.Right
				when hasSuggestion && IsAcceptKey(SuggestionAcceptKey.RightArrow):
				if (_textBox!.SelectionStart == _textBox.Text.Length)
				{
					AcceptSuggestion();
					e.Handled = true;
				}
				break;

			case Windows.System.VirtualKey.Tab
				when hasSuggestion && IsAcceptKey(SuggestionAcceptKey.Tab):
				e.Handled = true;
				break;

			case Windows.System.VirtualKey.Down when _matches.Count > 1:
				_matchIndex = (_matchIndex + 1) % _matches.Count;
				ShowMatch(_matchIndex);
				e.Handled = true;
				break;

			case Windows.System.VirtualKey.Up when _matches.Count > 1:
				_matchIndex = (_matchIndex - 1 + _matches.Count) % _matches.Count;
				ShowMatch(_matchIndex);
				e.Handled = true;
				break;

			case Windows.System.VirtualKey.Escape when hasSuggestion:
				ClearGhost();
				e.Handled = true;
				break;
		}
	}

	private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == Windows.System.VirtualKey.Tab
			&& !string.IsNullOrEmpty(_currentFullMatch)
			&& IsAcceptKey(SuggestionAcceptKey.Tab))
		{
			AcceptSuggestion();
			e.Handled = true;
		}
	}

	// ── Suggestion logic ─────────────────────────────────────────────────

	private void RefreshSuggestion()
	{
		string typed = _textBox?.Text ?? string.Empty;

		if (string.IsNullOrEmpty(typed) || SuggestionSource is null)
		{
			_matches.Clear();
			_matchIndex = -1;
			_currentFullMatch = string.Empty;
			ClearGhost();
			return;
		}

		var comparison = CaseSensitiveSuggestion
			? StringComparison.Ordinal
			: StringComparison.OrdinalIgnoreCase;

		_matches = SuggestionSource
			.Where(s => s.StartsWith(typed, comparison) && s.Length > typed.Length)
			.ToList();

		if (_matches.Count == 0)
		{
			_matchIndex = -1;
			_currentFullMatch = string.Empty;
			ClearGhost();
			return;
		}

		_matchIndex = 0;
		ShowMatch(_matchIndex);
	}

	private void ShowMatch(int index)
	{
		if (index < 0 || index >= _matches.Count) return;
		_currentFullMatch = _matches[index];
		UpdateGhost();
	}

	private void UpdateGhost()
	{
		if (_ghostTypedRun is null || _ghostRemainderRun is null || _textBox is null) return;

		_ghostBlock!.FontSize = _textBox.FontSize;
		_ghostBlock.FontFamily = _textBox.FontFamily;
		_ghostBlock.FontWeight = _textBox.FontWeight;
		_ghostBlock.FontStyle = _textBox.FontStyle;
		_ghostBlock.FontStretch = _textBox.FontStretch;

		if (string.IsNullOrEmpty(_currentFullMatch))
		{
			_ghostTypedRun.Text = string.Empty;
			_ghostRemainderRun.Text = string.Empty;
			return;
		}

		string typed = _textBox.Text;
		_ghostTypedRun.Text = typed;
		_ghostRemainderRun.Text = _currentFullMatch.Substring(typed.Length)
								  + (SuggestionSuffix ?? string.Empty);
		System.Diagnostics.Debug.WriteLine($"textbox font={_textBox.FontSize}, ghost font={_ghostBlock!.FontSize}, outer={FontSize}");
	}

	private void ClearGhost()
	{
		_currentFullMatch = string.Empty;
		if (_ghostTypedRun is not null) _ghostTypedRun.Text = string.Empty;
		if (_ghostRemainderRun is not null) _ghostRemainderRun.Text = string.Empty;
	}

	private void AcceptSuggestion()
	{
		if (_textBox is null || string.IsNullOrEmpty(_currentFullMatch)) return;

		string accepted = _currentFullMatch;
		_textBox.Text = accepted;
		_textBox.SelectionStart = accepted.Length;
		_textBox.SelectionLength = 0;

		_matches.Clear();
		_matchIndex = -1;
		_currentFullMatch = string.Empty;
		ClearGhost();

		SuggestionAccepted?.Invoke(this, accepted);
	}

	// ── Helpers ──────────────────────────────────────────────────────────

	private bool IsAcceptKey(SuggestionAcceptKey key)
		=> AcceptKey == key || AcceptKey == SuggestionAcceptKey.Both;

	private static T? FindDescendantByName<T>(DependencyObject root, string name)
	where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T dep && (dep as FrameworkElement)?.Name == name)
				return dep;
			var result = FindDescendantByName<T>(child, name);
			if (result is not null) return result;
		}
		return null;
	}
}
