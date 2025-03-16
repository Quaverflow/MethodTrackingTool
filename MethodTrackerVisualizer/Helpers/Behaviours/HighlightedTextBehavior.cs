using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MethodTrackerVisualizer.Helpers.Behaviours
{
    public static class HighlightedTextBehavior
    {
        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached(
                "FormattedText",
                typeof(string),
                typeof(HighlightedTextBehavior),
                new PropertyMetadata(string.Empty, OnPropertiesChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.RegisterAttached(
                "SearchText",
                typeof(string),
                typeof(HighlightedTextBehavior),
                new PropertyMetadata(string.Empty, OnPropertiesChanged));

        public static string GetFormattedText(DependencyObject obj) =>
            (string)obj.GetValue(FormattedTextProperty);
        public static void SetFormattedText(DependencyObject obj, string value) =>
            obj.SetValue(FormattedTextProperty, value);

        public static string GetSearchText(DependencyObject obj) =>
            (string)obj.GetValue(SearchTextProperty);
        public static void SetSearchText(DependencyObject obj, string value) =>
            obj.SetValue(SearchTextProperty, value);

        // Whenever either property changes, update the TextBlock.Inlines.
        private static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock tb)
            {
                string formattedText = GetFormattedText(tb);
                string searchText = GetSearchText(tb);

                var span = ConvertToHighlightedSpan(formattedText, searchText);
                tb.Inlines.Clear();
                tb.Inlines.Add(span);
            }
        }

        /// <summary>
        /// Converts the text into a Span where every occurrence of searchText is highlighted.
        /// </summary>
        private static Span ConvertToHighlightedSpan(string text, string searchText)
        {
            Span span = new Span();
            int lastIndex = 0;

            if (string.IsNullOrEmpty(searchText))
            {
                span.Inlines.Add(new Run(text) { Foreground = Brushes.White });
                return span;
            }

            var regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
            foreach (Match match in regex.Matches(text))
            {
                if (match.Index > lastIndex)
                {
                    string normalText = text.Substring(lastIndex, match.Index - lastIndex);
                    span.Inlines.Add(new Run(normalText) { Foreground = Brushes.White });
                }
                string highlightedText = match.Value;
                span.Inlines.Add(new Run(highlightedText)
                {
                    Background = Brushes.DarkBlue,
                });
                lastIndex = match.Index + match.Length;
            }
            if (lastIndex < text.Length)
            {
                span.Inlines.Add(new Run(text.Substring(lastIndex)) { Foreground = Brushes.White });
            }
            return span;
        }
    }
}
