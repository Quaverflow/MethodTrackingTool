using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MethodTrackerTool.Helpers;

public class SerializerHelpers
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        MaxDepth = 200,
        Converters = { new LogEntryConverter(), new CultureInfoConverter() , new TypeConverter(), new DelegateConverter() }
    };
    private class LogEntryConverter : JsonConverter<LogEntry>
    {
        public override LogEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialization is not supported.
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, LogEntry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(LogEntry.MethodName), value.MethodName);

            writer.WritePropertyName(nameof(LogEntry.Parameters));
            JsonSerializer.Serialize(writer, value.Parameters, options);

            writer.WritePropertyName(nameof(LogEntry.ReturnValue));
            JsonSerializer.Serialize(writer, value.ReturnValue, options);

            writer.WriteString(nameof(LogEntry.ReturnType), value.ReturnType);

            writer.WriteString(nameof(LogEntry.StartTime), value.StartTime);
            writer.WriteString(nameof(LogEntry.EndTime), value.EndTime);
            writer.WriteString(nameof(LogEntry.ElapsedTime), value.ElapsedTime);
            writer.WriteString(nameof(LogEntry.ExclusiveElapsedTime), value.ExclusiveElapsedTime);

            writer.WritePropertyName(nameof(LogEntry.Children));
            JsonSerializer.Serialize(writer, value.Children, options);

            writer.WriteEndObject();
        }
    }
    public class CultureInfoConverter : JsonConverter<CultureInfo>
    {
        public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialization is not supported.
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("System.CultureInfo is removed.");
        }
    }    
    
    public class TypeConverter : JsonConverter<Type>
    {
        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialization is not supported.
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("System.Type object is not serializable.");
        }
    }  
    
    public class DelegateConverter : JsonConverter<Delegate>
    {
        public override Delegate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialization is not supported.
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, Delegate value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("System.Delegate object is not serializable.");
        }
    }
}