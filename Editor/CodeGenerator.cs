using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Generates C# code for Supabase data models and API clients.
    /// </summary>
    public class CodeGenerator
    {
        private readonly SupabaseConfigManager configManager;
        private readonly SupabaseDatabaseService databaseService;
        
        // Default output paths
        private const string DEFAULT_RUNTIME_PATH = "Supabase Bridge/Runtime";
        private const string DEFAULT_MODELS_PATH = "Supabase Bridge/Runtime/Models";
        private const string DEFAULT_SERVICES_PATH = "Supabase Bridge/Runtime/Services";
        private const string DEFAULT_NAMESPACE = "SupabaseBridge.Runtime";
        
        // Template paths
        private const string TEMPLATES_PATH = "Supabase Bridge/Editor/Templates";
        private const string API_CLIENT_TEMPLATE = "ApiClientTemplate.txt";
        private const string AUTH_SERVICE_TEMPLATE = "AuthServiceTemplate.txt";
        private const string DATABASE_SERVICE_TEMPLATE = "DatabaseServiceTemplate.txt";
        private const string STORAGE_SERVICE_TEMPLATE = "StorageServiceTemplate.txt";
        
        /// <summary>
        /// Initializes a new instance of the CodeGenerator class.
        /// </summary>
        /// <param name="configManager">The configuration manager</param>
        /// <param name="databaseService">The database service</param>
        public CodeGenerator(SupabaseConfigManager configManager, SupabaseDatabaseService databaseService)
        {
            this.configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        
        /// <summary>
        /// Generates C# data model classes for the specified tables.
        /// </summary>
        /// <param name="tableNames">The names of the tables to generate models for</param>
        /// <param name="outputPath">The output path for the generated files</param>
        /// <param name="namespaceName">The namespace for the generated classes</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task<List<string>> GenerateDataModels(List<string> tableNames, string outputPath = null, string namespaceName = null)
        {
            if (tableNames == null || tableNames.Count == 0)
            {
                throw new ArgumentException("Table names cannot be null or empty", nameof(tableNames));
            }
            
            // Use default paths if not specified
            outputPath = string.IsNullOrEmpty(outputPath) ? DEFAULT_MODELS_PATH : outputPath;
            namespaceName = string.IsNullOrEmpty(namespaceName) ? DEFAULT_NAMESPACE : namespaceName;          
  
            // Create the output directory if it doesn't exist
            string fullPath = Path.Combine(Application.dataPath, outputPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            List<string> generatedFiles = new List<string>();
            
            // Generate a model for each table
            foreach (string tableName in tableNames)
            {
                // Get the table schema
                TableSchema schema = await databaseService.GetTableSchema(tableName);
                
                if (schema == null || schema.Columns == null || schema.Columns.Count == 0)
                {
                    Debug.LogWarning($"Could not generate model for table '{tableName}': Schema not available or empty");
                    continue;
                }
                
                // Generate the model class
                string className = FormatClassName(tableName);
                string code = GenerateModelClass(schema, className, namespaceName);
                
                // Write the code to a file
                string filePath = Path.Combine(fullPath, $"{className}.cs");
                File.WriteAllText(filePath, code);
                
                generatedFiles.Add(filePath);
                
                Debug.Log($"Generated model class for table '{tableName}' at '{filePath}'");
            }
            
            // Refresh the AssetDatabase to show the new files
            AssetDatabase.Refresh();
            
            return generatedFiles;
        }
        
        /// <summary>
        /// Generates a C# data model class for a table schema.
        /// </summary>
        /// <param name="schema">The table schema</param>
        /// <param name="className">The name of the class</param>
        /// <param name="namespaceName">The namespace for the class</param>
        /// <returns>The generated code</returns>
        private string GenerateModelClass(TableSchema schema, string className, string namespaceName)
        {
            StringBuilder sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            
            // Add namespace
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            
            // Add class documentation
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Represents a record from the '{schema.TableName}' table.");
            sb.AppendLine($"    /// </summary>");
            
            // Add class declaration with Serializable attribute
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");          
  
            // Add properties for each column
            foreach (var column in schema.Columns)
            {
                // Add property documentation
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Gets or sets the {FormatPropertyName(column.Name)} value.");
                
                // Add nullable information
                if (column.IsNullable)
                {
                    sb.AppendLine($"        /// This property can be null.");
                }
                
                // Add primary key information
                if (column.IsPrimaryKey)
                {
                    sb.AppendLine($"        /// This property is the primary key for the table.");
                }
                
                // Add default value information
                if (!string.IsNullOrEmpty(column.DefaultValue))
                {
                    sb.AppendLine($"        /// Default value: {column.DefaultValue}");
                }
                
                sb.AppendLine($"        /// </summary>");
                
                // Add SerializeField attribute for Unity serialization
                sb.AppendLine("        [SerializeField]");
                
                // Add property
                string propertyType = MapPostgresTypeToCSharType(column.DataType, column.IsNullable);
                string propertyName = FormatPropertyName(column.Name);
                
                sb.AppendLine($"        public {propertyType} {propertyName} {{ get; set; }}");
                
                // Add a blank line between properties
                sb.AppendLine();
            }
            
            // Add navigation properties for foreign keys
            if (schema.ForeignKeys != null && schema.ForeignKeys.Count > 0)
            {
                sb.AppendLine("        #region Navigation Properties");
                sb.AppendLine();
                
                foreach (var fk in schema.ForeignKeys)
                {
                    string referencedClassName = FormatClassName(fk.ReferencedTable);
                    string propertyName = FormatNavigationPropertyName(fk.ReferencedTable);
                    
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Gets or sets the related {referencedClassName} entity.");
                    sb.AppendLine($"        /// This property is a navigation property for the foreign key {fk.ColumnName}.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public {referencedClassName} {propertyName} {{ get; set; }}");
                    sb.AppendLine();
                }
                
                sb.AppendLine("        #endregion");
            }
            
            // Close class and namespace
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        } 
       
        /// <summary>
        /// Maps a PostgreSQL data type to a C# type.
        /// </summary>
        /// <param name="postgresType">The PostgreSQL data type</param>
        /// <param name="isNullable">Whether the column is nullable</param>
        /// <returns>The corresponding C# type</returns>
        private string MapPostgresTypeToCSharType(string postgresType, bool isNullable)
        {
            string csharpType;
            
            switch (postgresType.ToLower())
            {
                case "int":
                case "integer":
                case "smallint":
                    csharpType = "int";
                    break;
                
                case "bigint":
                    csharpType = "long";
                    break;
                
                case "real":
                case "float4":
                    csharpType = "float";
                    break;
                
                case "double precision":
                case "float8":
                    csharpType = "double";
                    break;
                
                case "numeric":
                case "decimal":
                    csharpType = "decimal";
                    break;
                
                case "boolean":
                case "bool":
                    csharpType = "bool";
                    break;
                
                case "uuid":
                    csharpType = "string";
                    break;
                
                case "date":
                    csharpType = "DateTime";
                    break;
                
                case "timestamp":
                case "timestamptz":
                case "timestamp with time zone":
                case "timestamp without time zone":
                    csharpType = "DateTime";
                    break;
                
                case "time":
                case "timetz":
                case "time with time zone":
                case "time without time zone":
                    csharpType = "TimeSpan";
                    break;
                
                case "json":
                case "jsonb":
                    csharpType = "string";
                    break;
                
                case "text":
                case "varchar":
                case "char":
                case "character":
                case "character varying":
                default:
                    csharpType = "string";
                    break;
            }  
          
            // Make value types nullable if the column is nullable
            if (isNullable && csharpType != "string")
            {
                if (csharpType == "int" || csharpType == "long" || csharpType == "float" || 
                    csharpType == "double" || csharpType == "decimal" || csharpType == "bool" || 
                    csharpType == "DateTime" || csharpType == "TimeSpan")
                {
                    csharpType += "?";
                }
            }
            
            return csharpType;
        }
        
        /// <summary>
        /// Formats a table name as a class name.
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <returns>The formatted class name</returns>
        private string FormatClassName(string tableName)
        {
            // Remove schema prefix if present
            if (tableName.Contains("."))
            {
                tableName = tableName.Split('.').Last();
            }
            
            // Convert to PascalCase
            return ToPascalCase(tableName);
        }
        
        /// <summary>
        /// Formats a column name as a property name.
        /// </summary>
        /// <param name="columnName">The column name</param>
        /// <returns>The formatted property name</returns>
        private string FormatPropertyName(string columnName)
        {
            // Convert to PascalCase
            return ToPascalCase(columnName);
        }
        
        /// <summary>
        /// Formats a table name as a navigation property name.
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <returns>The formatted navigation property name</returns>
        private string FormatNavigationPropertyName(string tableName)
        {
            // Remove schema prefix if present
            if (tableName.Contains("."))
            {
                tableName = tableName.Split('.').Last();
            }
            
            // Convert to PascalCase
            return ToPascalCase(tableName);
        }
        
        /// <summary>
        /// Converts a string to PascalCase.
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The PascalCase string</returns>
        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            } 
           
            // Handle snake_case
            if (input.Contains("_"))
            {
                StringBuilder sb = new StringBuilder();
                
                foreach (string part in input.Split('_'))
                {
                    if (!string.IsNullOrEmpty(part))
                    {
                        sb.Append(char.ToUpper(part[0]));
                        
                        if (part.Length > 1)
                        {
                            sb.Append(part.Substring(1).ToLower());
                        }
                    }
                }
                
                return sb.ToString();
            }
            
            // Handle simple case
            return char.ToUpper(input[0]) + (input.Length > 1 ? input.Substring(1).ToLower() : "");
        }
        
        /// <summary>
        /// Generates the API client code.
        /// </summary>
        /// <param name="outputPath">The output path for the generated file</param>
        /// <param name="namespaceName">The namespace for the generated class</param>
        /// <returns>The path to the generated file</returns>
        public string GenerateApiClient(string outputPath = null, string namespaceName = null)
        {
            // Use default paths if not specified
            outputPath = string.IsNullOrEmpty(outputPath) ? DEFAULT_SERVICES_PATH : outputPath;
            namespaceName = string.IsNullOrEmpty(namespaceName) ? DEFAULT_NAMESPACE : namespaceName;
            
            // Create the output directory if it doesn't exist
            string fullPath = Path.Combine(Application.dataPath, outputPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            // Get the current configuration
            var currentProfile = configManager.CurrentProfile;
            if (currentProfile == null)
            {
                throw new InvalidOperationException("No active Supabase profile found");
            }
            
            // Load the template
            string templatePath = Path.Combine(Application.dataPath, TEMPLATES_PATH, API_CLIENT_TEMPLATE);
            if (!File.Exists(templatePath))
            {
                // If template doesn't exist, create a default one
                string code = GenerateDefaultApiClientCode(namespaceName);
                
                // Write the code to a file
                string filePath = Path.Combine(fullPath, "SupabaseClient.cs");
                File.WriteAllText(filePath, code);
                
                // Refresh the AssetDatabase to show the new file
                AssetDatabase.Refresh();
                
                Debug.Log($"Generated API client at '{filePath}' (using default template)");
                return filePath;
            }       
     
            // Read the template
            string template = File.ReadAllText(templatePath);
            
            // Replace template placeholders
            string generatedCode = template
                .Replace("{{NAMESPACE}}", namespaceName)
                .Replace("{{SUPABASE_URL}}", currentProfile.SupabaseUrl)
                .Replace("{{SUPABASE_KEY}}", currentProfile.SupabaseKey)
                .Replace("{{GENERATION_DATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{{PROFILE_NAME}}", currentProfile.Name);
            
            // Write the code to a file
            string outputFilePath = Path.Combine(fullPath, "SupabaseClient.cs");
            File.WriteAllText(outputFilePath, generatedCode);
            
            // Refresh the AssetDatabase to show the new file
            AssetDatabase.Refresh();
            
            Debug.Log($"Generated API client at '{outputFilePath}'");
            return outputFilePath;
        }
        
        /// <summary>
        /// Generates the authentication service code.
        /// </summary>
        /// <param name="outputPath">The output path for the generated file</param>
        /// <param name="namespaceName">The namespace for the generated class</param>
        /// <returns>The path to the generated file</returns>
        public string GenerateAuthService(string outputPath = null, string namespaceName = null)
        {
            // Use default paths if not specified
            outputPath = string.IsNullOrEmpty(outputPath) ? DEFAULT_SERVICES_PATH : outputPath;
            namespaceName = string.IsNullOrEmpty(namespaceName) ? DEFAULT_NAMESPACE : namespaceName;
            
            // Create the output directory if it doesn't exist
            string fullPath = Path.Combine(Application.dataPath, outputPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            // Load the template
            string templatePath = Path.Combine(Application.dataPath, TEMPLATES_PATH, AUTH_SERVICE_TEMPLATE);
            if (!File.Exists(templatePath))
            {
                // If template doesn't exist, create a default one
                string code = GenerateDefaultAuthServiceCode(namespaceName);
                
                // Write the code to a file
                string filePath = Path.Combine(fullPath, "SupabaseAuthService.cs");
                File.WriteAllText(filePath, code);
                
                // Refresh the AssetDatabase to show the new file
                AssetDatabase.Refresh();
                
                Debug.Log($"Generated authentication service at '{filePath}' (using default template)");
                return filePath;
            }   
         
            // Read the template
            string template = File.ReadAllText(templatePath);
            
            // Replace template placeholders
            string generatedCode = template
                .Replace("{{NAMESPACE}}", namespaceName)
                .Replace("{{GENERATION_DATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            // Write the code to a file
            string outputFilePath = Path.Combine(fullPath, "SupabaseAuthService.cs");
            File.WriteAllText(outputFilePath, generatedCode);
            
            // Refresh the AssetDatabase to show the new file
            AssetDatabase.Refresh();
            
            Debug.Log($"Generated authentication service at '{outputFilePath}'");
            return outputFilePath;
        }
        
        /// <summary>
        /// Generates the database service code.
        /// </summary>
        /// <param name="outputPath">The output path for the generated file</param>
        /// <param name="namespaceName">The namespace for the generated class</param>
        /// <returns>The path to the generated file</returns>
        public string GenerateDatabaseService(string outputPath = null, string namespaceName = null)
        {
            // Use default paths if not specified
            outputPath = string.IsNullOrEmpty(outputPath) ? DEFAULT_SERVICES_PATH : outputPath;
            namespaceName = string.IsNullOrEmpty(namespaceName) ? DEFAULT_NAMESPACE : namespaceName;
            
            // Create the output directory if it doesn't exist
            string fullPath = Path.Combine(Application.dataPath, outputPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            // Load the template
            string templatePath = Path.Combine(Application.dataPath, TEMPLATES_PATH, DATABASE_SERVICE_TEMPLATE);
            if (!File.Exists(templatePath))
            {
                // If template doesn't exist, create a default one
                string code = GenerateDefaultDatabaseServiceCode(namespaceName);
                
                // Write the code to a file
                string filePath = Path.Combine(fullPath, "SupabaseDatabaseService.cs");
                File.WriteAllText(filePath, code);
                
                // Refresh the AssetDatabase to show the new file
                AssetDatabase.Refresh();
                
                Debug.Log($"Generated database service at '{filePath}' (using default template)");
                return filePath;
            }      
      
            // Read the template
            string template = File.ReadAllText(templatePath);
            
            // Replace template placeholders
            string generatedCode = template
                .Replace("{{NAMESPACE}}", namespaceName)
                .Replace("{{GENERATION_DATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            // Write the code to a file
            string outputFilePath = Path.Combine(fullPath, "SupabaseDatabaseService.cs");
            File.WriteAllText(outputFilePath, generatedCode);
            
            // Refresh the AssetDatabase to show the new file
            AssetDatabase.Refresh();
            
            Debug.Log($"Generated database service at '{outputFilePath}'");
            return outputFilePath;
        }
        
        /// <summary>
        /// Generates the storage service code.
        /// </summary>
        /// <param name="outputPath">The output path for the generated file</param>
        /// <param name="namespaceName">The namespace for the generated class</param>
        /// <returns>The path to the generated file</returns>
        public string GenerateStorageService(string outputPath = null, string namespaceName = null)
        {
            // Use default paths if not specified
            outputPath = string.IsNullOrEmpty(outputPath) ? DEFAULT_SERVICES_PATH : outputPath;
            namespaceName = string.IsNullOrEmpty(namespaceName) ? DEFAULT_NAMESPACE : namespaceName;
            
            // Create the output directory if it doesn't exist
            string fullPath = Path.Combine(Application.dataPath, outputPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            // Load the template
            string templatePath = Path.Combine(Application.dataPath, TEMPLATES_PATH, STORAGE_SERVICE_TEMPLATE);
            if (!File.Exists(templatePath))
            {
                // If template doesn't exist, create a default one
                string code = GenerateDefaultStorageServiceCode(namespaceName);
                
                // Write the code to a file
                string filePath = Path.Combine(fullPath, "SupabaseStorageService.cs");
                File.WriteAllText(filePath, code);
                
                // Refresh the AssetDatabase to show the new file
                AssetDatabase.Refresh();
                
                Debug.Log($"Generated storage service at '{filePath}' (using default template)");
                return filePath;
            }           
 
            // Read the template
            string template = File.ReadAllText(templatePath);
            
            // Replace template placeholders
            string generatedCode = template
                .Replace("{{NAMESPACE}}", namespaceName)
                .Replace("{{GENERATION_DATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            // Write the code to a file
            string outputFilePath = Path.Combine(fullPath, "SupabaseStorageService.cs");
            File.WriteAllText(outputFilePath, generatedCode);
            
            // Refresh the AssetDatabase to show the new file
            AssetDatabase.Refresh();
            
            Debug.Log($"Generated storage service at '{outputFilePath}'");
            return outputFilePath;
        }
        
        /// <summary>
        /// Generates all API code (client and services).
        /// </summary>
        /// <param name="outputPath">The output path for the generated files</param>
        /// <param name="namespaceName">The namespace for the generated classes</param>
        /// <returns>A list of paths to the generated files</returns>
        public List<string> GenerateAllApiCode(string outputPath = null, string namespaceName = null)
        {
            List<string> generatedFiles = new List<string>();
            
            try
            {
                // Generate API client
                string apiClientPath = GenerateApiClient(outputPath, namespaceName);
                generatedFiles.Add(apiClientPath);
                
                // Generate authentication service
                string authServicePath = GenerateAuthService(outputPath, namespaceName);
                generatedFiles.Add(authServicePath);
                
                // Generate database service
                string dbServicePath = GenerateDatabaseService(outputPath, namespaceName);
                generatedFiles.Add(dbServicePath);
                
                // Generate storage service
                string storageServicePath = GenerateStorageService(outputPath, namespaceName);
                generatedFiles.Add(storageServicePath);
                
                Debug.Log($"Generated all API code successfully. Files: {generatedFiles.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating API code: {ex.Message}");
                throw;
            }
            
            return generatedFiles;
        }  
      
        /// <summary>
        /// Creates template files if they don't exist.
        /// </summary>
        public void EnsureTemplatesExist()
        {
            // Create the templates directory if it doesn't exist
            string templatesDir = Path.Combine(Application.dataPath, TEMPLATES_PATH);
            if (!Directory.Exists(templatesDir))
            {
                Directory.CreateDirectory(templatesDir);
            }
            
            // Create API client template if it doesn't exist
            string apiClientTemplatePath = Path.Combine(templatesDir, API_CLIENT_TEMPLATE);
            if (!File.Exists(apiClientTemplatePath))
            {
                File.WriteAllText(apiClientTemplatePath, GetDefaultApiClientTemplate());
            }
            
            // Create auth service template if it doesn't exist
            string authServiceTemplatePath = Path.Combine(templatesDir, AUTH_SERVICE_TEMPLATE);
            if (!File.Exists(authServiceTemplatePath))
            {
                File.WriteAllText(authServiceTemplatePath, GetDefaultAuthServiceTemplate());
            }
            
            // Create database service template if it doesn't exist
            string dbServiceTemplatePath = Path.Combine(templatesDir, DATABASE_SERVICE_TEMPLATE);
            if (!File.Exists(dbServiceTemplatePath))
            {
                File.WriteAllText(dbServiceTemplatePath, GetDefaultDatabaseServiceTemplate());
            }
            
            // Create storage service template if it doesn't exist
            string storageServiceTemplatePath = Path.Combine(templatesDir, STORAGE_SERVICE_TEMPLATE);
            if (!File.Exists(storageServiceTemplatePath))
            {
                File.WriteAllText(storageServiceTemplatePath, GetDefaultStorageServiceTemplate());
            }
            
            // Refresh the AssetDatabase to show the new files
            AssetDatabase.Refresh();
            
            Debug.Log("Template files created successfully");
        }
        
        #region Default Code Generation Methods
        
        /// <summary>
        /// Generates default API client code.
        /// </summary>
        /// <param name="namespaceName">The namespace for the generated class</param>
        /// <returns>The generated code</returns>
        private string GenerateDefaultApiClientCode(string namespaceName)
        {
            var currentProfile = configManager.CurrentProfile;
            string supabaseUrl = currentProfile?.SupabaseUrl ?? "https://your-project-url.supabase.co";
            string supabaseKey = currentProfile?.SupabaseKey ?? "your-api-key"; 
           
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Client for interacting with the Supabase API.");
            sb.AppendLine("    /// Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class SupabaseClient");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly HttpClient httpClient;");
            sb.AppendLine("        private readonly string baseUrl;");
            sb.AppendLine("        private readonly string apiKey;");
            sb.AppendLine("        private string accessToken;");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Initializes a new instance of the SupabaseClient class.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public SupabaseClient()");
            sb.AppendLine("        {");
            sb.AppendLine($"            this.baseUrl = \"{supabaseUrl}\";");
            sb.AppendLine($"            this.apiKey = \"{supabaseKey}\";");
            sb.AppendLine("            this.httpClient = new HttpClient();");
            sb.AppendLine("            this.httpClient.DefaultRequestHeaders.Add(\"apikey\", apiKey);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Initializes a new instance of the SupabaseClient class with custom URL and key.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"url\">The Supabase project URL</param>");
            sb.AppendLine("        /// <param name=\"key\">The Supabase API key</param>");
            sb.AppendLine("        public SupabaseClient(string url, string key)");
            sb.AppendLine("        {");
            sb.AppendLine("            this.baseUrl = url;");
            sb.AppendLine("            this.apiKey = key;");
            sb.AppendLine("            this.httpClient = new HttpClient();");
            sb.AppendLine("            this.httpClient.DefaultRequestHeaders.Add(\"apikey\", apiKey);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Sets the access token for authenticated requests.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"token\">The access token</param>");
            sb.AppendLine("        public void SetAccessToken(string token)");
            sb.AppendLine("        {");
            sb.AppendLine("            this.accessToken = token;");
            sb.AppendLine("            ");
            sb.AppendLine("            // Update the Authorization header");
            sb.AppendLine("            if (this.httpClient.DefaultRequestHeaders.Contains(\"Authorization\"))");
            sb.AppendLine("            {");
            sb.AppendLine("                this.httpClient.DefaultRequestHeaders.Remove(\"Authorization\");");
            sb.AppendLine("            }");
            sb.AppendLine("            ");
            sb.AppendLine("            if (!string.IsNullOrEmpty(token))");
            sb.AppendLine("            {");
            sb.AppendLine("                this.httpClient.DefaultRequestHeaders.Add(\"Authorization\", $\"Bearer {token}\");");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets the base URL of the Supabase project.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <returns>The base URL</returns>");
            sb.AppendLine("        public string GetBaseUrl()");
            sb.AppendLine("        {");
            sb.AppendLine("            return this.baseUrl;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Add more methods for API interaction here");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }  
      
        /// <summary>
        /// Generates default authentication service code.
        /// </summary>
        /// <param name="namespaceName">The namespace for the generated class</param>
        /// <returns>The generated code</returns>
        private string GenerateDefaultAuthServiceCode(string namespaceName)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Service for handling Supabase authentication.");
            sb.AppendLine("    /// Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class SupabaseAuthService");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly SupabaseClient client;");
            sb.AppendLine("        private User currentUser;");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Event that is triggered when the authentication state changes.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public event Action<User> OnAuthStateChanged;");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets a value indicating whether the user is authenticated.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public bool IsAuthenticated => currentUser != null;");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets the current user.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public User CurrentUser => currentUser;");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Initializes a new instance of the SupabaseAuthService class.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"client\">The Supabase client</param>");
            sb.AppendLine("        public SupabaseAuthService(SupabaseClient client)");
            sb.AppendLine("        {");
            sb.AppendLine("            this.client = client;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Add authentication methods here");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Represents a Supabase user.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine("    public class User");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the user ID.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string Id { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the user email.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string Email { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the access token.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string AccessToken { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the refresh token.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string RefreshToken { get; set; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }  
      
        /// <summary>
        /// Generates default database service code.
        /// </summary>
        /// <param name="namespaceName">The namespace for the generated class</param>
        /// <returns>The generated code</returns>
        private string GenerateDefaultDatabaseServiceCode(string namespaceName)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Service for interacting with the Supabase database.");
            sb.AppendLine("    /// Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class SupabaseDatabaseService");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly SupabaseClient client;");
            sb.AppendLine("        private const string REST_ENDPOINT = \"/rest/v1/\";");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Initializes a new instance of the SupabaseDatabaseService class.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"client\">The Supabase client</param>");
            sb.AppendLine("        public SupabaseDatabaseService(SupabaseClient client)");
            sb.AppendLine("        {");
            sb.AppendLine("            this.client = client;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Add database methods here");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Represents options for querying a table.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class QueryOptions");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the columns to select.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public List<string> Select { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the filters to apply.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public Dictionary<string, string> Filters { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the column to order by.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string OrderBy { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets a value indicating whether to order in ascending order.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public bool? Ascending { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the maximum number of records to return.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public int? Limit { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the number of records to skip.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public int? Offset { get; set; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }     
   
        /// <summary>
        /// Generates default storage service code.
        /// </summary>
        /// <param name="namespaceName">The namespace for the generated class</param>
        /// <returns>The generated code</returns>
        private string GenerateDefaultStorageServiceCode(string namespaceName)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Service for interacting with Supabase storage.");
            sb.AppendLine("    /// Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class SupabaseStorageService");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly SupabaseClient client;");
            sb.AppendLine("        private const string STORAGE_ENDPOINT = \"/storage/v1\";");
            sb.AppendLine("        private const string BUCKETS_ENDPOINT = \"/storage/v1/bucket\";");
            sb.AppendLine("        private const string OBJECTS_ENDPOINT = \"/storage/v1/object\";");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Initializes a new instance of the SupabaseStorageService class.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"client\">The Supabase client</param>");
            sb.AppendLine("        public SupabaseStorageService(SupabaseClient client)");
            sb.AppendLine("        {");
            sb.AppendLine("            this.client = client;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        // Add storage methods here");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Represents information about a storage bucket.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine("    public class BucketInfo");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the name of the bucket.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string Name { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets a value indicating whether the bucket is public.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public bool IsPublic { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the creation date of the bucket.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public DateTime CreatedAt { get; set; }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Represents information about a file in storage.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine("    public class FileObject");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the name of the file.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string Name { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the name of the bucket containing the file.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string BucketName { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the content type of the file.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string ContentType { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the size of the file in bytes.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public long Size { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the creation date of the file.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public DateTime CreatedAt { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the last update date of the file.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public DateTime UpdatedAt { get; set; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }        
 
       #endregion
        
        #region Template Methods
        
        /// <summary>
        /// Gets the default API client template.
        /// </summary>
        /// <returns>The template content</returns>
        private string GetDefaultApiClientTemplate()
        {
            return @"using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// Client for interacting with the Supabase API.
    /// Generated on: {{GENERATION_DATE}}
    /// Profile: {{PROFILE_NAME}}
    /// </summary>
    public class SupabaseClient
    {
        private readonly HttpClient httpClient;
        private readonly string baseUrl;
        private readonly string apiKey;
        private string accessToken;
        
        /// <summary>
        /// Initializes a new instance of the SupabaseClient class.
        /// </summary>
        public SupabaseClient()
        {
            this.baseUrl = ""{{SUPABASE_URL}}"";
            this.apiKey = ""{{SUPABASE_KEY}}"";
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add(""apikey"", apiKey);
        }
        
        /// <summary>
        /// Initializes a new instance of the SupabaseClient class with custom URL and key.
        /// </summary>
        /// <param name=""url"">The Supabase project URL</param>
        /// <param name=""key"">The Supabase API key</param>
        public SupabaseClient(string url, string key)
        {
            this.baseUrl = url;
            this.apiKey = key;
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add(""apikey"", apiKey);
        }
        
        /// <summary>
        /// Sets the access token for authenticated requests.
        /// </summary>
        /// <param name=""token"">The access token</param>
        public void SetAccessToken(string token)
        {
            this.accessToken = token;
            
            // Update the Authorization header
            if (this.httpClient.DefaultRequestHeaders.Contains(""Authorization""))
            {
                this.httpClient.DefaultRequestHeaders.Remove(""Authorization"");
            }
            
            if (!string.IsNullOrEmpty(token))
            {
                this.httpClient.DefaultRequestHeaders.Add(""Authorization"", $""Bearer {token}"");
            }
        }
        
        /// <summary>
        /// Gets the base URL of the Supabase project.
        /// </summary>
        /// <returns>The base URL</returns>
        public string GetBaseUrl()
        {
            return this.baseUrl;
        }
        
        // Add more methods for API interaction here
    }
}";
        }    
    
        /// <summary>
        /// Gets the default authentication service template.
        /// </summary>
        /// <returns>The template content</returns>
        private string GetDefaultAuthServiceTemplate()
        {
            return @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// Service for handling Supabase authentication.
    /// Generated on: {{GENERATION_DATE}}
    /// </summary>
    public class SupabaseAuthService
    {
        private readonly SupabaseClient client;
        private User currentUser;
        
        /// <summary>
        /// Event that is triggered when the authentication state changes.
        /// </summary>
        public event Action<User> OnAuthStateChanged;
        
        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        public bool IsAuthenticated => currentUser != null;
        
        /// <summary>
        /// Gets the current user.
        /// </summary>
        public User CurrentUser => currentUser;
        
        /// <summary>
        /// Initializes a new instance of the SupabaseAuthService class.
        /// </summary>
        /// <param name=""client"">The Supabase client</param>
        public SupabaseAuthService(SupabaseClient client)
        {
            this.client = client;
        }
        
        // Add authentication methods here
    }
    
    /// <summary>
    /// Represents a Supabase user.
    /// </summary>
    [Serializable]
    public class User
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the user email.
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; }
        
        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        public string RefreshToken { get; set; }
    }
}";
        }        

        /// <summary>
        /// Gets the default database service template.
        /// </summary>
        /// <returns>The template content</returns>
        private string GetDefaultDatabaseServiceTemplate()
        {
            return @"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// Service for interacting with the Supabase database.
    /// Generated on: {{GENERATION_DATE}}
    /// </summary>
    public class SupabaseDatabaseService
    {
        private readonly SupabaseClient client;
        private const string REST_ENDPOINT = ""/rest/v1/"";
        
        /// <summary>
        /// Initializes a new instance of the SupabaseDatabaseService class.
        /// </summary>
        /// <param name=""client"">The Supabase client</param>
        public SupabaseDatabaseService(SupabaseClient client)
        {
            this.client = client;
        }
        
        // Add database methods here
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
}";
        }   
     
        /// <summary>
        /// Gets the default storage service template.
        /// </summary>
        /// <returns>The template content</returns>
        private string GetDefaultStorageServiceTemplate()
        {
            return @"using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace {{NAMESPACE}}
{
    /// <summary>
    /// Service for interacting with Supabase storage.
    /// Generated on: {{GENERATION_DATE}}
    /// </summary>
    public class SupabaseStorageService
    {
        private readonly SupabaseClient client;
        private const string STORAGE_ENDPOINT = ""/storage/v1"";
        private const string BUCKETS_ENDPOINT = ""/storage/v1/bucket"";
        private const string OBJECTS_ENDPOINT = ""/storage/v1/object"";
        
        /// <summary>
        /// Initializes a new instance of the SupabaseStorageService class.
        /// </summary>
        /// <param name=""client"">The Supabase client</param>
        public SupabaseStorageService(SupabaseClient client)
        {
            this.client = client;
        }
        
        // Add storage methods here
    }
    
    /// <summary>
    /// Represents information about a storage bucket.
    /// </summary>
    [Serializable]
    public class BucketInfo
    {
        /// <summary>
        /// Gets or sets the name of the bucket.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the bucket is public.
        /// </summary>
        public bool IsPublic { get; set; }
        
        /// <summary>
        /// Gets or sets the creation date of the bucket.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
    
    /// <summary>
    /// Represents information about a file in storage.
    /// </summary>
    [Serializable]
    public class FileObject
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the bucket containing the file.
        /// </summary>
        public string BucketName { get; set; }
        
        /// <summary>
        /// Gets or sets the content type of the file.
        /// </summary>
        public string ContentType { get; set; }
        
        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        public long Size { get; set; }
        
        /// <summary>
        /// Gets or sets the creation date of the file.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the last update date of the file.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}";
        }
        
        #endregion
    }
}