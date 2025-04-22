using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
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
        ContractResolver = new CustomContractResolver(),
        Error = (_, args) => args.ErrorContext.Handled = true
    };

    public static void StreamSerialize(
        IEnumerable<LogEntry> entries,
        Stream outputStream)
    {
        using var sw = new StreamWriter(outputStream, Encoding.UTF8, 8192, leaveOpen: true);
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

    internal class CustomContractResolver : DefaultContractResolver
    {
        private static readonly Type[] ExcludeList =
        [
            typeof(MemberInfo),
            typeof(ParameterInfo),
            typeof(ConstructorInfo),
            typeof(PropertyInfo)
        ];

        public static Dictionary<string, string[]> UserDefinedPrivateMembers = [];

        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            const BindingFlags flags = CommonHelpers.CommonBindingFlags;
            var props = objectType
                .GetProperties(flags)
                .Where(x => x.GetMethod.IsPublic || IsFromIncludeList(objectType, x.Name))
                .Cast<MemberInfo>();

            return props.ToList();
        }

        private static bool IsFromIncludeList(Type type, string name)
        {
            var result = UserDefinedPrivateMembers.TryGetValue(type.FullName ?? "", out var includeList) &&
                   includeList.Contains(name);

            return result;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (type == typeof(LogEntry))
            {
                var names = new[]
                {
                    nameof(LogEntry.MethodName),
                    nameof(LogEntry.Parameters),
                    nameof(LogEntry.ReturnValue),
                    nameof(LogEntry.ReturnType),
                    nameof(LogEntry.Exception),
                    nameof(LogEntry.Children)
                };

                return names
                    .Where(n => GetProperty(type, n) != null)
                    .Select(n => base.CreateProperty(GetProperty(type, n)! /*checked above*/, memberSerialization))
                    .Where(p => p != null)
                    .Select(p =>
                    {
                        p.Readable = p.Writable = true;
                        return p;
                    })
                    .ToList();
            }

            var props = base.CreateProperties(type, memberSerialization);
            foreach (var prop in props)
            {
                prop.Readable = prop.Writable = true;

                if (ExcludeList.Any(t => t.IsAssignableFrom(prop.PropertyType)) || 
                    (prop.PropertyType?.Namespace?.StartsWith("System.Reflection") ?? false))
                {
                    prop.Ignored = true;
                    continue;
                }

                if (prop.PropertyType == typeof(CultureInfo))
                {
                    prop.ValueProvider = new ConstantValueProvider("System.CultureInfo is removed.");
                    continue;
                }

                if (prop.PropertyType == typeof(Type))
                {
                    prop.ValueProvider = new ConstantValueProvider("System.Type object is not serializable.");
                    continue;
                }

                if (typeof(Delegate).IsAssignableFrom(prop.PropertyType))
                {
                    prop.ValueProvider = new DelegateStringValueProvider(prop.ValueProvider);
                    continue;
                }

                if (typeof(IDictionary).IsAssignableFrom(prop.PropertyType))
                {
                    prop.ValueProvider = new ReflectionFilteringValueProvider(prop.ValueProvider);
                }


                if (IsFromIncludeList(type, prop.PropertyName))
                {
                    continue;
                }
            }

            return props;
        }

        private static MemberInfo? GetProperty(Type type, string name)
        {
            var value = type.GetProperty(name, CommonHelpers.CommonBindingFlags);
            if (value != null)
            {
                if (value.GetMethod.IsPublic)
                {
                    return value;
                }

                if (IsFromIncludeList(type, name))
                {
                    return value;
                }

                var field = type.GetField(name, CommonHelpers.CommonBindingFlags);
                if (field?.IsPublic is true || IsFromIncludeList(type, name))
                {
                    return field;
                }
            }

            return null;
        }

        private class ConstantValueProvider(object constant) : IValueProvider
        {
            public object GetValue(object target) => constant;
            public void SetValue(object target, object? value) { }
        }

        private class DelegateStringValueProvider(IValueProvider? inner) : IValueProvider
        {
            public object? GetValue(object target) =>
                inner?.GetValue(target) is not Delegate del
                    ? null
                    : $"{del.GetType().FullName} delegate is not serializable.";
            public void SetValue(object target, object? value) => inner?.SetValue(target, value);
        }

        private class ReflectionFilteringValueProvider(IValueProvider? inner) : IValueProvider
        {
            public object? GetValue(object target)
            {
                if (inner?.GetValue(target) is not IDictionary dict)
                {
                    return null;
                }

                var copy = (IDictionary)Activator.CreateInstance(dict.GetType())!;
                foreach (DictionaryEntry kv in dict)
                {
                    var v = kv.Value;
                    if (v != null)
                    {
                        var t = v.GetType();
                        if (t.Namespace?.StartsWith("System.Reflection") == true
                            || typeof(MemberInfo).IsAssignableFrom(t))
                        {
                            continue;
                        }
                    }
                    copy[kv.Key] = kv.Value;
                }
                return copy;
            }

            public void SetValue(object target, object? value) => inner?.SetValue(target, value);
        }
    }
}