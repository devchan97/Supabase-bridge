using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Service for managing Supabase database operations.
    /// Provides methods for querying tables, schemas, and performing CRUD operations.
    /// </summary>
    public class SupabaseDatabaseService
    {
        private readonly SupabaseClient client;
        
        // Database endpoints
        private const string TABLES_ENDPOINT = "/rest/v1/";
        private const string SCHEMA_ENDPOINT = "/rest/v1/";
        
        // Table information cache
        private List<TableInfo> tables;
        private Dictionary<string, TableSchema> tableSchemas;
        
        /// <summary>
        /// Gets the list of tables in the database.
        /// </summary>
        public List<TableInfo> Tables => tables;
        
        /// <summary>
        /// Initializes a new instance of the SupabaseDatabaseService class.
        /// </summary>
        /// <param name="client">The Supabase client to use for API requests</param>
        public SupabaseDatabaseService(SupabaseClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            tables = new List<TableInfo>();
            tableSchemas = new Dictionary<string, TableSchema>();
        }
        
        /// <summary>
        /// Fetches the list of tables from the database.
        /// </summary>
        /// <returns>The list of tables</returns>
        public async Task<List<TableInfo>> FetchTables()
        {
            try
            {
                // Query to get tables from the information schema
                var queryParams = new Dictionary<string, string>
                {
                    { "select", "*" }
                };
                
                string response = await client.Get("rest/v1/information_schema/tables", queryParams);
                
                // Parse the response
                tables = ParseTablesResponse(response);
                
                return tables;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to fetch tables: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the schema for a specific table.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <returns>The table schema</returns>
        public async Task<TableSchema> GetTableSchema(string tableName)
        {
            try
            {
                // Check if we already have the schema cached
                if (tableSchemas.TryGetValue(tableName, out TableSchema cachedSchema))
                {
                    return cachedSchema;
                }
                
                // Query to get columns for the table
                var columnsQueryParams = new Dictionary<string, string>
                {
                    { "select", "*" },
                    { "table_name", $"eq.{tableName}" }
                };
                
                string columnsResponse = await client.Get("rest/v1/information_schema/columns", columnsQueryParams);
                
                // Query to get foreign keys for the table
                var fkQueryParams = new Dictionary<string, string>
                {
                    { "select", "*" },
                    { "table_name", $"eq.{tableName}" }
                };
                
                string fkResponse = await client.Get("rest/v1/information_schema/key_column_usage", fkQueryParams);
                
                // Parse the responses
                TableSchema schema = ParseTableSchema(tableName, columnsResponse, fkResponse);
                
                // Cache the schema
                tableSchemas[tableName] = schema;
                
                return schema;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get table schema for {tableName}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Queries a table with the specified options.
        /// </summary>
        /// <param name="tableName">The name of the table to query</param>
        /// <param name="options">Query options</param>
        /// <returns>The query results as a list of dynamic objects</returns>
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
                string jsonBody = SupabaseJsonUtility.ToJson(data);
                
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
                string jsonBody = SupabaseJsonUtility.ToJson(data);
                
                // Set the prefer header to return the updated records
                var queryParams = new Dictionary<string, string>
                {
                    { "prefer", "return=representation" }
                };
                
                // Add the filter if provided
                if (!string.IsNullOrEmpty(filter))
                {
                    queryParams.Add(filter.Split('=')[0], filter.Split('=')[1]);
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
                    queryParams.Add(filter.Split('=')[0], filter.Split('=')[1]);
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
        /// Parses the response from a tables query.
        /// </summary>
        /// <param name="json">The JSON response</param>
        /// <returns>A list of table information</returns>
        private List<TableInfo> ParseTablesResponse(string json)
        {
            try
            {
                // Parse the JSON array
                var tables = new List<TableInfo>();
                
                // Extract table information from the JSON
                // This is a simplified implementation - in a real implementation,
                // you would use a proper JSON parser
                
                // For now, we'll just create some dummy tables for testing
                tables.Add(new TableInfo { Name = "users", Schema = "public", RowCount = 10 });
                tables.Add(new TableInfo { Name = "posts", Schema = "public", RowCount = 25 });
                tables.Add(new TableInfo { Name = "comments", Schema = "public", RowCount = 50 });
                
                return tables;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse tables response: {ex.Message}");
                return new List<TableInfo>();
            }
        }
        
        /// <summary>
        /// Parses the response from a table schema query.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="columnsJson">The JSON response for columns</param>
        /// <param name="fkJson">The JSON response for foreign keys</param>
        /// <returns>The table schema</returns>
        private TableSchema ParseTableSchema(string tableName, string columnsJson, string fkJson)
        {
            try
            {
                // Create a new table schema
                var schema = new TableSchema
                {
                    TableName = tableName,
                    Columns = new List<ColumnInfo>(),
                    ForeignKeys = new List<ForeignKeyInfo>()
                };
                
                // Parse columns
                // This is a simplified implementation - in a real implementation,
                // you would use a proper JSON parser
                
                // For now, we'll just create some dummy columns for testing
                if (tableName == "users")
                {
                    schema.Columns.Add(new ColumnInfo { Name = "id", DataType = "uuid", IsPrimaryKey = true, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "email", DataType = "text", IsPrimaryKey = false, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "name", DataType = "text", IsPrimaryKey = false, IsNullable = true });
                    schema.Columns.Add(new ColumnInfo { Name = "created_at", DataType = "timestamp", IsPrimaryKey = false, IsNullable = false, DefaultValue = "now()" });
                }
                else if (tableName == "posts")
                {
                    schema.Columns.Add(new ColumnInfo { Name = "id", DataType = "uuid", IsPrimaryKey = true, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "title", DataType = "text", IsPrimaryKey = false, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "content", DataType = "text", IsPrimaryKey = false, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "user_id", DataType = "uuid", IsPrimaryKey = false, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "created_at", DataType = "timestamp", IsPrimaryKey = false, IsNullable = false, DefaultValue = "now()" });
                    
                    schema.ForeignKeys.Add(new ForeignKeyInfo { ColumnName = "user_id", ReferencedTable = "users", ReferencedColumn = "id" });
                }
                else if (tableName == "comments")
                {
                    schema.Columns.Add(new ColumnInfo { Name = "id", DataType = "uuid", IsPrimaryKey = true, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "content", DataType = "text", IsPrimaryKey = false, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "post_id", DataType = "uuid", IsPrimaryKey = false, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "user_id", DataType = "uuid", IsPrimaryKey = false, IsNullable = false });
                    schema.Columns.Add(new ColumnInfo { Name = "created_at", DataType = "timestamp", IsPrimaryKey = false, IsNullable = false, DefaultValue = "now()" });
                    
                    schema.ForeignKeys.Add(new ForeignKeyInfo { ColumnName = "post_id", ReferencedTable = "posts", ReferencedColumn = "id" });
                    schema.ForeignKeys.Add(new ForeignKeyInfo { ColumnName = "user_id", ReferencedTable = "users", ReferencedColumn = "id" });
                }
                
                return schema;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse table schema for {tableName}: {ex.Message}");
                return new TableSchema { TableName = tableName, Columns = new List<ColumnInfo>(), ForeignKeys = new List<ForeignKeyInfo>() };
            }
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
                // Parse the JSON array
                var results = new List<Dictionary<string, object>>();
                
                // This is a simplified implementation - in a real implementation,
                // you would use a proper JSON parser
                
                // For now, we'll just return an empty list
                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse query response: {ex.Message}");
                return new List<Dictionary<string, object>>();
            }
        }
    }
    
    /// <summary>
    /// Represents information about a database table.
    /// </summary>
    [Serializable]
    public class TableInfo
    {
        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the schema of the table.
        /// </summary>
        public string Schema { get; set; }
        
        /// <summary>
        /// Gets or sets the number of rows in the table.
        /// </summary>
        public int RowCount { get; set; }
    }
    
    /// <summary>
    /// Represents the schema of a database table.
    /// </summary>
    [Serializable]
    public class TableSchema
    {
        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        /// Gets or sets the columns in the table.
        /// </summary>
        public List<ColumnInfo> Columns { get; set; }
        
        /// <summary>
        /// Gets or sets the foreign keys in the table.
        /// </summary>
        public List<ForeignKeyInfo> ForeignKeys { get; set; }
    }
    
    /// <summary>
    /// Represents information about a database column.
    /// </summary>
    [Serializable]
    public class ColumnInfo
    {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the data type of the column.
        /// </summary>
        public string DataType { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the column can be null.
        /// </summary>
        public bool IsNullable { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the column is a primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }
        
        /// <summary>
        /// Gets or sets the default value of the column.
        /// </summary>
        public string DefaultValue { get; set; }
    }
    
    /// <summary>
    /// Represents information about a foreign key.
    /// </summary>
    [Serializable]
    public class ForeignKeyInfo
    {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string ColumnName { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the referenced table.
        /// </summary>
        public string ReferencedTable { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the referenced column.
        /// </summary>
        public string ReferencedColumn { get; set; }
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