using System;
using System.Collections.Generic;
using UnityEngine;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Utility class for parsing JSON responses from Supabase API.
    /// Provides simple JSON parsing methods without external dependencies.
    /// </summary>
    public static class SupabaseResponseParser
    {
        /// <summary>
        /// Extracts a property value from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <param name="propertyName">The name of the property to extract</param>
        /// <returns>The property value as a string, or null if not found</returns>
        public static string ExtractJsonProperty(string json, string propertyName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(propertyName))
                return null;

            try
            {
                // Look for the property in the format "propertyName":"value"
                string searchPattern = $"\"{propertyName}\":";
                int startIndex = json.IndexOf(searchPattern);
                
                if (startIndex == -1)
                    return null;

                startIndex += searchPattern.Length;
                
                // Skip whitespace
                while (startIndex < json.Length && char.IsWhiteSpace(json[startIndex]))
                    startIndex++;

                if (startIndex >= json.Length)
                    return null;

                // Check if the value is a string (starts with quote)
                if (json[startIndex] == '"')
                {
                    startIndex++; // Skip opening quote
                    int endIndex = FindClosingQuote(json, startIndex);
                    
                    if (endIndex == -1)
                        return null;

                    return UnescapeJsonString(json.Substring(startIndex, endIndex - startIndex));
                }
                // Check if the value is a boolean or number
                else
                {
                    int endIndex = FindValueEnd(json, startIndex);
                    
                    if (endIndex == -1)
                        return null;

                    return json.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting JSON property '{propertyName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts an array of objects from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <returns>A list of JSON object strings</returns>
        public static List<string> ExtractJsonArray(string json)
        {
            var result = new List<string>();
            
            if (string.IsNullOrEmpty(json))
                return result;

            try
            {
                // Find the start of the array
                int arrayStart = json.IndexOf('[');
                if (arrayStart == -1)
                    return result;

                int arrayEnd = FindMatchingBracket(json, arrayStart, '[', ']');
                if (arrayEnd == -1)
                    return result;

                // Extract the array content
                string arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1).Trim();
                
                if (string.IsNullOrEmpty(arrayContent))
                    return result;

                // Parse individual objects
                int currentIndex = 0;
                while (currentIndex < arrayContent.Length)
                {
                    // Skip whitespace and commas
                    while (currentIndex < arrayContent.Length && 
                           (char.IsWhiteSpace(arrayContent[currentIndex]) || arrayContent[currentIndex] == ','))
                        currentIndex++;

                    if (currentIndex >= arrayContent.Length)
                        break;

                    // Find the start of an object
                    if (arrayContent[currentIndex] == '{')
                    {
                        int objectEnd = FindMatchingBracket(arrayContent, currentIndex, '{', '}');
                        if (objectEnd != -1)
                        {
                            string objectJson = arrayContent.Substring(currentIndex, objectEnd - currentIndex + 1);
                            result.Add(objectJson);
                            currentIndex = objectEnd + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        currentIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting JSON array: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Finds the closing quote for a JSON string value, handling escaped quotes.
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <param name="startIndex">The index to start searching from</param>
        /// <returns>The index of the closing quote, or -1 if not found</returns>
        private static int FindClosingQuote(string json, int startIndex)
        {
            for (int i = startIndex; i < json.Length; i++)
            {
                if (json[i] == '"')
                {
                    // Check if this quote is escaped
                    int backslashCount = 0;
                    for (int j = i - 1; j >= startIndex && json[j] == '\\'; j--)
                        backslashCount++;

                    // If even number of backslashes (including 0), the quote is not escaped
                    if (backslashCount % 2 == 0)
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds the end of a JSON value (for non-string values).
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <param name="startIndex">The index to start searching from</param>
        /// <returns>The index after the end of the value</returns>
        private static int FindValueEnd(string json, int startIndex)
        {
            for (int i = startIndex; i < json.Length; i++)
            {
                char c = json[i];
                if (c == ',' || c == '}' || c == ']' || char.IsWhiteSpace(c))
                    return i;
            }
            return json.Length;
        }

        /// <summary>
        /// Finds the matching closing bracket for an opening bracket.
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <param name="startIndex">The index of the opening bracket</param>
        /// <param name="openBracket">The opening bracket character</param>
        /// <param name="closeBracket">The closing bracket character</param>
        /// <returns>The index of the matching closing bracket, or -1 if not found</returns>
        private static int FindMatchingBracket(string json, int startIndex, char openBracket, char closeBracket)
        {
            int bracketCount = 1;
            bool inString = false;
            
            for (int i = startIndex + 1; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == openBracket)
                        bracketCount++;
                    else if (c == closeBracket)
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                            return i;
                    }
                }
            }
            
            return -1;
        }

        /// <summary>
        /// Unescapes a JSON string value.
        /// </summary>
        /// <param name="str">The escaped string</param>
        /// <returns>The unescaped string</returns>
        private static string UnescapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }
    }
}