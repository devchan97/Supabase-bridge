using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Utility class for JSON operations in Supabase Bridge.
    /// Provides simple JSON serialization and deserialization methods.
    /// </summary>
    public static class SupabaseJsonUtility
    {
        /// <summary>
        /// Converts a dictionary to a JSON string.
        /// </summary>
        /// <param name="data">The dictionary to convert</param>
        /// <returns>A JSON string representation of the dictionary</returns>
        public static string ToJson(Dictionary<string, object> data)
        {
            if (data == null || data.Count == 0)
                return "{}";

            var sb = new StringBuilder();
            sb.Append("{");

            bool first = true;
            foreach (var kvp in data)
            {
                if (!first)
                    sb.Append(",");

                sb.Append($"\"{EscapeJsonString(kvp.Key)}\":");
                sb.Append(ValueToJson(kvp.Value));

                first = false;
            }

            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// Converts a value to its JSON representation.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The JSON representation of the value</returns>
        private static string ValueToJson(object value)
        {
            if (value == null)
                return "null";

            if (value is string str)
                return $"\"{EscapeJsonString(str)}\"";

            if (value is bool boolean)
                return boolean.ToString().ToLower();

            if (value is int || value is long || value is float || value is double)
                return value.ToString();

            if (value is Dictionary<string, object> dict)
                return ToJson(dict);

            // For other types, convert to string and escape
            return $"\"{EscapeJsonString(value.ToString())}\"";
        }

        /// <summary>
        /// Escapes special characters in a JSON string.
        /// </summary>
        /// <param name="str">The string to escape</param>
        /// <returns>The escaped string</returns>
        private static string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}