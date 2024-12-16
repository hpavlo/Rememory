using Jeffijoe.MessageFormat;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rememory.Helper
{
    public static class ResourceExtensions
    {
        private static readonly ResourceLoader _resourceLoader = new();
        private static readonly CultureInfo _locale = CultureInfo.CurrentCulture;
        private static readonly MessageFormatter _formatter = new(useCache: false, locale: _locale.TwoLetterISOLanguageName);

        public static string GetLocalizedResource(this string resourceKey) => _resourceLoader.GetString(resourceKey);

        public static string GetLocalizedFormatResource(this string resourceKey, IReadOnlyDictionary<string, object?> pairs)
        {
            var value = resourceKey.GetLocalizedResource();

            if (value is null)
            {
                return string.Empty;
            }

            try
            {
                value = _formatter.FormatMessage(value, pairs);
            }
            catch
            {
                value = string.Empty;
            }

            return value;
        }

        public static string GetLocalizedFormatResource(this string resourceKey, params object[] values)
        {
            var pairs = values.Select((value, index) => new KeyValuePair<string, object?>(index.ToString(), value))
                              .ToDictionary(pair => pair.Key, pair => pair.Value);
            return GetLocalizedFormatResource(resourceKey, pairs);
        }
    }
}
