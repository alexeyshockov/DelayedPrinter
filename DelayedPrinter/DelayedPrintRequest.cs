using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DelayedPrinter
{
    public record DelayedPrintRequest([MinLength(1)] string Message, DateTimeOffset PrintAt)
    {
        // It's important to have a unique ID to prevent loosing request with the same text and same print date
        public string Id { get; init; } = Guid.NewGuid().ToString();

        [JsonIgnore]
        public long Timestamp => PrintAt.ToUnixTimeSeconds();
    }
}
