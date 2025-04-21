using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

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

            switch (caseType)
            {
                case TextCaseType.UpperCase:
                    return inputText.ToUpper();

                case TextCaseType.LowerCase:
                    return inputText.ToLower();

                case TextCaseType.CapitalizeCase:
                    // Use TextInfo of the current culture for correct word capitalization
                    // Important to convert to lower case first to correctly handle all-caps words
                    TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
                    return textInfo.ToTitleCase(inputText.ToLower());

                case TextCaseType.SentenceCase:
                    // Convert everything to lower case first
                    string lowerText = inputText.ToLower();

                    Regex sentenceStartRegex = SentenceCaseRegex();
                    string sentenceCaseText = sentenceStartRegex.Replace(lowerText, match =>
                        match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value.ToUpper()
                    );
                    return sentenceCaseText;

                case TextCaseType.InvertCase:
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

                case TextCaseType.TrimWhitespace:
                    return inputText.Trim();
                default:
                    return inputText;
            }
        }

        [GeneratedRegex(@"(^|\.|\?|!)(\s*)(\p{Ll})")]
        private static partial Regex SentenceCaseRegex();
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
        CapitalizeCase,
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
        TrimWhitespace
    }
}
