using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MethodTrackerTool.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MethodTrackerTool.Helpers;

internal static class SerializerHelpers
{
    public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
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
        internal static Dictionary<string, HashSet<string>> OptInMembers = [];

        private static readonly Type[] ExcludeList = new[]
        {
            typeof(MemberInfo),
            typeof(ParameterInfo),
            typeof(ConstructorInfo),
            typeof(PropertyInfo)
        };

        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var members = base.GetSerializableMembers(objectType);
            var lookupType = objectType.IsGenericType
                ? objectType.GetGenericTypeDefinition()
                : objectType;

            if (OptInMembers.TryGetValue(lookupType.FullName, out var names))
            {
                foreach (var name in names)
                {
                    var mi = objectType.GetProperty(name, CommonHelpers.CommonBindingFlags) as MemberInfo
                             ?? objectType.GetField(name, CommonHelpers.CommonBindingFlags);
                    if (mi != null && !members.Contains(mi))
                    {
                        members.Add(mi);
                    }
                }
            }

            return members
                .Where(m => !(m is FieldInfo f && f.Name.Contains("BackingField")))
                .ToList();
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
                    .Where(n => type.GetMember(n, CommonHelpers.CommonBindingFlags).FirstOrDefault() != null)
                    .Select(n => base.CreateProperty(type.GetMember(n, CommonHelpers.CommonBindingFlags).First(), memberSerialization))
                    .Where(p => p != null)
                    .Select(p => { p.Readable = p.Writable = true; return p; })
                    .ToList();
            }

            var props = base.CreateProperties(type, memberSerialization);
            foreach (var prop in props)
            {
                prop.Readable = prop.Writable = true;

                if (ExcludeList.Any(t => t.IsAssignableFrom(prop.PropertyType))
                    || (prop.PropertyType?.Namespace?.StartsWith("System.Reflection") ?? false))
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
            }
            return props;
        }

        private class ConstantValueProvider(object constant) : IValueProvider
        {
            public object GetValue(object target) => constant;
            public void SetValue(object target, object? value) { }
        }

        private class DelegateStringValueProvider(IValueProvider? inner) : IValueProvider
        {
            public object? GetValue(object target)
            {
                var del = inner?.GetValue(target) as Delegate;
                return del == null
                    ? null
                    : $"{del.GetType().FullName} delegate is not serializable.";
            }
            public void SetValue(object target, object? value)
                => inner?.SetValue(target, value);
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
            public void SetValue(object target, object? value)
                => inner?.SetValue(target, value);
        }
    }
}