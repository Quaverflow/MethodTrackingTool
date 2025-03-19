using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MethodTrackerTool.Models;

namespace MethodTrackerTool.Helpers;

internal class SerializerHelpers
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        MaxDepth = 200,
        Converters =
        {
            new LogEntryConverter(),
            new CultureInfoConverter(),
            new TypeConverter(),
            new ConverterFactory(),
            new ExceptionConverter()
        }
    };

    public static readonly JsonSerializerOptions PropertyOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        MaxDepth = 200,
        Converters =
        {
            new LogEntryConverter(),
            new CultureInfoConverter(),
            new TypeConverter(),
            new ExceptionConverter()
        }
    };

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

    public class TypeConverter : JsonConverter<Type>
    {
        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("System.Type object is not serializable.");
        }
    }

    public class ConverterFactory : JsonConverterFactory
    {
        public static List<Type?> TypesToIgnore = new();
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsSerializable && !TypesToIgnore.Contains(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType =  typeof(Converter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }

    public class Converter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Deserializing delegates is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var type = value?.GetType() ?? typeof(T);

            if (IsSimpleType(type))
            {
                JsonSerializer.Serialize(writer, value, PropertyOptions);
            }
            else if (value is IEnumerable enumerable)
            {
                writer.WriteStartArray();
                foreach (var item in enumerable)
                {
                    try
                    {
                        JsonSerializer.Serialize(writer, item, options);
                    }
                    catch
                    {
                        ConverterFactory.TypesToIgnore.Add(item?.GetType());
                        writer.WriteStringValue($"{item?.GetType()} cannot be serialized");
                    }
                }
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStartObject();
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(x => x.PropertyType.IsSerializable)
                             .Where(x => !x.GetCustomAttributes<JsonIgnoreAttribute>().Any()))
                {
                    writer.WritePropertyName(prop.Name);
                    try
                    {
                        var propValue = prop.GetValue(value);
                        JsonSerializer.Serialize(writer, propValue, options);
                    }
                    catch
                    {
                        ConverterFactory.TypesToIgnore.Add(prop.PropertyType);
                        writer.WriteStringValue($"{prop.PropertyType} cannot be serialized");
                    }
                }
                writer.WriteEndObject();
            }
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(Guid) ||
                   type == typeof(TimeSpan);
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
            var stackTrace = value.StackTrace?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var result = new { value.Message, StackTrace = stackTrace, value.InnerException };
            JsonSerializer.Serialize(writer, result, options);
        }
    }
}