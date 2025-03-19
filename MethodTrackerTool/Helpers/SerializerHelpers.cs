using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MethodTrackerTool.Models;

namespace MethodTrackerTool.Helpers;

internal class SerializerHelpers
{
    public static readonly JsonSerializerOptions SerializerOptions = SafeJsonFactory.CreateOptions();
    private class LogEntryConverter : JsonConverter<LogEntry>
    {
        public override LogEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
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
            writer.WritePropertyName(nameof(LogEntry.Exceptions));
            JsonSerializer.Serialize(writer, value.Exceptions, options);

            writer.WriteString(nameof(LogEntry.StartTime), value.StartTime);
            writer.WriteString(nameof(LogEntry.EndTime), value.EndTime);
            writer.WriteString(nameof(LogEntry.ElapsedTime), value.ElapsedTime);
            writer.WriteString(nameof(LogEntry.ExclusiveElapsedTime), value.ExclusiveElapsedTime);

            writer.WritePropertyName(nameof(LogEntry.MemoryBefore));
            JsonSerializer.Serialize(writer, value.MemoryBefore, options);
            writer.WritePropertyName(nameof(LogEntry.MemoryAfter));
            JsonSerializer.Serialize(writer, value.MemoryAfter, options);
            writer.WritePropertyName(nameof(LogEntry.MemoryIncrease));
            JsonSerializer.Serialize(writer, value.MemoryIncrease, options);

            writer.WritePropertyName(nameof(LogEntry.Children));
            JsonSerializer.Serialize(writer, value.Children, options);

            writer.WriteEndObject();
        }
    }
    public class CultureInfoConverter : JsonConverter<CultureInfo>
    {
        public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("System.CultureInfo is removed.");
        }
    }

    public class ExceptionConverter : JsonConverter<Exception>
    {
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            var stackTrace = value.StackTrace?.Split(["\r\n", "\n"], StringSplitOptions.None);
            var result = new { value.Message, StackTrace = stackTrace, value.InnerException };
            JsonSerializer.Serialize(writer, result, options);
        }
    }
    public static class SafeJsonFactory
    {
        private static readonly ConcurrentDictionary<Type, JsonConverter> _converters = new();

        public static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                MaxDepth = 200,
                Converters =
                {
                    new LogEntryConverter(),
                    new CultureInfoConverter(),
                    new ExceptionConverter(),
                    new SafeJsonFactoryConverter()
                }
            };

            return options;
        }

        private class SafeJsonFactoryConverter : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return !typeToConvert.IsPrimitive && typeToConvert != typeof(string);
            }

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                return _converters.GetOrAdd(typeToConvert,
                    t => (JsonConverter)Activator.CreateInstance(typeof(SafeJsonConverter<>).MakeGenericType(t)));
            }
        }
    }
    public class SafeJsonConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    var propValue = property.GetValue(value);
                    if (propValue != null)
                    {
                        writer.WritePropertyName(property.Name);
                        JsonSerializer.Serialize(writer, propValue, options);
                    }
                }
                catch (Exception ex)
                {
                    // Log error or handle it gracefully
                    Console.WriteLine($"Skipping property '{property.Name}' due to error: {ex.Message}");
                }
            }
            writer.WriteEndObject();
        }
    }
}