using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

namespace SupabaseBridge.Runtime
{
    /// <summary>
    /// Service for managing Supabase database operations at runtime.
    /// Provides methods for performing CRUD operations on tables.
    /// </summary>
    public class SupabaseDatabaseService
    {
        private readonly SupabaseClient client;
        
        // Database endpoints
        private const string TABLES_ENDPOINT = "/rest/v1/";
        
        /// <summary>
        /// Initializes a new instance of the SupabaseDatabaseService class.
        /// </summary>
        /// <param name="client">The Supabase client to use for API requests</param>
        public SupabaseDatabaseService(SupabaseClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }
        
        /// <summary>
        /// Queries a table with the specified options.
        /// </summary>
        /// <param name="tableName">The name of the table to query</param>
        /// <param name="options">Query options</param>
        /// <returns>The query results as a list of dictionaries</returns>
        public async Task<List<Dictionary<string, object>>> QueryTable(string tableName, QueryOptions options = null)
        {
            try
            {
                // Build query parameters
                var queryParams = BuildQueryParameters(options);
                
                // Send the request
                string response = await client.Get($"{TABLES_ENDPOINT}{tableName}", queryParams);
                
                // Parse the response
                return ParseQueryResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Query failed for table {tableName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Inserts a new record into a table.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="data">The data to insert</param>
        /// <returns>The inserted record</returns>
        public async Task<Dictionary<string, object>> Insert(string tableName, Dictionary<string, object> data)
        {
            try
            {
                // Convert data to JSON
                string jsonBody = JsonUtility.ToJson(new DictionaryWrapper(data));
                
                // Set the prefer header to return the inserted record
                var queryParams = new Dictionary<string, string>
                {
                    { "prefer", "return=representation" }
                };
                
                // Send the request
                string response = await client.Post($"{TABLES_ENDPOINT}{tableName}", jsonBody, queryParams);
                
                // Parse the response
                var results = ParseQueryResponse(response);
                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Insert failed for table {tableName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates records in a table.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="data">The data to update</param>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The updated records</returns>
        public async Task<List<Dictionary<string, object>>> Update(string tableName, Dictionary<string, object> data, string filter)
        {
            try
            {
                // Convert data to JSON
                string jsonBody = JsonUtility.ToJson(new DictionaryWrapper(data));
                
                // Set the prefer header to return the updated records
                var queryParams = new Dictionary<string, string>
                {
                    { "prefer", "return=representation" }
                };
                
                // Add the filter if provided
                if (!string.IsNullOrEmpty(filter))
                {
                    string[] filterParts = filter.Split('=');
                    if (filterParts.Length == 2)
                    {
                        queryParams.Add(filterParts[0], filterParts[1]);
                    }
                }
                
                // Send the request
                string response = await client.Patch($"{TABLES_ENDPOINT}{tableName}", jsonBody, queryParams);
                
                // Parse the response
                return ParseQueryResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Update failed for table {tableName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes records from a table.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="filter">The filter to apply</param>
        /// <returns>The deleted records</returns>
        public async Task<List<Dictionary<string, object>>> Delete(string tableName, string filter)
        {
            try
            {
                // Set the prefer header to return the deleted records
                var queryParams = new Dictionary<string, string>
                {
                    { "prefer", "return=representation" }
                };
                
                // Add the filter if provided
                if (!string.IsNullOrEmpty(filter))
                {
                    string[] filterParts = filter.Split('=');
                    if (filterParts.Length == 2)
                    {
                        queryParams.Add(filterParts[0], filterParts[1]);
                    }
                }
                
                // Send the request
                string response = await client.Delete($"{TABLES_ENDPOINT}{tableName}", queryParams);
                
                // Parse the response
                return ParseQueryResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Delete failed for table {tableName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Builds query parameters from query options.
        /// </summary>
        /// <param name="options">The query options</param>
        /// <returns>A dictionary of query parameters</returns>
        private Dictionary<string, string> BuildQueryParameters(QueryOptions options)
        {
            var queryParams = new Dictionary<string, string>();
            
            if (options == null)
            {
                return queryParams;
            }
            
            // Add select columns
            if (options.Select != null && options.Select.Count > 0)
            {
                queryParams["select"] = string.Join(",", options.Select);
            }
            
            // Add filters
            if (options.Filters != null)
            {
                foreach (var filter in options.Filters)
                {
                    queryParams[filter.Key] = filter.Value;
                }
            }
            
            // Add order
            if (!string.IsNullOrEmpty(options.OrderBy))
            {
                queryParams["order"] = options.OrderBy;
                
                if (options.Ascending.HasValue)
                {
                    queryParams["order"] += options.Ascending.Value ? ".asc" : ".desc";
                }
            }
            
            // Add pagination
            if (options.Limit.HasValue)
            {
                queryParams["limit"] = options.Limit.Value.ToString();
            }
            
            if (options.Offset.HasValue)
            {
                queryParams["offset"] = options.Offset.Value.ToString();
            }
            
            return queryParams;
        }
        
        /// <summary>
        /// Parses the response from a query.
        /// </summary>
        /// <param name="json">The JSON response</param>
        /// <returns>A list of dictionaries representing the query results</returns>
        private List<Dictionary<string, object>> ParseQueryResponse(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    return new List<Dictionary<string, object>>();
                }
                
                // For simplicity, we're using a basic parsing approach
                // In a real implementation, you would use a more robust JSON parser
                
                // Check if the response is an array
                if (json.StartsWith("[") && json.EndsWith("]"))
                {
                    // Remove the outer brackets
                    json = json.Substring(1, json.Length - 2);
                    
                    // Split the array into objects
                    List<string> objectStrings = SplitJsonArray(json);
                    
                    // Parse each object
                    var results = new List<Dictionary<string, object>>();
                    foreach (var objStr in objectStrings)
                    {
                        results.Add(ParseJsonObject(objStr));
                    }
                    
                    return results;
                }
                else if (json.StartsWith("{") && json.EndsWith("}"))
                {
                    // Single object response
                    var result = ParseJsonObject(json);
                    return new List<Dictionary<string, object>> { result };
                }
                
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse query response: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }
        
        /// <summary>
        /// Splits a JSON array string into individual object strings.
        /// </summary>
        /// <param name="json">The JSON array string</param>
        /// <returns>A list of JSON object strings</returns>
        private List<string> SplitJsonArray(string json)
        {
            var result = new List<string>();
            
            int bracketCount = 0;
            int startIndex = 0;
            
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '{')
                {
                    if (bracketCount == 0)
                    {
                        startIndex = i;
                    }
                    bracketCount++;
                }
                else if (c == '}')
                {
                    bracketCount--;
                    
                    if (bracketCount == 0)
                    {
                        // Extract the object string
                        string objStr = json.Substring(startIndex, i - startIndex + 1);
                        result.Add(objStr);
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Parses a JSON object string into a dictionary.
        /// </summary>
        /// <param name="json">The JSON object string</param>
        /// <returns>A dictionary representing the JSON object</returns>
        private Dictionary<string, object> ParseJsonObject(string json)
        {
            // This is a very simplified JSON parser
            // In a real implementation, you would use a more robust JSON parser
            
            var result = new Dictionary<string, object>();
            
            // Remove the outer braces
            json = json.Substring(1, json.Length - 2);
            
            // Split by commas, but respect nested objects and arrays
            List<string> keyValuePairs = SplitJsonKeyValuePairs(json);
            
            foreach (var pair in keyValuePairs)
            {
                // Split by the first colon
                int colonIndex = pair.IndexOf(':');
                
                if (colonIndex > 0)
                {
                    // Extract the key (remove quotes)
                    string key = pair.Substring(0, colonIndex).Trim();
                    if (key.StartsWith("\"") && key.EndsWith("\""))
                    {
                        key = key.Substring(1, key.Length - 2);
                    }
                    
                    // Extract the value
                    string valueStr = pair.Substring(colonIndex + 1).Trim();
                    
                    // Parse the value
                    object value = ParseJsonValue(valueStr);
                    
                    // Add to the dictionary
                    result[key] = value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Splits a JSON object string into key-value pair strings.
        /// </summary>
        /// <param name="json">The JSON object string</param>
        /// <returns>A list of key-value pair strings</returns>
        private List<string> SplitJsonKeyValuePairs(string json)
        {
            var result = new List<string>();
            
            int bracketCount = 0;
            int braceCount = 0;
            int startIndex = 0;
            
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '{')
                {
                    braceCount++;
                }
                else if (c == '}')
                {
                    braceCount--;
                }
                else if (c == '[')
                {
                    bracketCount++;
                }
                else if (c == ']')
                {
                    bracketCount--;
                }
                else if (c == ',' && bracketCount == 0 && braceCount == 0)
                {
                    // Extract the key-value pair
                    string pair = json.Substring(startIndex, i - startIndex);
                    result.Add(pair);
                    
                    startIndex = i + 1;
                }
            }
            
            // Add the last pair
            if (startIndex < json.Length)
            {
                string pair = json.Substring(startIndex);
                result.Add(pair);
            }
            
            return result;
        }
        
        /// <summary>
        /// Parses a JSON value string into an object.
        /// </summary>
        /// <param name="json">The JSON value string</param>
        /// <returns>The parsed value</returns>
        private object ParseJsonValue(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            
            // Remove leading/trailing whitespace
            json = json.Trim();
            
            // Check the type of the value
            if (json == "null")
            {
                return null;
            }
            else if (json == "true")
            {
                return true;
            }
            else if (json == "false")
            {
                return false;
            }
            else if (json.StartsWith("\"") && json.EndsWith("\""))
            {
                // String value
                return json.Substring(1, json.Length - 2);
            }
            else if (json.StartsWith("{") && json.EndsWith("}"))
            {
                // Object value
                return ParseJsonObject(json);
            }
            else if (json.StartsWith("[") && json.EndsWith("]"))
            {
                // Array value
                // For simplicity, we'll just return the raw JSON string
                return json;
            }
            else
            {
                // Number value
                if (int.TryParse(json, out int intValue))
                {
                    return intValue;
                }
                else if (float.TryParse(json, out float floatValue))
                {
                    return floatValue;
                }
                else
                {
                    // Default to string
                    return json;
                }
            }
        }
        
        /// <summary>
        /// Helper class for wrapping dictionary data in a serializable format.
        /// </summary>
        [Serializable]
        private class DictionaryWrapper
        {
            public string data;
            
            public DictionaryWrapper(Dictionary<string, object> dict)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("{");
                
                bool first = true;
                foreach (var kvp in dict)
                {
                    if (!first)
                        sb.Append(",");
                    
                    sb.Append($"\"{kvp.Key}\":");
                    
                    if (kvp.Value == null)
                    {
                        sb.Append("null");
                    }
                    else if (kvp.Value is string)
                    {
                        sb.Append($"\"{kvp.Value}\"");
                    }
                    else if (kvp.Value is Dictionary<string, object>)
                    {
                        sb.Append(JsonUtility.ToJson(new DictionaryWrapper((Dictionary<string, object>)kvp.Value)));
                    }
                    else
                    {
                        sb.Append(kvp.Value.ToString().ToLower()); // For booleans
                    }
                    
                    first = false;
                }
                
                sb.Append("}");
                data = sb.ToString();
            }
        }
    }
    
    /// <summary>
    /// Represents options for querying a table.
    /// </summary>
    public class QueryOptions
    {
        /// <summary>
        /// Gets or sets the columns to select.
        /// </summary>
        public List<string> Select { get; set; }
        
        /// <summary>
        /// Gets or sets the filters to apply.
        /// </summary>
        public Dictionary<string, string> Filters { get; set; }
        
        /// <summary>
        /// Gets or sets the column to order by.
        /// </summary>
        public string OrderBy { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to order in ascending order.
        /// </summary>
        public bool? Ascending { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of records to return.
        /// </summary>
        public int? Limit { get; set; }
        
        /// <summary>
        /// Gets or sets the number of records to skip.
        /// </summary>
        public int? Offset { get; set; }
    }
} 