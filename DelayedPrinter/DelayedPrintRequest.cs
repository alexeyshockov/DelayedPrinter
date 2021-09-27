using System;
using System.Text.Json.Serialization;

namespace DelayedPrinter
{
    public record DelayedPrintRequest(string Message, DateTimeOffset PrintAt)
    {
        [JsonIgnore]
        public long Timestamp => PrintAt.ToUnixTimeSeconds();
    }
}
