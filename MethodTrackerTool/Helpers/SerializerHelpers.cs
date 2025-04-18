using System;
using System.Globalization;
using MethodTrackerTool.Models;
using Newtonsoft.Json;

namespace MethodTrackerTool.Helpers;

internal static class SerializerHelpers
{
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters =
        {
            new LogEntryConverter(),
            new CultureInfoConverter(),
            new TypeConverter(),
            new DelegateConverter(),
            new ExceptionConverter()
        },
        Error = (sender, args) =>
        {
            args.ErrorContext.Handled = true;
        }
    };

    private class LogEntryConverter : JsonConverter<LogEntry>
    {
        public override LogEntry ReadJson(JsonReader reader, Type objectType, LogEntry existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void WriteJson(JsonWriter writer, LogEntry value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(nameof(LogEntry.MethodName));
            writer.WriteValue(value.MethodName);

            writer.WritePropertyName(nameof(LogEntry.Parameters));
            serializer.Serialize(writer, value.Parameters);

            writer.WritePropertyName(nameof(LogEntry.ReturnValue));
            serializer.Serialize(writer, value.ReturnValue);

            writer.WritePropertyName(nameof(LogEntry.ReturnType));
            writer.WriteValue(value.ReturnType);

            writer.WritePropertyName(nameof(LogEntry.Exceptions));
            serializer.Serialize(writer, value.Exceptions);

            writer.WritePropertyName(nameof(LogEntry.StartTime));
            writer.WriteValue(value.StartTime);

            writer.WritePropertyName(nameof(LogEntry.EndTime));
            writer.WriteValue(value.EndTime);

            writer.WritePropertyName(nameof(LogEntry.ElapsedTime));
            writer.WriteValue(value.ElapsedTime);

            writer.WritePropertyName(nameof(LogEntry.ExclusiveElapsedTime));
            writer.WriteValue(value.ExclusiveElapsedTime);

            writer.WritePropertyName(nameof(LogEntry.MemoryBefore));
            serializer.Serialize(writer, value.MemoryBefore);

            writer.WritePropertyName(nameof(LogEntry.MemoryAfter));
            serializer.Serialize(writer, value.MemoryAfter);

            writer.WritePropertyName(nameof(LogEntry.MemoryIncrease));
            serializer.Serialize(writer, value.MemoryIncrease);

            writer.WritePropertyName(nameof(LogEntry.Children));
            serializer.Serialize(writer, value.Children);

            writer.WriteEndObject();
        }
    }

    private class CultureInfoConverter : JsonConverter<CultureInfo>
    {
        public override CultureInfo ReadJson(JsonReader reader, Type objectType, CultureInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void WriteJson(JsonWriter writer, CultureInfo value, JsonSerializer serializer)
        {
            writer.WriteValue("System.CultureInfo is removed.");
        }
    }

    private class TypeConverter : JsonConverter<Type>
    {
        public override Type ReadJson(JsonReader reader, Type objectType, Type existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void WriteJson(JsonWriter writer, Type value, JsonSerializer serializer)
        {
            writer.WriteValue("System.Type object is not serializable.");
        }
    }

    private class DelegateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(Delegate).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException("Deserializing delegates is not supported.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.GetType().FullName} delegate is not serializable.");
        }
    }

    private class ExceptionConverter : JsonConverter<Exception>
    {
        public override Exception ReadJson(JsonReader reader, Type objectType, Exception existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void WriteJson(JsonWriter writer, Exception value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Message");
            writer.WriteValue(value.Message);

            writer.WritePropertyName("StackTrace");
            if (value.StackTrace is not null)
            {
                writer.WriteStartArray();
                foreach (var line in value.StackTrace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    writer.WriteValue(line);
                }
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNull();
            }

            writer.WritePropertyName("InnerException");
            serializer.Serialize(writer, value.InnerException);

            writer.WriteEndObject();
        }
    }

}