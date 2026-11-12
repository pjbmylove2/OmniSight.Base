using System.Globalization;
using System.Text;

namespace OmniSight.Base.Logging
{
    internal static class LogRecordFormatter
    {
        public static string Format(LogEntry entry)
        {
            var line = new StringBuilder()
                .Append('[').Append(entry.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)).Append("] ");
            if (entry.EventCode.Length > 0)
            {
                line.Append('[');
                AppendSingleLine(line, entry.EventCode);
                line.Append("] ");
            }

            AppendSingleLine(line, entry.Message);
            if (entry.ExceptionDetails.Length > 0)
            {
                line.Append(" / ");
                AppendSingleLine(line, entry.ExceptionDetails);
            }

            return line.ToString();
        }

        private static void AppendSingleLine(StringBuilder line, string value)
        {
            bool first = true;
            foreach (string part in value.Replace("\r", "").Split('\n'))
            {
                string text = part.Trim();
                if (text.Length == 0 || text.StartsWith("at ") || text.StartsWith("위치:") || text.StartsWith("---"))
                {
                    continue;
                }

                if (!first)
                {
                    line.Append(" / ");
                }
                line.Append(text.Replace("\0", ""));
                first = false;
            }
        }
    }
}
