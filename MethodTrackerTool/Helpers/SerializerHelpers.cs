using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using MethodTrackerTool.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        },
        ContractResolver = new ReflectionFilteringContractResolver(),
        Error = (_, args) => args.ErrorContext.Handled = true
    };

    public static void StreamSerialize(
        IEnumerable<LogEntry> entries,
        Stream outputStream)
    {
        using var sw = new StreamWriter(
            outputStream,
            Encoding.UTF8,
            bufferSize: 8192,
            leaveOpen: true);
        using var jw = new JsonTextWriter(sw);
        jw.Formatting = SerializerSettings.Formatting;

        var serializer = JsonSerializer.Create(SerializerSettings);

        jw.WriteStartArray();

        foreach (var entry in entries)
        {
            serializer.Serialize(jw, entry);
        }

        jw.WriteEndArray();
        jw.Flush();
    }

    private class LogEntryConverter : JsonConverter<LogEntry>
    {
        public override LogEntry ReadJson(JsonReader reader, Type objectType, LogEntry? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException("Deserialization is not supported.");

        public override void WriteJson(JsonWriter writer, LogEntry? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(LogEntry.MethodName));
            writer.WriteValue(value.MethodName);

            writer.WritePropertyName(nameof(LogEntry.Parameters));
            serializer.Serialize(writer, value.Parameters);

            writer.WritePropertyName(nameof(LogEntry.ReturnValue));
            serializer.Serialize(writer, value.ReturnValue);

            writer.WritePropertyName(nameof(LogEntry.ReturnType));
            writer.WriteValue(value.ReturnType);

            writer.WritePropertyName(nameof(LogEntry.Exception));
            serializer.Serialize(writer, value.Exception);

            writer.WritePropertyName(nameof(LogEntry.Children));
            serializer.Serialize(writer, value.Children);

            writer.WriteEndObject();
        }
    }

    private class CultureInfoConverter : JsonConverter<CultureInfo>
    {
        public override CultureInfo ReadJson(JsonReader reader, Type objectType, CultureInfo? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException("Deserialization is not supported.");

        public override void WriteJson(JsonWriter writer, CultureInfo? value, JsonSerializer serializer) => writer.WriteValue("System.CultureInfo is removed.");
    }

    private class TypeConverter : JsonConverter<Type>
    {
        public override Type ReadJson(JsonReader reader, Type objectType, Type? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException("Deserialization is not supported.");

        public override void WriteJson(JsonWriter writer, Type? value, JsonSerializer serializer) => writer.WriteValue("System.Type object is not serializable.");
    }

    private class DelegateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(Delegate).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotSupportedException("Deserializing delegates is not supported.");

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => writer.WriteValue($"{value?.GetType().FullName} delegate is not serializable.");
    }


    public class ReflectionFilteringContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(
            MemberInfo member,
            MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);

            var t = prop.PropertyType;
            if (t == null)
            {
                prop.Ignored = true;
                return prop;
            }

            if (t.Namespace?.StartsWith("System.Reflection", StringComparison.Ordinal) == true
                || typeof(MemberInfo).IsAssignableFrom(t))
            {
                prop.Ignored = true;
            }

            if (typeof(System.Collections.IDictionary).IsAssignableFrom(t)
                || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
            {
                prop.ValueProvider = new ReflectionFilteringValueProvider(prop.ValueProvider);
            }

            return prop;
        }

        class ReflectionFilteringValueProvider(IValueProvider? inner) : IValueProvider
        {
            public object? GetValue(object target)
            {
                var value = inner?.GetValue(target);
                if (value is not System.Collections.IDictionary dict)
                {
                    return value;
                }

                var newDict = (System.Collections.IDictionary)Activator.CreateInstance(dict.GetType())!;
                foreach (System.Collections.DictionaryEntry kv in dict)
                {
                    var val = kv.Value;
                    var t = val?.GetType();
                    if (t != null && t.Namespace?.StartsWith("System.Reflection", StringComparison.Ordinal) == true)
                    {
                        continue;
                    }

                    if (val is MemberInfo)
                    {
                        continue;
                    }

                    newDict[kv.Key] = val;
                }
                return newDict;
            }

            public void SetValue(object target, object? value) => inner?.SetValue(target, value);
        }
    }
}
