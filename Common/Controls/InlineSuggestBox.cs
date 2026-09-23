using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Input;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Tunetastic.Common.Controls;

/// <summary>
/// A text box control that provides inline suggestions as the user types.
/// </summary>
[TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
public sealed class InlineSuggestBox : Control
{
	private const string PartTextBox = "PART_TextBox";

	private TextBox? _textBox;

	private List<string> _matches = new();
	private int _matchIndex = -1;
	private string _currentFullMatch = string.Empty;
	private bool _suggestionDismissed;

	// The exact string we last wrote into _textBox.Text internally (to show a
	// suggestion). TextBox_TextChanged compares against this to decide whether
	// the change came from us or from the user. Cleared whenever the user makes
	// a real edit or the suggestion is dismissed/accepted.
	private string _lastInternalBoxText = string.Empty;

	// ── Dependency Properties ────────────────────────────────────────────

	/// <summary>
	/// Identifies the <see cref="SuggestionSource"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty SuggestionSourceProperty =
		DependencyProperty.Register(nameof(SuggestionSource), typeof(IList<string>),
			typeof(InlineSuggestBox), new PropertyMetadata(null,
				(d, _) => ((InlineSuggestBox)d).RefreshSuggestion()));

	/// <summary>
	/// Gets or sets the source of suggestions to display.
	/// </summary>
	public IList<string>? SuggestionSource
	{
		get => (IList<string>?)GetValue(SuggestionSourceProperty);
		set => SetValue(SuggestionSourceProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SuggestionSuffix"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty SuggestionSuffixProperty =
		DependencyProperty.Register(nameof(SuggestionSuffix), typeof(string),
			typeof(InlineSuggestBox), new PropertyMetadata(string.Empty,
				(d, _) => ((InlineSuggestBox)d).UpdateSuggestionSelection()));

	/// <summary>
	/// Gets or sets the visual-only hint appended after the suggestion remainder in the selection.
	/// Never written to the Text DP and never included in accepted text.
	/// </summary>
	public string SuggestionSuffix
	{
		get => (string)GetValue(SuggestionSuffixProperty);
		set => SetValue(SuggestionSuffixProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="CaseSensitiveSuggestion"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty CaseSensitiveSuggestionProperty =
		DependencyProperty.Register(nameof(CaseSensitiveSuggestion), typeof(bool),
			typeof(InlineSuggestBox), new PropertyMetadata(false,
				(d, _) => ((InlineSuggestBox)d).RefreshSuggestion()));

	/// <summary>
	/// Gets or sets a value indicating whether suggestion matching is case sensitive.
	/// </summary>
	public bool CaseSensitiveSuggestion
	{
		get => (bool)GetValue(CaseSensitiveSuggestionProperty);
		set => SetValue(CaseSensitiveSuggestionProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="Text"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty TextProperty =
	DependencyProperty.Register(nameof(Text), typeof(string),
		typeof(InlineSuggestBox), new PropertyMetadata(string.Empty, (d, e) =>
		{
			var box = (InlineSuggestBox)d;
			if (box._textBox is not null && box._textBox.Text != (string)e.NewValue)
				box._textBox.Text = (string)e.NewValue;
		}));

	/// <summary>
	/// Gets or sets the text content of the control.
	/// </summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="PlaceholderText"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty PlaceholderTextProperty =
		DependencyProperty.Register(nameof(PlaceholderText), typeof(string),
			typeof(InlineSuggestBox), new PropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets the text to display when the control is empty.
	/// </summary>
	public string PlaceholderText
	{
		get => (string)GetValue(PlaceholderTextProperty);
		set => SetValue(PlaceholderTextProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="TextWrapping"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty TextWrappingProperty =
		DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping),
			typeof(InlineSuggestBox), new PropertyMetadata(TextWrapping.NoWrap));

	/// <summary>
	/// Gets or sets how the text should wrap when it reaches the edge of the control.
	/// </summary>
	public TextWrapping TextWrapping
	{
		get => (TextWrapping)GetValue(TextWrappingProperty);
		set => SetValue(TextWrappingProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="AcceptsReturn"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty AcceptsReturnProperty =
		DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool),
			typeof(InlineSuggestBox), new PropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value indicating whether the control accepts return characters.
	/// </summary>
	public bool AcceptsReturn
	{
		get => (bool)GetValue(AcceptsReturnProperty);
		set => SetValue(AcceptsReturnProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="IsReadOnly"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty IsReadOnlyProperty =
		DependencyProperty.Register(nameof(IsReadOnly), typeof(bool),
			typeof(InlineSuggestBox), new PropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value indicating whether the control is read-only.
	/// </summary>
	public bool IsReadOnly
	{
		get => (bool)GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="MaxLength"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty MaxLengthProperty =
		DependencyProperty.Register(nameof(MaxLength), typeof(int),
			typeof(InlineSuggestBox), new PropertyMetadata(0));

	/// <summary>
	/// Gets or sets the maximum number of characters that can be entered into the control.
	/// </summary>
	public int MaxLength
	{
		get => (int)GetValue(MaxLengthProperty);
		set => SetValue(MaxLengthProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="MultiSuggestEnabled"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty MultiSuggestEnabledProperty =
		DependencyProperty.Register(nameof(MultiSuggestEnabled), typeof(bool),
			typeof(InlineSuggestBox), new PropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value indicating whether multi-suggestion mode is enabled.
	/// </summary>
	public bool MultiSuggestEnabled
	{
		get => (bool)GetValue(MultiSuggestEnabledProperty);
		set => SetValue(MultiSuggestEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SplitRules"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty SplitRulesProperty =
		DependencyProperty.Register(nameof(SplitRules), typeof(IList<ArtistSplitRule>),
			typeof(InlineSuggestBox), new PropertyMetadata(null));

	/// <summary>
	/// Gets or sets the rules used to split text into tokens for suggestions.
	/// </summary>
	public IList<ArtistSplitRule>? SplitRules
	{
		get => (IList<ArtistSplitRule>?)GetValue(SplitRulesProperty);
		set => SetValue(SplitRulesProperty, value);
	}

	// ── Events ───────────────────────────────────────────────────────────

	/// <summary>
	/// Occurs when the text content of the control changes.
	/// </summary>
	public event TextChangedEventHandler? TextChanged;

	/// <summary>
	/// Occurs when a suggestion is accepted by the user.
	/// </summary>
	public event EventHandler<string>? SuggestionAccepted;

	// ── Constructor ──────────────────────────────────────────────────────

	/// <summary>
	/// Initializes a new instance of the <see cref="InlineSuggestBox"/> class.
	/// </summary>
	public InlineSuggestBox()
	{
		DefaultStyleKey = typeof(InlineSuggestBox);
	}

	// ── Template ─────────────────────────────────────────────────────────

	/// <summary>
	/// Called when the control template is applied to initialize the control.
	/// </summary>
	/// <param name="e">Event data that describes how this event was generated.</param>
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (_textBox is not null)
		{
			_textBox.TextChanged -= TextBox_TextChanged;
			_textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
			_textBox.PointerPressed -= TextBox_PointerPressed;
			_textBox.SelectionChanged -= TextBox_SelectionChanged;
			_textBox.LostFocus -= TextBox_LostFocus;
		}

		_textBox = GetTemplateChild(PartTextBox) as TextBox;
		_lastInternalBoxText = string.Empty;
		_suggestionDismissed = false;
		ClearState();

		if (_textBox is not null)
		{
			_textBox.TextChanged += TextBox_TextChanged;
			_textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			_textBox.PointerPressed += TextBox_PointerPressed;
			_textBox.SelectionChanged += TextBox_SelectionChanged;
			_textBox.LostFocus += TextBox_LostFocus;

			if (!string.IsNullOrEmpty(Text))
				_textBox.Text = Text;
		}
	}

	// ── Event handlers ───────────────────────────────────────────────────

	/// <summary>
	/// Handles text changed events in the text box to update the control state.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The text changed event arguments.</param>
	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		string boxText = _textBox!.Text;

		// If the box text exactly matches what we last wrote internally, this
		// TextChanged is our own async notification — ignore it entirely.
		// This works even when TextChanged fires on the next dispatcher frame
		// after our write, because _lastInternalBoxText is a persistent field
		// (not a try/finally flag that resets before the callback fires).
		if (boxText == _lastInternalBoxText) return;

		// Real user edit. Clear the internal-text record and update the Text DP.
		_lastInternalBoxText = string.Empty;
		_suggestionDismissed = false;
		ClearState();

		Text = boxText;
		RefreshSuggestion();
		TextChanged?.Invoke(this, e);
	}

	/// <summary>
	/// Handles preview key down events to manage suggestion navigation and acceptance.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The key routed event arguments.</param>
	private void TextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
	{
		bool hasSuggestion = !string.IsNullOrEmpty(_currentFullMatch);

		switch (e.Key)
		{
			// ── Accept: Right arrow ───────────────────────────────────────
			case Windows.System.VirtualKey.Right:
				if (hasSuggestion && _textBox!.SelectionLength > 0)
				{
					AcceptSuggestion();
					e.Handled = true;
				}
				else if (!hasSuggestion && _suggestionDismissed && _textBox!.SelectionStart == _textBox.Text.Length)
				{
					_suggestionDismissed = false;
					RefreshSuggestion();
					e.Handled = true;
				}
				break;

			// ── Cycle down through matches ────────────────────────────────
			case Windows.System.VirtualKey.Down when _matches.Count > 0:
				e.Handled = true;
				_matchIndex = (_matchIndex + 1) % _matches.Count;
				ShowMatch(_matchIndex);
				break;

			case Windows.System.VirtualKey.Up when _matches.Count > 0:
				e.Handled = true;
				_matchIndex = (_matchIndex - 1 + _matches.Count) % _matches.Count;
				ShowMatch(_matchIndex);
				break;

			// ── Dismiss ───────────────────────────────────────────────────

			case Windows.System.VirtualKey.Left when hasSuggestion:
				DismissSuggestion();
				break;

			case Windows.System.VirtualKey.Back when hasSuggestion:
				{
					e.Handled = true;
					string current = Text;
					if (current.Length == 0) { DismissSuggestion(); break; }
					string trimmed = current.Substring(0, current.Length - 1);
					_lastInternalBoxText = trimmed; // allow TextChanged to treat next write as real
					Text = trimmed;
					_textBox!.Text = trimmed;
					_textBox.SelectionStart = trimmed.Length;
					_textBox.SelectionLength = 0;
					// Now re-evaluate suggestions based on the shortened typed text
					RefreshSuggestion();
					break;
				}

			case Windows.System.VirtualKey.Escape when hasSuggestion:
				DismissSuggestion();
				e.Handled = true;
				break;
		}
	}

	/// <summary>
	/// Handles pointer pressed events to dismiss suggestions when clicking outside.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The pointer routed event arguments.</param>
	private void TextBox_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(_currentFullMatch)) return;
		DismissSuggestion();
		e.Handled = true;
	}

	/// <summary>
	/// Handles selection changed events to manage suggestion dismissal.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The routed event arguments.</param>
	private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(_currentFullMatch)) return;
		if (_textBox!.SelectionLength == 0 && _textBox.Text == _lastInternalBoxText)
			DismissSuggestion();
	}

	/// <summary>
	/// Handles lost focus events to dismiss suggestions.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The routed event arguments.</param>
	private void TextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		DismissSuggestion();
	}

	// ── Suggestion logic ─────────────────────────────────────────────────

	/// <summary>
	/// Refreshes the suggestion display based on current text input.
	/// </summary>
	private void RefreshSuggestion()
	{
		if (_textBox is null || _textBox.FocusState == FocusState.Unfocused) return;

		string fullText = Text;
		var (typed, _) = GetCurrentToken(fullText);

		if (string.IsNullOrEmpty(typed) || SuggestionSource is null)
		{
			ClearState();
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
			ClearState();
			return;
		}

		_matchIndex = 0;
		ShowMatch(_matchIndex);
	}

	/// <summary>
	/// Shows a match at the specified index in the suggestion list.
	/// </summary>
	/// <param name="index">The index of the match to show.</param>
	private void ShowMatch(int index)
	{
		if (index < 0 || index >= _matches.Count) return;
		_currentFullMatch = _matches[index];
		UpdateSuggestionSelection();
	}

	/// <summary>
	/// Updates the suggestion selection by writing the suggested text into the text box.
	/// </summary>
	private void UpdateSuggestionSelection()
	{
		if (_textBox is null || string.IsNullOrEmpty(_currentFullMatch)) return;

		string realTyped = Text;
		var (typed, tokenStart) = GetCurrentToken(realTyped);

		string prefix = realTyped.Substring(0, tokenStart);
		string suffix = SuggestionSuffix ?? string.Empty;

		string newBoxText = prefix + _currentFullMatch + suffix;
		int selectionStart = tokenStart + typed.Length;
		int selectionLength = newBoxText.Length - selectionStart;

		// Record before writing so TextBox_TextChanged (async) can match it.
		_lastInternalBoxText = newBoxText;

		_textBox.Text = newBoxText;
		_textBox.SelectionStart = selectionStart;
		_textBox.SelectionLength = selectionLength;
	}

	/// <summary>
	/// Accepts the current suggestion and updates the control state.
	/// </summary>
	private void AcceptSuggestion()
	{
		if (_textBox is null || string.IsNullOrEmpty(_currentFullMatch)) return;

		string acceptedToken = _currentFullMatch;
		string realTyped = Text;
		var (_, tokenStart) = GetCurrentToken(realTyped);
		string prefix = realTyped.Substring(0, tokenStart);
		string acceptedFullText = prefix + acceptedToken; // suffix intentionally excluded

		_lastInternalBoxText = acceptedFullText;

		_textBox.Text = acceptedFullText;
		_textBox.SelectionStart = acceptedFullText.Length;
		_textBox.SelectionLength = 0;

		Text = acceptedFullText;
		ClearState();
		SuggestionAccepted?.Invoke(this, acceptedToken);
	}

	/// <summary>
	/// Dismisses the current suggestion and restores the original text.
	/// </summary>
	private void DismissSuggestion()
	{
		if (_textBox is null) return;

		string realTyped = Text;

		_lastInternalBoxText = realTyped;

		_textBox.Text = realTyped;
		_textBox.SelectionStart = realTyped.Length;
		_textBox.SelectionLength = 0;

		_suggestionDismissed = true;
		ClearState();
	}

	/// <summary>
	/// Clears the current match state without affecting the text box content.
	/// </summary>
	private void ClearState()
	{
		_currentFullMatch = string.Empty;
		_matches.Clear();
		_matchIndex = -1;
	}

	// ── Helpers ──────────────────────────────────────────────────────────

	/// <summary>
	/// Gets the current token from the full text based on split rules.
	/// </summary>
	/// <param name="fullText">The full text to parse.</param>
	/// <returns>A tuple containing the token and its start index.</returns>
	private (string token, int startIndex) GetCurrentToken(string fullText)
	{
		if (!MultiSuggestEnabled || SplitRules is null || SplitRules.Count == 0)
			return (fullText, 0);

		int lastSplitEnd = -1;

		foreach (var rule in SplitRules.Where(r => r.Active && r.Type == "Splitter"))
		{
			string pattern = rule.IsRegex ? rule.Pattern : Regex.Escape(rule.Pattern);
			var matches = Regex.Matches(fullText, pattern, RegexOptions.IgnoreCase);
			foreach (System.Text.RegularExpressions.Match m in matches)
			{
				int end = m.Index + m.Length;
				if (end > lastSplitEnd)
					lastSplitEnd = end;
			}
		}

		if (lastSplitEnd < 0) return (fullText, 0);

		int tokenStart = lastSplitEnd;
		while (tokenStart < fullText.Length && fullText[tokenStart] == ' ')
			tokenStart++;

		return (fullText.Substring(tokenStart), tokenStart);
	}
}
