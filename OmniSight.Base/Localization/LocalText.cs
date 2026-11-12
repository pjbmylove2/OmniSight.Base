using System;
using System.Globalization;

namespace OmniSight.Base.Localization
{
    /// <summary>Operator text only. Never use for packet values, paths, OCR or CSV fields.</summary>
    public static class LocalText
    {
        public static string Get(string key, string korean)
        {
            return LocalizationManager.Get(key, korean);
        }

        public static string Format(string key, string korean, params object[] arguments)
        {
            string translated = Get(key, korean);
            try
            {
                return string.Format(CultureInfo.CurrentCulture, translated, arguments);
            }
            catch (FormatException)
            {
                // Invalid external translations must never interrupt inspection or alarms.
                return string.Format(CultureInfo.CurrentCulture, korean, arguments);
            }
        }

        public static string Interpolate(string key, FormattableString korean)
        {
            if (korean == null) { return string.Empty; }
            return Format(key, korean.Format, korean.GetArguments());
        }
    }
}
