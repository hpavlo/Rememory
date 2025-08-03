using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Rememory.Helper
{
    /// <summary>
    /// Provides a static method for text conversion.
    /// </summary>
    public static partial class TextConverter
    {
        /// <summary>
        /// Converts the input string to the specified text format.
        /// </summary>
        /// <param name="inputText">The input string to convert.</param>
        /// <param name="caseType">The type of text conversion.</param>
        /// <returns>The converted string, or string.Empty if the input is null or empty.</returns>
        public static string ConvertText(this string inputText, TextCaseType caseType)
        {
            // Handle null or empty input string to prevent errors
            if (string.IsNullOrEmpty(inputText))
            {
                return string.Empty;
            }

            return caseType switch
            {
                TextCaseType.UpperCase => inputText.ToUpper(),
                TextCaseType.LowerCase => inputText.ToLower(),
                TextCaseType.CapitalizedCase => inputText.ToCapitalizedCase(),
                TextCaseType.SentenceCase => inputText.ToSentenceCase(),
                TextCaseType.InvertCase => inputText.ToInvertCase(),
                TextCaseType.TrimWhitespace => inputText.Trim(),
                TextCaseType.CamelCase => inputText.ToCamelCase(),
                TextCaseType.PascalCase => inputText.ToPascalCase(),
                TextCaseType.SnakeCase => inputText.ToSnakeCase(),
                TextCaseType.KebabCase => inputText.ToKebabCase(),
                _ => inputText,
            };
        }

        [GeneratedRegex(@"(^|\.|\?|!)(\s*)(\p{Ll})")]
        private static partial Regex SentenceCaseRegex();

        [GeneratedRegex(@"(?<=[\p{Ll}])(?=[\p{Lu}])")]
        private static partial Regex SeparateCombinedWordsRegex();

        [GeneratedRegex(@"[\p{L}\p{N}]+")]
        private static partial Regex SplitWordsRegex();

        private static string ToCapitalizedCase(this string inputText)
        {
            // Use TextInfo of the current culture for correct word capitalization
            // Important to convert to lower case first to correctly handle all-caps words
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(inputText.ToLower());
        }

        private static string ToSentenceCase(this string inputText)
        {
            // Convert everything to lower case first
            string lowerText = inputText.ToLower();

            Regex sentenceStartRegex = SentenceCaseRegex();
            string sentenceCaseText = sentenceStartRegex.Replace(lowerText, match =>
                match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value.ToUpper()
            );
            return sentenceCaseText;
        }

        private static string ToInvertCase(this string inputText)
        {
            StringBuilder invertedCase = new(inputText.Length);
            foreach (char c in inputText)
            {
                if (char.IsUpper(c))
                {
                    invertedCase.Append(char.ToLower(c));
                }
                else if (char.IsLower(c))
                {
                    invertedCase.Append(char.ToUpper(c));
                }
                else
                {
                    invertedCase.Append(c);
                }
            }
            return invertedCase.ToString();
        }

        private static string ToCamelCase(this string inputText)
        {
            var words = SplitWords(inputText);
            if (words.Count == 0)
            {
                return string.Empty;
            }

            // The first word remains lower-case.
            var firstWord = words[0].ToLower();
            // Capitalize the first letter of all subsequent words.
            var restWords = words.Skip(1).Select(ToCapitalizedCase);
            return firstWord + string.Concat(restWords);
        }

        private static string ToPascalCase(this string inputText)
        {
            var words = SplitWords(inputText);
            return string.Concat(words.Select(ToCapitalizedCase));
        }

        private static string ToSnakeCase(this string inputText)
        {
            var words = SplitWords(inputText);
            return string.Join("_", words.Select(w => w.ToLower()));
        }

        private static string ToKebabCase(this string inputText)
        {
            var words = SplitWords(inputText);
            return string.Join("-", words.Select(w => w.ToLower()));
        }

        private static List<string> SplitWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return [];
            }

            // Insert a space before each uppercase letter that is preceded by a lowercase letter.
            // For example, "someInputText" becomes "some Input Text".
            string processedInput = SeparateCombinedWordsRegex().Replace(input, " ");

            // Use a regex that is Unicode-aware to extract sequences of letters and numbers.
            var matches = SplitWordsRegex().Matches(processedInput);
            return [.. matches.Cast<Match>().Select(match => match.Value)];
        }
    }

    /// <summary>
    /// Defines text transformation types.
    /// </summary>
    public enum TextCaseType
    {
        /// <summary>
        /// UPPER CASE
        /// </summary>
        UpperCase,
        /// <summary>
        /// lower case
        /// </summary>
        LowerCase,
        /// <summary>
        /// Capitalize Every Word (Title Case)
        /// </summary>
        CapitalizedCase,
        /// <summary>
        /// Sentence case
        /// </summary>
        SentenceCase,
        /// <summary>
        /// iNVERT cASE (Invert case)
        /// </summary>
        InvertCase,
        /// <summary>
        /// Trim leading and trailing whitespace (Trim whitespace)
        /// </summary>
        TrimWhitespace,
        /// <summary>
        /// camelCase
        /// </summary>
        CamelCase,
        /// <summary>
        /// PascalCase
        /// </summary>
        PascalCase,
        /// <summary>
        /// snake_case
        /// </summary>
        SnakeCase,
        /// <summary>
        /// kebab-case
        /// </summary>
        KebabCase
    }
}
