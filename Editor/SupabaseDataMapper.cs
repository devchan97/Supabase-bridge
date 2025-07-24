using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Utility class for mapping between Supabase data types and C# types.
    /// </summary>
    public static class SupabaseDataMapper
    {
        /// <summary>
        /// Maps a Supabase data type to a C# type.
        /// </summary>
        /// <param name="supabaseType">The Supabase data type</param>
        /// <returns>The corresponding C# type name</returns>
        public static string MapToCSharpType(string supabaseType)
        {
            if (string.IsNullOrEmpty(supabaseType))
                return "object";

            // Normalize the type name (remove any size constraints, etc.)
            string normalizedType = supabaseType.ToLower().Split('(')[0].Trim();
            
            switch (normalizedType)
            {
                // Numeric types
                case "int2":
                case "int4":
                case "integer":
                    return "int";
                
                case "int8":
                case "bigint":
                    return "long";
                
                case "float4":
                case "real":
                    return "float";
                
                case "float8":
                case "double precision":
                    return "double";
                
                case "numeric":
                case "decimal":
                    return "decimal";
                
                // Text types
                case "varchar":
                case "char":
                case "text":
                case "name":
                    return "string";
                
                // Boolean type
                case "bool":
                case "boolean":
                    return "bool";
                
                // Date/time types
                case "date":
                    return "DateTime";
                
                case "time":
                    return "TimeSpan";
                
                case "timetz":
                    return "DateTimeOffset";
                
                case "timestamp":
                case "timestamptz":
                    return "DateTime";
                
                // Binary data
                case "bytea":
                    return "byte[]";
                
                // JSON types
                case "json":
                case "jsonb":
                    return "string"; // Or consider using a JSON-specific type
                
                // UUID
                case "uuid":
                    return "Guid";
                
                // Array types
                case "array":
                    return "List<object>"; // Generic list, actual type would depend on the array elements
                
                // Default for unknown types
                default:
                    Debug.LogWarning($"Unknown Supabase type: {supabaseType}, mapping to 'object'");
                    return "object";
            }
        }

        /// <summary>
        /// Gets the default value for a C# type.
        /// </summary>
        /// <param name="csharpType">The C# type name</param>
        /// <returns>A string representation of the default value</returns>
        public static string GetDefaultValueForType(string csharpType)
        {
            switch (csharpType)
            {
                case "int":
                case "long":
                case "float":
                case "double":
                case "decimal":
                    return "0";
                
                case "bool":
                    return "false";
                
                case "string":
                    return "string.Empty";
                
                case "DateTime":
                    return "DateTime.MinValue";
                
                case "TimeSpan":
                    return "TimeSpan.Zero";
                
                case "DateTimeOffset":
                    return "DateTimeOffset.MinValue";
                
                case "Guid":
                    return "Guid.Empty";
                
                case "byte[]":
                    return "new byte[0]";
                
                default:
                    if (csharpType.StartsWith("List<"))
                    {
                        return $"new {csharpType}()";
                    }
                    return "null";
            }
        }

        /// <summary>
        /// Generates a C# class from a table schema.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="columns">The columns in the table</param>
        /// <param name="namespace">The namespace for the generated class</param>
        /// <returns>The generated C# class code</returns>
        public static string GenerateClassFromSchema(string tableName, List<ColumnInfo> columns, string @namespace = "SupabaseBridge.Models")
        {
            if (string.IsNullOrEmpty(tableName) || columns == null || columns.Count == 0)
            {
                throw new ArgumentException("Table name and columns are required");
            }
            
            StringBuilder sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            
            // Add namespace
            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine("{");
            
            // Add class documentation
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Represents a row in the {tableName} table.");
            sb.AppendLine("    /// </summary>");
            
            // Add class declaration
            string className = FormatClassName(tableName);
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");
            
            // Add properties for each column
            foreach (var column in columns)
            {
                string propertyName = FormatPropertyName(column.Name);
                string propertyType = MapToCSharpType(column.DataType);
                
                // Add property documentation
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Gets or sets the {column.Name} column value.");
                sb.AppendLine("        /// </summary>");
                
                // Add JsonProperty attribute if the property name differs from the column name
                if (propertyName != column.Name)
                {
                    sb.AppendLine($"        [SerializeField]");
                }
                
                // Add property
                sb.AppendLine($"        public {propertyType} {propertyName} {{ get; set; }}");
                sb.AppendLine();
            }
            
            // Add constructor
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// Initializes a new instance of the {className} class.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public {className}()");
            sb.AppendLine("        {");
            
            // Initialize properties with default values
            foreach (var column in columns)
            {
                string propertyName = FormatPropertyName(column.Name);
                string propertyType = MapToCSharpType(column.DataType);
                string defaultValue = GetDefaultValueForType(propertyType);
                
                sb.AppendLine($"            {propertyName} = {defaultValue};");
            }
            
            sb.AppendLine("        }");
            
            // Close class and namespace
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Formats a table name as a C# class name.
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <returns>A formatted class name</returns>
        private static string FormatClassName(string tableName)
        {
            // Remove schema prefix if present
            if (tableName.Contains("."))
            {
                tableName = tableName.Substring(tableName.LastIndexOf('.') + 1);
            }
            
            // Convert to PascalCase
            return ToPascalCase(tableName);
        }

        /// <summary>
        /// Formats a column name as a C# property name.
        /// </summary>
        /// <param name="columnName">The column name</param>
        /// <returns>A formatted property name</returns>
        private static string FormatPropertyName(string columnName)
        {
            // Convert to PascalCase
            return ToPascalCase(columnName);
        }

        /// <summary>
        /// Converts a string to PascalCase.
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The PascalCase string</returns>
        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            // Split by underscores, spaces, and hyphens
            string[] parts = input.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Convert each part to title case
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1).ToLower() : "");
                }
            }
            
            // Join the parts
            return string.Join("", parts);
        }
    }

    // Note: The database model classes (ColumnInfo, ForeignKeyInfo, TableInfo, TableSchema)
    // are defined in SupabaseDatabaseService.cs
}