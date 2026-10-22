using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Common;

// Week 7 Day 4: found by the bursary form. <input type="date"> sends
// "2026-11-30": a date with no time zone. System.Text.Json reads that as
// DateTimeKind.Unspecified, and Npgsql refuses to write an Unspecified
// DateTime to a timestamptz column, so every such request was a 500.
//
// Every DateTime this API READS is now UTC: a value with an offset is
// converted, and a value without one is taken to be UTC already. Writing
// is unchanged.
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
