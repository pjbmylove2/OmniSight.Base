namespace OmniSight.Base.Logging
{
    public sealed class LogManagerHealth
    {
        public string SessionId { get; internal set; }
        public string FallbackRoot { get; internal set; }
        public string LastError { get; internal set; }
        public long Accepted { get; internal set; }
        public long Written { get; internal set; }
        public long Dropped { get; internal set; }
        public long Failed { get; internal set; }
        public long FallbackWritten { get; internal set; }
        public long QueuedBytes { get; internal set; }
        public int QueueDepth { get; internal set; }
        public bool IsStorageDegraded { get; internal set; }
    }

}
