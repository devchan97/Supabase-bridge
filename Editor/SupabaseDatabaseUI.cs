using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// UI class for managing Supabase database operations in the editor.
    /// </summary>
    public class SupabaseDatabaseUI
    {
        private readonly SupabaseDatabaseService databaseService;
        
        // UI state
        private Vector2 tablesScrollPosition;
        private Vector2 schemaScrollPosition;
        private Vector2 dataScrollPosition;
        private string selectedTable;
        private TableSchema selectedTableSchema;
        private List<Dictionary<string, object>> tableData;
        private bool isLoadingTables;
        private bool isLoadingSchema;
        private bool isLoadingData;
        private string errorMessage;
        private string successMessage;
        private int dataPageIndex = 0;
        private const int DATA_PAGE_SIZE = 20;
        private bool showSchemaTab = true;
        
        // Data editing
        private Dictionary<string, object> editingRow;
        private bool isEditing = false;
        private bool isCreatingNew = false;
        
        /// <summary>
        /// Initializes a new instance of the SupabaseDatabaseUI class.
        /// </summary>
        /// <param name="databaseService">The database service to use</param>
        public SupabaseDatabaseUI(SupabaseDatabaseService databaseService)
        {
            this.databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }
        
        /// <summary>
        /// Draws the database UI.
        /// </summary>
        public void Draw()
        {
            SupabaseEditorStyles.DrawSectionHeader("Database");
            
            SupabaseEditorStyles.DrawInfoBox("Manage Supabase database tables and schemas. You can view table structures, query data, and perform CRUD operations.");
            
            if (databaseService == null)
            {
                SupabaseEditorStyles.DrawErrorBox("Please connect to Supabase in the Project Setup tab first.");
                return;
            }
            
            EditorGUILayout.Space(10);
            
            // Refresh tables button
            if (GUILayout.Button("Refresh Table Structure", SupabaseEditorStyles.ButtonStyle))
            {
                FetchTablesAsync();
            }
            
            EditorGUILayout.Space(5);
            
            // Display error message if any
            if (!string.IsNullOrEmpty(errorMessage))
            {
                SupabaseEditorStyles.DrawErrorBox(errorMessage);
            }
            
            // Display success message if any
            if (!string.IsNullOrEmpty(successMessage))
            {
                SupabaseEditorStyles.DrawSuccessBox(successMessage);
            }
            
            EditorGUILayout.Space(10);
            
            // Split the view into two columns
            EditorGUILayout.BeginHorizontal();
            
            // Left column: Tables list
            DrawTablesList();
            
            // Right column: Table details
            DrawTableDetails();
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the tables list.
        /// </summary>
        private void DrawTablesList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            
            SupabaseEditorStyles.DrawSubHeader("Tables");
            
            // Tables list
            tablesScrollPosition = EditorGUILayout.BeginScrollView(tablesScrollPosition, EditorStyles.helpBox, GUILayout.Height(500));
            
            if (databaseService.Tables != null && databaseService.Tables.Count > 0)
            {
                foreach (var table in databaseService.Tables)
                {
                    bool isSelected = selectedTable == table.Name;
                    
                    if (GUILayout.Toggle(isSelected, $"{table.Name} ({table.RowCount})", isSelected ? SupabaseEditorStyles.SelectedListItemStyle : SupabaseEditorStyles.ListItemStyle))
                    {
                        if (!isSelected)
                        {
                            selectedTable = table.Name;
                            _ = FetchTableSchemaAsync(selectedTable);
                            _ = FetchTableDataAsync(selectedTable);
                        }
                    }
                }
            }
            else if (isLoadingTables)
            {
                EditorGUILayout.LabelField("Loading tables...");
            }
            else
            {
                EditorGUILayout.LabelField("No tables found.");
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the table details.
        /// </summary>
        private void DrawTableDetails()
        {
            EditorGUILayout.BeginVertical();
            
            if (!string.IsNullOrEmpty(selectedTable))
            {
                // Table schema
                SupabaseEditorStyles.DrawSubHeader($"Table: {selectedTable}");
                
                // Schema and data tabs
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Toggle(showSchemaTab, "Schema", showSchemaTab ? SupabaseEditorStyles.TabSelectedStyle : SupabaseEditorStyles.TabStyle))
                {
                    showSchemaTab = true;
                }
                
                if (GUILayout.Toggle(!showSchemaTab, "Data", !showSchemaTab ? SupabaseEditorStyles.TabSelectedStyle : SupabaseEditorStyles.TabStyle))
                {
                    showSchemaTab = false;
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (showSchemaTab)
                {
                    DrawSchemaTab();
                }
                else
                {
                    DrawDataTab();
                }
                
                EditorGUILayout.Space(10);
                
                // Generate data models button
                if (GUILayout.Button("Generate Data Models", SupabaseEditorStyles.ButtonStyle))
                {
                    GenerateDataModels();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Select a table to view its schema and data.");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the schema tab.
        /// </summary>
        private void DrawSchemaTab()
        {
            schemaScrollPosition = EditorGUILayout.BeginScrollView(schemaScrollPosition, EditorStyles.helpBox, GUILayout.Height(500));
            
            if (selectedTableSchema != null)
            {
                // Display columns
                EditorGUILayout.LabelField("Columns:", EditorStyles.boldLabel);
                
                // Column headers
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(150));
                EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("PK", EditorStyles.boldLabel, GUILayout.Width(30));
                EditorGUILayout.LabelField("Nullable", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("Default", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Draw a separator line
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
                EditorGUILayout.Space(5);
                
                foreach (var column in selectedTableSchema.Columns)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Column name
                    EditorGUILayout.LabelField(column.Name, GUILayout.Width(150));
                    
                    // Data type
                    EditorGUILayout.LabelField(column.DataType, GUILayout.Width(100));
                    
                    // Primary key indicator
                    if (column.IsPrimaryKey)
                    {
                        GUI.color = SupabaseEditorStyles.SupabaseGreen;
                        EditorGUILayout.LabelField("✓", GUILayout.Width(30));
                        GUI.color = Color.white;
                    }
                    else
                    {
                        EditorGUILayout.LabelField("", GUILayout.Width(30));
                    }
                    
                    // Nullable indicator
                    EditorGUILayout.LabelField(column.IsNullable ? "NULL" : "NOT NULL", GUILayout.Width(80));
                    
                    // Default value
                    if (!string.IsNullOrEmpty(column.DefaultValue))
                    {
                        EditorGUILayout.LabelField($"{column.DefaultValue}");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("-");
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space(10);
                
                // Display foreign keys
                if (selectedTableSchema.ForeignKeys != null && selectedTableSchema.ForeignKeys.Count > 0)
                {
                    EditorGUILayout.LabelField("Foreign Keys:", EditorStyles.boldLabel);
                    
                    // Foreign key headers
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Column", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField("References", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField("Referenced Column", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(5);
                    
                    // Draw a separator line
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    
                    EditorGUILayout.Space(5);
                    
                    foreach (var fk in selectedTableSchema.ForeignKeys)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        // Column name
                        EditorGUILayout.LabelField(fk.ColumnName, GUILayout.Width(150));
                        
                        // Referenced table
                        EditorGUILayout.LabelField(fk.ReferencedTable, GUILayout.Width(150));
                        
                        // Referenced column
                        EditorGUILayout.LabelField(fk.ReferencedColumn);
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No foreign keys defined for this table.");
                }
            }
            else if (isLoadingSchema)
            {
                EditorGUILayout.LabelField("Loading schema...");
            }
            else
            {
                EditorGUILayout.LabelField("No schema available.");
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Draws the data tab.
        /// </summary>
        private void DrawDataTab()
        {
            // Query controls
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh Data", GUILayout.Width(120)))
            {
                _ = FetchTableDataAsync(selectedTable);
            }
            
            if (GUILayout.Button("Add New", GUILayout.Width(120)))
            {
                StartNewRow();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Data table
            dataScrollPosition = EditorGUILayout.BeginScrollView(dataScrollPosition, EditorStyles.helpBox, GUILayout.Height(400));
            
            if (tableData != null && tableData.Count > 0 && selectedTableSchema != null)
            {
                // Table headers
                EditorGUILayout.BeginHorizontal();
                
                // Actions column
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(120));
                
                // Data columns
                foreach (var column in selectedTableSchema.Columns)
                {
                    EditorGUILayout.LabelField(column.Name, EditorStyles.boldLabel, GUILayout.Width(150));
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Draw a separator line
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
                EditorGUILayout.Space(5);
                
                // Display editing row if in edit mode
                if (isEditing && editingRow != null)
                {
                    DrawEditingRow();
                }
                
                // Display new row form if creating new
                if (isCreatingNew && editingRow != null)
                {
                    DrawNewRow();
                }
                
                // Table data rows
                int startIndex = dataPageIndex * DATA_PAGE_SIZE;
                int endIndex = Mathf.Min(startIndex + DATA_PAGE_SIZE, tableData.Count);
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    var row = tableData[i];
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Action buttons
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(120));
                    
                    if (GUILayout.Button("Edit", GUILayout.Width(50)))
                    {
                        StartEditingRow(row);
                    }
                    
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        DeleteRowAsync(row);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Data columns
                    foreach (var column in selectedTableSchema.Columns)
                    {
                        string value = row.ContainsKey(column.Name) ? row[column.Name]?.ToString() ?? "NULL" : "NULL";
                        EditorGUILayout.LabelField(value, GUILayout.Width(150));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                // Pagination controls
                EditorGUILayout.Space(10);
                
                EditorGUILayout.BeginHorizontal();
                
                // Previous page button
                GUI.enabled = dataPageIndex > 0;
                if (GUILayout.Button("Previous Page", GUILayout.Width(120)))
                {
                    dataPageIndex--;
                }
                
                // Page indicator
                int totalPages = Mathf.CeilToInt((float)tableData.Count / DATA_PAGE_SIZE);
                EditorGUILayout.LabelField($"Page {dataPageIndex + 1} of {totalPages}", GUILayout.Width(100));
                
                // Next page button
                GUI.enabled = (dataPageIndex + 1) * DATA_PAGE_SIZE < tableData.Count;
                if (GUILayout.Button("Next Page", GUILayout.Width(120)))
                {
                    dataPageIndex++;
                }
                
                GUI.enabled = true;
                
                EditorGUILayout.EndHorizontal();
            }
            else if (isLoadingData)
            {
                EditorGUILayout.LabelField("Loading data...");
            }
            else
            {
                EditorGUILayout.LabelField("No data available.");
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Draws the editing row.
        /// </summary>
        private void DrawEditingRow()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Action buttons
            EditorGUILayout.BeginHorizontal(GUILayout.Width(120));
            
            if (GUILayout.Button("Save", GUILayout.Width(50)))
            {
                SaveEditedRowAsync();
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Width(60)))
            {
                CancelEditing();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Data columns
            foreach (var column in selectedTableSchema.Columns)
            {
                // Skip primary key columns when editing
                if (column.IsPrimaryKey)
                {
                    string value = editingRow.ContainsKey(column.Name) ? editingRow[column.Name]?.ToString() ?? "NULL" : "NULL";
                    EditorGUILayout.LabelField(value, GUILayout.Width(150));
                    continue;
                }
                
                // Get the current value
                object currentValue = editingRow.ContainsKey(column.Name) ? editingRow[column.Name] : null;
                
                // Create the appropriate editor field based on data type
                object newValue = DrawColumnEditor(column, currentValue);
                
                // Update the value if changed
                if (newValue != null || column.IsNullable)
                {
                    editingRow[column.Name] = newValue;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Draw a separator line
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// Draws the new row form.
        /// </summary>
        private void DrawNewRow()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Action buttons
            EditorGUILayout.BeginHorizontal(GUILayout.Width(120));
            
            if (GUILayout.Button("Create", GUILayout.Width(50)))
            {
                CreateNewRowAsync();
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Width(60)))
            {
                CancelEditing();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Data columns
            foreach (var column in selectedTableSchema.Columns)
            {
                // Skip auto-generated primary keys
                if (column.IsPrimaryKey && column.DefaultValue != null)
                {
                    EditorGUILayout.LabelField("(Auto)", GUILayout.Width(150));
                    continue;
                }
                
                // Get the current value
                object currentValue = editingRow.ContainsKey(column.Name) ? editingRow[column.Name] : null;
                
                // Create the appropriate editor field based on data type
                object newValue = DrawColumnEditor(column, currentValue);
                
                // Update the value if changed
                if (newValue != null || column.IsNullable)
                {
                    editingRow[column.Name] = newValue;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Draw a separator line
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// Draws an editor field for a column based on its data type.
        /// </summary>
        /// <param name="column">The column information</param>
        /// <param name="currentValue">The current value</param>
        /// <returns>The new value</returns>
        private object DrawColumnEditor(ColumnInfo column, object currentValue)
        {
            // Handle different data types
            switch (column.DataType.ToLower())
            {
                case "text":
                case "varchar":
                case "char":
                case "uuid":
                    string strValue = currentValue?.ToString() ?? "";
                    return EditorGUILayout.TextField(strValue, GUILayout.Width(150));
                
                case "int":
                case "integer":
                case "smallint":
                case "bigint":
                    int intValue = 0;
                    if (currentValue != null)
                    {
                        int.TryParse(currentValue.ToString(), out intValue);
                    }
                    return EditorGUILayout.IntField(intValue, GUILayout.Width(150));
                
                case "float":
                case "real":
                case "double precision":
                case "numeric":
                case "decimal":
                    float floatValue = 0f;
                    if (currentValue != null)
                    {
                        float.TryParse(currentValue.ToString(), out floatValue);
                    }
                    return EditorGUILayout.FloatField(floatValue, GUILayout.Width(150));
                
                case "boolean":
                case "bool":
                    bool boolValue = false;
                    if (currentValue != null)
                    {
                        bool.TryParse(currentValue.ToString(), out boolValue);
                    }
                    return EditorGUILayout.Toggle(boolValue, GUILayout.Width(150));
                
                case "date":
                case "timestamp":
                case "timestamptz":
                    // For dates, we'll just use a string field for now
                    string dateValue = currentValue?.ToString() ?? "";
                    return EditorGUILayout.TextField(dateValue, GUILayout.Width(150));
                
                default:
                    // For unknown types, use a string field
                    string defaultValue = currentValue?.ToString() ?? "";
                    return EditorGUILayout.TextField(defaultValue, GUILayout.Width(150));
            }
        }
        
        /// <summary>
        /// Starts editing a row.
        /// </summary>
        /// <param name="row">The row to edit</param>
        private void StartEditingRow(Dictionary<string, object> row)
        {
            // Create a copy of the row to edit
            editingRow = new Dictionary<string, object>(row);
            isEditing = true;
            isCreatingNew = false;
        }
        
        /// <summary>
        /// Starts creating a new row.
        /// </summary>
        private void StartNewRow()
        {
            // Create an empty row
            editingRow = new Dictionary<string, object>();
            isEditing = false;
            isCreatingNew = true;
        }
        
        /// <summary>
        /// Cancels editing.
        /// </summary>
        private void CancelEditing()
        {
            editingRow = null;
            isEditing = false;
            isCreatingNew = false;
        }
        
        /// <summary>
        /// Saves the edited row asynchronously.
        /// </summary>
        private async void SaveEditedRowAsync()
        {
            try
            {
                isLoadingData = true;
                errorMessage = "";
                successMessage = "";
                
                // Find the primary key column
                var pkColumn = selectedTableSchema.Columns.FirstOrDefault(c => c.IsPrimaryKey);
                
                if (pkColumn == null)
                {
                    errorMessage = "Cannot update row: No primary key found.";
                    return;
                }
                
                // Get the primary key value
                if (!editingRow.TryGetValue(pkColumn.Name, out object pkValue) || pkValue == null)
                {
                    errorMessage = "Cannot update row: Primary key value is missing.";
                    return;
                }
                
                // Create a filter for the primary key
                string filter = $"{pkColumn.Name}=eq.{pkValue}";
                
                // Update the row
                await databaseService.Update(selectedTable, editingRow, filter);
                
                // Refresh the data
                await FetchTableDataAsync(selectedTable);
                
                successMessage = "Row updated successfully!";
                
                // Exit editing mode
                CancelEditing();
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to update row: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isLoadingData = false;
            }
        }
        
        /// <summary>
        /// Creates a new row asynchronously.
        /// </summary>
        private async void CreateNewRowAsync()
        {
            try
            {
                isLoadingData = true;
                errorMessage = "";
                successMessage = "";
                
                // Insert the new row
                await databaseService.Insert(selectedTable, editingRow);
                
                // Refresh the data
                await FetchTableDataAsync(selectedTable);
                
                successMessage = "Row created successfully!";
                
                // Exit editing mode
                CancelEditing();
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to create row: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isLoadingData = false;
            }
        }
        
        /// <summary>
        /// Deletes a row asynchronously.
        /// </summary>
        /// <param name="row">The row to delete</param>
        private async void DeleteRowAsync(Dictionary<string, object> row)
        {
            try
            {
                isLoadingData = true;
                errorMessage = "";
                successMessage = "";
                
                // Find the primary key column
                var pkColumn = selectedTableSchema.Columns.FirstOrDefault(c => c.IsPrimaryKey);
                
                if (pkColumn == null)
                {
                    errorMessage = "Cannot delete row: No primary key found.";
                    return;
                }
                
                // Get the primary key value
                if (!row.TryGetValue(pkColumn.Name, out object pkValue) || pkValue == null)
                {
                    errorMessage = "Cannot delete row: Primary key value is missing.";
                    return;
                }
                
                // Create a filter for the primary key
                string filter = $"{pkColumn.Name}=eq.{pkValue}";
                
                // Confirm deletion
                if (!EditorUtility.DisplayDialog("Confirm Deletion", 
                    $"Are you sure you want to delete this row?\n\nThis action cannot be undone.", 
                    "Delete", "Cancel"))
                {
                    return;
                }
                
                // Delete the row
                await databaseService.Delete(selectedTable, filter);
                
                // Refresh the data
                await FetchTableDataAsync(selectedTable);
                
                successMessage = "Row deleted successfully!";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to delete row: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isLoadingData = false;
            }
        }
        
        /// <summary>
        /// Fetches the list of tables asynchronously.
        /// </summary>
        private async void FetchTablesAsync()
        {
            try
            {
                isLoadingTables = true;
                errorMessage = "";
                successMessage = "";
                
                // Fetch tables
                await databaseService.FetchTables();
                
                // If we have a selected table, refresh its data
                if (!string.IsNullOrEmpty(selectedTable))
                {
                    _ = FetchTableSchemaAsync(selectedTable);
                    _ = FetchTableDataAsync(selectedTable);
                }
                
                successMessage = "Tables refreshed successfully!";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to fetch tables: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isLoadingTables = false;
            }
        }
        
        /// <summary>
        /// Fetches the schema for a table asynchronously.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        private async Task FetchTableSchemaAsync(string tableName)
        {
            try
            {
                isLoadingSchema = true;
                errorMessage = "";
                
                // Fetch table schema
                selectedTableSchema = await databaseService.GetTableSchema(tableName);
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to fetch table schema: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isLoadingSchema = false;
            }
        }
        
        /// <summary>
        /// Fetches the data for a table asynchronously.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        private async Task FetchTableDataAsync(string tableName)
        {
            try
            {
                isLoadingData = true;
                errorMessage = "";
                
                // Create query options
                var options = new QueryOptions
                {
                    Limit = DATA_PAGE_SIZE,
                    Offset = dataPageIndex * DATA_PAGE_SIZE
                };
                
                // Fetch table data
                tableData = await databaseService.QueryTable(tableName, options);
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to fetch table data: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isLoadingData = false;
            }
        }
        
        /// <summary>
        /// Generates data models for the selected table.
        /// </summary>
        private void GenerateDataModels()
        {
            if (string.IsNullOrEmpty(selectedTable) || selectedTableSchema == null)
            {
                errorMessage = "Cannot generate data models: No table selected.";
                return;
            }
            
            try
            {
                // Ask the user for the output path
                string defaultPath = "Supabase Bridge/Runtime/Models";
                string outputPath = EditorUtility.SaveFolderPanel("Select Output Folder for Model Classes", defaultPath, "");
                
                // If the user cancels, return
                if (string.IsNullOrEmpty(outputPath))
                {
                    return;
                }
                
                // Convert the full path to a path relative to the Assets folder
                if (outputPath.StartsWith(Application.dataPath))
                {
                    outputPath = outputPath.Substring(Application.dataPath.Length + 1);
                }
                
                // Ask the user for the namespace
                string namespaceName = EditorInputDialog.Show("Enter Namespace", "Enter the namespace for the generated classes:", "SupabaseBridge.Runtime");
                
                if (string.IsNullOrEmpty(namespaceName))
                {
                    namespaceName = "SupabaseBridge.Runtime";
                }
                
                // Create a code generator
                var codeGenerator = new CodeGenerator(SupabaseConfigManager.Instance, databaseService);
                
                // Generate the model
                GenerateDataModelsAsync(codeGenerator, new List<string> { selectedTable }, outputPath, namespaceName);
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to generate data models: {ex.Message}";
                Debug.LogError(errorMessage);
            }
        }
        
        /// <summary>
        /// Generates data models asynchronously.
        /// </summary>
        /// <param name="codeGenerator">The code generator</param>
        /// <param name="tableNames">The names of the tables to generate models for</param>
        /// <param name="outputPath">The output path for the generated files</param>
        /// <param name="namespaceName">The namespace for the generated classes</param>
        private async void GenerateDataModelsAsync(CodeGenerator codeGenerator, List<string> tableNames, string outputPath, string namespaceName)
        {
            try
            {
                // Show progress dialog
                EditorUtility.DisplayProgressBar("Generating Data Models", $"Generating model for table '{selectedTable}'...", 0.5f);
                
                // Generate the models
                List<string> generatedFiles = await codeGenerator.GenerateDataModels(tableNames, outputPath, namespaceName);
                
                // Show success message
                successMessage = $"Generated {generatedFiles.Count} model class(es) at '{outputPath}'";
                
                // Refresh the AssetDatabase to show the new files
                AssetDatabase.Refresh();
                
                // Open the first generated file in the editor
                if (generatedFiles.Count > 0)
                {
                    string relativePath = generatedFiles[0].Replace(Application.dataPath, "Assets");
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
                    if (asset != null)
                    {
                        AssetDatabase.OpenAsset(asset);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to generate data models: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                // Hide progress dialog
                EditorUtility.ClearProgressBar();
            }
        }
    }
}