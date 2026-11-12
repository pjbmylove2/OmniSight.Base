using System;

namespace OmniSight.Base.Logging
{
    public enum LogCategory
    {
        SYSTEM = 0,
        COMMUNICATION,
        RESULT,
        DEBUG,
        ALARM,
        DIO,
        ETHERNET,
        MAX
    }

    internal readonly struct LogEntry
    {
        public LogCategory Category { get; }
        public DateTime Timestamp { get; }
        public string Message { get; }
        public string LotId { get; }
        public int CamId { get; }
        public string EventCode { get; }
        public string ExceptionDetails { get; }

        public LogEntry(LogCategory category, string message, string lotId, int camId,
            string eventCode = null, Exception exception = null)
        {
            Category = category;
            Timestamp = DateTime.Now;
            Message = Limit(message, 32768);
            EventCode = Limit(eventCode, 128);
            ExceptionDetails = exception == null ? string.Empty : Limit(exception.ToString(), 32768);
            LotId = Limit(lotId, 128);
            CamId = camId;
        }
        private static string Limit(string value, int maximum)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Length <= maximum ? value : value.Substring(0, maximum) + " [TRUNCATED]";
        }
    }

}
