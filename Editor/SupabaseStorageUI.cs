using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// UI component for managing Supabase storage.
    /// Provides UI for bucket management and file operations.
    /// </summary>
    public class SupabaseStorageUI
    {
        private readonly SupabaseStorageService storageService;
        
        // UI state
        private string bucketName = "";
        private bool isPublicBucket = false;
        private string filePath = "";
        private string storageFilePath = "";
        private Vector2 bucketsScrollPosition;
        private Vector2 filesScrollPosition;
        private Vector2 previewScrollPosition;
        private string currentFolder = "";
        private string searchFilter = "";
        private bool showPreview = true;
        private float uploadProgress = 0f;
        
        // Operation state
        private bool isCheckingBucket = false;
        private bool isCreatingBucket = false;
        private bool isUploadingFile = false;
        private bool isListingFiles = false;
        private bool isDeletingFile = false;
        private bool isGettingFileUrl = false;
        
        // Results
        private bool? bucketExists = null;
        private List<BucketInfo> buckets = new List<BucketInfo>();
        private List<FileObject> files = new List<FileObject>();
        private string fileUrl = "";
        private Texture2D previewTexture = null;
        private string previewText = "";
        
        // Selected items
        private string selectedBucket = "";
        private string selectedFile = "";
        
        // Messages
        private string errorMessage = "";
        private string successMessage = "";
        
        // File view mode
        private enum FileViewMode { List, Grid }
        private FileViewMode currentViewMode = FileViewMode.List;
        
        /// <summary>
        /// Initializes a new instance of the SupabaseStorageUI class.
        /// </summary>
        /// <param name="storageService">The storage service to use</param>
        public SupabaseStorageUI(SupabaseStorageService storageService)
        {
            this.storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }
        
        /// <summary>
        /// Draws the storage UI.
        /// </summary>
        public void Draw()
        {
            SupabaseEditorStyles.DrawSectionHeader("Storage");
            
            SupabaseEditorStyles.DrawInfoBox("Manage Supabase storage buckets and files. You can create buckets, upload files, and manage file permissions.");
            
            EditorGUILayout.Space(10);
            
            // Draw the bucket management section
            DrawBucketManagementSection();
            
            EditorGUILayout.Space(20);
            
            // Draw the file management section
            DrawFileManagementSection();
            
            EditorGUILayout.Space(10);
            
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
            
            // Handle drag and drop for file uploads
            HandleDragAndDrop();
        }
        
        /// <summary>
        /// Draws the bucket management section.
        /// </summary>
        private void DrawBucketManagementSection()
        {
            SupabaseEditorStyles.DrawSubHeader("Bucket Management");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Bucket name field
            EditorGUILayout.LabelField("Bucket Name");
            bucketName = EditorGUILayout.TextField(bucketName);
            
            EditorGUILayout.Space(5);
            
            // Public bucket toggle
            isPublicBucket = EditorGUILayout.Toggle("Public Bucket", isPublicBucket);
            
            EditorGUILayout.Space(10);
            
            // Bucket operations
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !isCheckingBucket && !isCreatingBucket && !string.IsNullOrEmpty(bucketName);
            
            if (GUILayout.Button("Check if Bucket Exists", SupabaseEditorStyles.ButtonStyle))
            {
                CheckBucketExistsAsync(bucketName);
            }
            
            if (GUILayout.Button("Create Bucket", SupabaseEditorStyles.ButtonStyle))
            {
                CreateBucketAsync(bucketName, isPublicBucket);
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Display bucket existence status if available
            if (bucketExists.HasValue)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Bucket Status:", GUILayout.Width(100));
                
                if (bucketExists.Value)
                {
                    EditorGUILayout.LabelField("Exists", EditorStyles.boldLabel);
                    GUI.color = SupabaseEditorStyles.SupabaseGreen;
                    EditorGUILayout.LabelField("●", GUILayout.Width(20));
                }
                else
                {
                    EditorGUILayout.LabelField("Does Not Exist", EditorStyles.boldLabel);
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("●", GUILayout.Width(20));
                }
                
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(10);
            
            // List buckets button
            if (GUILayout.Button("List Buckets", SupabaseEditorStyles.ButtonStyle))
            {
                ListBucketsAsync();
            }
            
            EditorGUILayout.Space(5);
            
            // Display buckets if available
            if (buckets.Count > 0)
            {
                EditorGUILayout.LabelField("Available Buckets:", EditorStyles.boldLabel);
                
                bucketsScrollPosition = EditorGUILayout.BeginScrollView(bucketsScrollPosition, GUILayout.Height(150));
                
                foreach (var bucket in buckets)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    bool isSelected = selectedBucket == bucket.Name;
                    bool newIsSelected = EditorGUILayout.ToggleLeft(bucket.Name, isSelected);
                    
                    if (newIsSelected != isSelected)
                    {
                        selectedBucket = newIsSelected ? bucket.Name : "";
                        
                        if (newIsSelected)
                        {
                            // Load files for the selected bucket
                            ListFilesAsync(selectedBucket);
                        }
                    }
                    
                    EditorGUILayout.LabelField(bucket.IsPublic ? "Public" : "Private", GUILayout.Width(60));
                    EditorGUILayout.LabelField(bucket.CreatedAt.ToString("yyyy-MM-dd"), GUILayout.Width(100));
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the file management section.
        /// </summary>
        private void DrawFileManagementSection()
        {
            SupabaseEditorStyles.DrawSubHeader("File Management");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Selected bucket info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Bucket:", GUILayout.Width(100));
            
            if (!string.IsNullOrEmpty(selectedBucket))
            {
                EditorGUILayout.LabelField(selectedBucket, EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.LabelField("None", EditorStyles.boldLabel);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // File upload section
            if (!string.IsNullOrEmpty(selectedBucket))
            {
                // Draw the upload section
                DrawUploadSection();
                
                EditorGUILayout.Space(10);
                
                // Draw the file browser section
                DrawFileBrowserSection();
                
                EditorGUILayout.Space(10);
                
                // Draw the file preview section if a file is selected
                if (!string.IsNullOrEmpty(selectedFile) && showPreview)
                {
                    DrawFilePreviewSection();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Select a bucket to manage files.");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the file upload section.
        /// </summary>
        private void DrawUploadSection()
        {
            SupabaseEditorStyles.DrawSubHeader("Upload Files");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Local file path field
            EditorGUILayout.LabelField("Local File Path");
            
            EditorGUILayout.BeginHorizontal();
            filePath = EditorGUILayout.TextField(filePath);
            
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("Select File to Upload", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    filePath = path;
                    // Auto-fill storage path with filename if empty
                    if (string.IsNullOrEmpty(storageFilePath))
                    {
                        string fileName = Path.GetFileName(path);
                        storageFilePath = string.IsNullOrEmpty(currentFolder) ? 
                            fileName : 
                            Path.Combine(currentFolder, fileName).Replace("\\", "/");
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Storage file path field
            EditorGUILayout.LabelField("Storage File Path (e.g., 'folder/file.txt')");
            
            EditorGUILayout.BeginHorizontal();
            storageFilePath = EditorGUILayout.TextField(storageFilePath);
            
            if (!string.IsNullOrEmpty(currentFolder) && GUILayout.Button("Use Current Folder", GUILayout.Width(120)))
            {
                string fileName = string.IsNullOrEmpty(filePath) ? 
                    "file.txt" : 
                    Path.GetFileName(filePath);
                    
                storageFilePath = string.IsNullOrEmpty(currentFolder) ? 
                    fileName : 
                    Path.Combine(currentFolder, fileName).Replace("\\", "/");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Upload button
            GUI.enabled = !isUploadingFile && !string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(storageFilePath);
            
            if (GUILayout.Button("Upload File", SupabaseEditorStyles.ButtonStyle))
            {
                UploadFileAsync(selectedBucket, filePath, storageFilePath);
            }
            
            GUI.enabled = true;
            
            // Display upload progress if uploading
            if (isUploadingFile)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Uploading...");
                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(rect, uploadProgress, $"{(uploadProgress * 100):F0}%");
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox("You can also drag and drop files directly onto this window to upload them.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the file browser section.
        /// </summary>
        private void DrawFileBrowserSection()
        {
            SupabaseEditorStyles.DrawSubHeader("File Browser");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Toolbar with refresh button, search field, and view mode toggle
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ListFilesAsync(selectedBucket, currentFolder);
            }
            
            // Folder navigation
            if (!string.IsNullOrEmpty(currentFolder))
            {
                if (GUILayout.Button("↑ Up", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    // Go up one level
                    int lastSlash = currentFolder.LastIndexOf('/');
                    if (lastSlash > 0)
                    {
                        currentFolder = currentFolder.Substring(0, lastSlash);
                    }
                    else
                    {
                        currentFolder = "";
                    }
                    
                    ListFilesAsync(selectedBucket, currentFolder);
                }
            }
            
            // Current folder display
            GUILayout.Label(string.IsNullOrEmpty(currentFolder) ? "/" : $"/{currentFolder}/", EditorStyles.toolbarButton);
            
            GUILayout.FlexibleSpace();
            
            // Search field
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            string newSearchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));
            if (newSearchFilter != searchFilter)
            {
                searchFilter = newSearchFilter;
                // We don't need to refresh the files here, just filter the existing list
            }
            
            // View mode toggle
            EditorGUILayout.LabelField("View:", GUILayout.Width(40));
            if (GUILayout.Button("List", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                currentViewMode = FileViewMode.List;
            }
            if (GUILayout.Button("Grid", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                currentViewMode = FileViewMode.Grid;
            }
            
            // Preview toggle
            showPreview = GUILayout.Toggle(showPreview, "Preview", EditorStyles.toolbarButton, GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Display files if available
            if (files.Count > 0)
            {
                // Filter files based on search term
                var filteredFiles = files;
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    filteredFiles = files.Where(f => f.Name.ToLower().Contains(searchFilter.ToLower())).ToList();
                }
                
                // Group files by folders
                var folders = new HashSet<string>();
                var filesInCurrentFolder = new List<FileObject>();
                
                foreach (var file in filteredFiles)
                {
                    string relativePath = file.Name;
                    
                    // Skip files that don't match the current folder
                    if (!string.IsNullOrEmpty(currentFolder) && !relativePath.StartsWith(currentFolder + "/"))
                    {
                        continue;
                    }
                    
                    // Extract the relative path from the current folder
                    if (!string.IsNullOrEmpty(currentFolder) && relativePath.StartsWith(currentFolder + "/"))
                    {
                        relativePath = relativePath.Substring(currentFolder.Length + 1);
                    }
                    
                    // Check if this is a file in a subfolder
                    int slashIndex = relativePath.IndexOf('/');
                    if (slashIndex >= 0)
                    {
                        // This is a file in a subfolder, extract the folder name
                        string folderName = relativePath.Substring(0, slashIndex);
                        folders.Add(folderName);
                    }
                    else
                    {
                        // This is a file in the current folder
                        filesInCurrentFolder.Add(file);
                    }
                }
                
                // Display the file count
                EditorGUILayout.LabelField($"Files: {filesInCurrentFolder.Count}, Folders: {folders.Count}");
                
                // Start the scroll view
                filesScrollPosition = EditorGUILayout.BeginScrollView(filesScrollPosition, GUILayout.Height(200));
                
                // Display folders first
                if (folders.Count > 0)
                {
                    EditorGUILayout.LabelField("Folders:", EditorStyles.boldLabel);
                    
                    if (currentViewMode == FileViewMode.List)
                    {
                        // List view for folders
                        foreach (var folder in folders.OrderBy(f => f))
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            // Folder icon
                            GUILayout.Label("📁", GUILayout.Width(20));
                            
                            // Folder name as a button
                            if (GUILayout.Button(folder, EditorStyles.label))
                            {
                                // Navigate into this folder
                                currentFolder = string.IsNullOrEmpty(currentFolder) ? 
                                    folder : 
                                    Path.Combine(currentFolder, folder).Replace("\\", "/");
                                    
                                ListFilesAsync(selectedBucket, currentFolder);
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        // Grid view for folders
                        int gridColumns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 50) / 100));
                        int folderIndex = 0;
                        
                        foreach (var folder in folders.OrderBy(f => f))
                        {
                            if (folderIndex % gridColumns == 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                            }
                            
                            // Folder as a button with icon
                            if (GUILayout.Button(new GUIContent($"📁 {folder}"), GUILayout.Width(100), GUILayout.Height(80)))
                            {
                                // Navigate into this folder
                                currentFolder = string.IsNullOrEmpty(currentFolder) ? 
                                    folder : 
                                    Path.Combine(currentFolder, folder).Replace("\\", "/");
                                    
                                ListFilesAsync(selectedBucket, currentFolder);
                            }
                            
                            folderIndex++;
                            
                            if (folderIndex % gridColumns == 0 || folderIndex == folders.Count)
                            {
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        
                        // End the last row if needed
                        if (folderIndex % gridColumns != 0)
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    EditorGUILayout.Space(10);
                }
                
                // Display files
                if (filesInCurrentFolder.Count > 0)
                {
                    EditorGUILayout.LabelField("Files:", EditorStyles.boldLabel);
                    
                    if (currentViewMode == FileViewMode.List)
                    {
                        // List view for files
                        foreach (var file in filesInCurrentFolder)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            bool isSelected = selectedFile == file.Name;
                            bool newIsSelected = EditorGUILayout.ToggleLeft(GetFileNameFromPath(file.Name), isSelected);
                            
                            if (newIsSelected != isSelected)
                            {
                                selectedFile = newIsSelected ? file.Name : "";
                                
                                if (newIsSelected && showPreview)
                                {
                                    LoadFilePreview(selectedBucket, selectedFile);
                                }
                            }
                            
                            EditorGUILayout.LabelField(file.ContentType, GUILayout.Width(100));
                            EditorGUILayout.LabelField(FormatFileSize(file.Size), GUILayout.Width(80));
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        // Grid view for files
                        int gridColumns = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 50) / 100));
                        int fileIndex = 0;
                        
                        foreach (var file in filesInCurrentFolder)
                        {
                            if (fileIndex % gridColumns == 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                            }
                            
                            // File as a button with icon
                            string fileName = GetFileNameFromPath(file.Name);
                            string fileIcon = GetFileIcon(file.ContentType);
                            bool isSelected = selectedFile == file.Name;
                            
                            GUIStyle fileStyle = isSelected ? 
                                SupabaseEditorStyles.SelectedListItemStyle : 
                                SupabaseEditorStyles.ListItemStyle;
                                
                            if (GUILayout.Button(new GUIContent($"{fileIcon} {fileName}"), fileStyle, GUILayout.Width(100), GUILayout.Height(80)))
                            {
                                selectedFile = isSelected ? "" : file.Name;
                                
                                if (!isSelected && showPreview)
                                {
                                    LoadFilePreview(selectedBucket, file.Name);
                                }
                            }
                            
                            fileIndex++;
                            
                            if (fileIndex % gridColumns == 0 || fileIndex == filesInCurrentFolder.Count)
                            {
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        
                        // End the last row if needed
                        if (fileIndex % gridColumns != 0)
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space(10);
                
                // File operations
                if (!string.IsNullOrEmpty(selectedFile))
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    GUI.enabled = !isGettingFileUrl && !isDeletingFile;
                    
                    if (GUILayout.Button("Get File URL", SupabaseEditorStyles.ButtonStyle))
                    {
                        GetFileUrlAsync(selectedBucket, selectedFile);
                    }
                    
                    if (GUILayout.Button("Download", SupabaseEditorStyles.ButtonStyle))
                    {
                        DownloadFileAsync(selectedBucket, selectedFile);
                    }
                    
                    if (GUILayout.Button("Delete", SupabaseEditorStyles.ButtonStyle))
                    {
                        if (EditorUtility.DisplayDialog("Delete File", 
                            $"Are you sure you want to delete the file '{GetFileNameFromPath(selectedFile)}'?", 
                            "Yes", "No"))
                        {
                            DeleteFileAsync(selectedBucket, selectedFile);
                        }
                    }
                    
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Display file URL if available
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        EditorGUILayout.Space(5);
                        
                        EditorGUILayout.LabelField("File URL:", EditorStyles.boldLabel);
                        EditorGUILayout.SelectableLabel(fileUrl, EditorStyles.textField, GUILayout.Height(40));
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        if (GUILayout.Button("Copy URL", SupabaseEditorStyles.ButtonStyle))
                        {
                            EditorGUIUtility.systemCopyBuffer = fileUrl;
                            successMessage = "URL copied to clipboard!";
                        }
                        
                        if (GUILayout.Button("Open in Browser", SupabaseEditorStyles.ButtonStyle))
                        {
                            Application.OpenURL(fileUrl);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else if (isListingFiles)
            {
                EditorGUILayout.LabelField("Loading files...");
            }
            else
            {
                EditorGUILayout.LabelField("No files found in this bucket.");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the file preview section.
        /// </summary>
        private void DrawFilePreviewSection()
        {
            SupabaseEditorStyles.DrawSubHeader("File Preview");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Display the file name
            EditorGUILayout.LabelField(GetFileNameFromPath(selectedFile), EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            
            // Display the preview based on file type
            if (previewTexture != null)
            {
                // Image preview
                float maxPreviewWidth = EditorGUIUtility.currentViewWidth - 40;
                float maxPreviewHeight = 300;
                
                float aspectRatio = (float)previewTexture.width / previewTexture.height;
                float previewWidth = Mathf.Min(maxPreviewWidth, previewTexture.width);
                float previewHeight = previewWidth / aspectRatio;
                
                if (previewHeight > maxPreviewHeight)
                {
                    previewHeight = maxPreviewHeight;
                    previewWidth = previewHeight * aspectRatio;
                }
                
                Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
            }
            else if (!string.IsNullOrEmpty(previewText))
            {
                // Text preview
                previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, GUILayout.Height(200));
                EditorGUILayout.TextArea(previewText, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("No preview available for this file type.");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Formats a file size in bytes to a human-readable string.
        /// </summary>
        /// <param name="bytes">The size in bytes</param>
        /// <returns>A human-readable size string</returns>
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:0.##} {suffixes[suffixIndex]}";
        }
        
        /// <summary>
        /// Gets the file name from a path.
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The file name</returns>
        private string GetFileNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
                
            int lastSlash = path.LastIndexOf('/');
            return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        }
        
        /// <summary>
        /// Gets an icon for a file based on its content type.
        /// </summary>
        /// <param name="contentType">The content type</param>
        /// <returns>An icon string</returns>
        private string GetFileIcon(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return "📄";
                
            if (contentType.StartsWith("image/"))
                return "🖼️";
                
            if (contentType.StartsWith("text/"))
                return "📝";
                
            if (contentType.StartsWith("audio/"))
                return "🔊";
                
            if (contentType.StartsWith("video/"))
                return "🎬";
                
            if (contentType.Contains("pdf"))
                return "📑";
                
            if (contentType.Contains("zip") || contentType.Contains("compressed"))
                return "📦";
                
            if (contentType.Contains("json") || contentType.Contains("xml"))
                return "📊";
                
            return "📄";
        }
        
        /// <summary>
        /// Handles drag and drop operations for file uploads.
        /// </summary>
        private void HandleDragAndDrop()
        {
            // Check if we have a selected bucket
            if (string.IsNullOrEmpty(selectedBucket))
                return;
                
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetLastRect();
            
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;
                        
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        
                        foreach (string path in DragAndDrop.paths)
                        {
                            if (File.Exists(path))
                            {
                                // Get the file name
                                string fileName = Path.GetFileName(path);
                                
                                // Create the storage path
                                string storagePath = string.IsNullOrEmpty(currentFolder) ? 
                                    fileName : 
                                    Path.Combine(currentFolder, fileName).Replace("\\", "/");
                                    
                                // Upload the file
                                UploadFileAsync(selectedBucket, path, storagePath);
                            }
                        }
                    }
                    
                    evt.Use();
                    break;
            }
        }
        
        /// <summary>
        /// Loads a preview for a file.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The path of the file in the bucket</param>
        private async void LoadFilePreview(string bucketName, string filePath)
        {
            try
            {
                // Clear existing preview
                previewTexture = null;
                previewText = "";
                
                // Get the file URL
                string url = await storageService.GetFileUrl(bucketName, filePath);
                
                // Get the file content type
                string contentType = files.FirstOrDefault(f => f.Name == filePath)?.ContentType ?? "";
                
                // Handle different file types
                if (contentType.StartsWith("image/"))
                {
                    // Load image preview
                    using (var www = new UnityEngine.Networking.UnityWebRequest(url))
                    {
                        www.downloadHandler = new UnityEngine.Networking.DownloadHandlerTexture(true);
                        await www.SendWebRequest();
                        
                        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        {
                            previewTexture = ((UnityEngine.Networking.DownloadHandlerTexture)www.downloadHandler).texture;
                        }
                        else
                        {
                            Debug.LogError($"Failed to load image preview: {www.error}");
                        }
                    }
                }
                else if (contentType.StartsWith("text/") || 
                         contentType.Contains("json") || 
                         contentType.Contains("xml") ||
                         contentType.Contains("javascript") ||
                         contentType.Contains("css"))
                {
                    // Load text preview
                    using (var www = new UnityEngine.Networking.UnityWebRequest(url))
                    {
                        www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                        await www.SendWebRequest();
                        
                        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        {
                            previewText = www.downloadHandler.text;
                            
                            // Limit preview size
                            if (previewText.Length > 10000)
                            {
                                previewText = previewText.Substring(0, 10000) + "...\n\n[Text preview truncated due to size]";
                            }
                        }
                        else
                        {
                            Debug.LogError($"Failed to load text preview: {www.error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load file preview: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Downloads a file asynchronously.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The path of the file in the bucket</param>
        private async void DownloadFileAsync(string bucketName, string filePath)
        {
            try
            {
                // Get the file URL
                string url = await storageService.GetFileUrl(bucketName, filePath);
                
                // Get the file name
                string fileName = GetFileNameFromPath(filePath);
                
                // Show save file dialog
                string savePath = EditorUtility.SaveFilePanel(
                    "Save File",
                    "",
                    fileName,
                    ""
                );
                
                if (string.IsNullOrEmpty(savePath))
                    return;
                    
                // Download the file
                using (var www = new UnityEngine.Networking.UnityWebRequest(url))
                {
                    www.downloadHandler = new UnityEngine.Networking.DownloadHandlerFile(savePath);
                    await www.SendWebRequest();
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        successMessage = $"File '{fileName}' downloaded successfully!";
                    }
                    else
                    {
                        errorMessage = $"Failed to download file: {www.error}";
                        Debug.LogError(errorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to download file: {ex.Message}";
                Debug.LogError(errorMessage);
            }
        }
        
        #region Async Operations
        
        /// <summary>
        /// Checks if a bucket exists asynchronously.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        private async void CheckBucketExistsAsync(string bucketName)
        {
            try
            {
                isCheckingBucket = true;
                errorMessage = "";
                successMessage = "";
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
                
                // Check if the bucket exists
                bucketExists = await storageService.BucketExists(bucketName);
                
                successMessage = bucketExists.Value
                    ? $"Bucket '{bucketName}' exists."
                    : $"Bucket '{bucketName}' does not exist.";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to check if bucket exists: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isCheckingBucket = false;
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
            }
        }
        
        /// <summary>
        /// Creates a bucket asynchronously.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="isPublic">Whether the bucket should be public</param>
        private async void CreateBucketAsync(string bucketName, bool isPublic)
        {
            try
            {
                isCreatingBucket = true;
                errorMessage = "";
                successMessage = "";
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
                
                // Create the bucket
                BucketInfo bucket = await storageService.CreateBucket(bucketName, isPublic);
                
                // Update the bucket list
                if (!buckets.Exists(b => b.Name == bucket.Name))
                {
                    buckets.Add(bucket);
                }
                
                // Update the bucket existence status
                bucketExists = true;
                
                successMessage = $"Bucket '{bucketName}' created successfully!";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to create bucket: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isCreatingBucket = false;
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
            }
        }
        
        /// <summary>
        /// Lists all buckets asynchronously.
        /// </summary>
        private async void ListBucketsAsync()
        {
            try
            {
                errorMessage = "";
                successMessage = "";
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
                
                // List the buckets
                buckets = await storageService.ListBuckets();
                
                successMessage = $"Found {buckets.Count} bucket(s).";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to list buckets: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
            }
        }
        
        /// <summary>
        /// Uploads a file asynchronously.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The local path of the file</param>
        /// <param name="storageFilePath">The path where the file should be stored</param>
        private async void UploadFileAsync(string bucketName, string filePath, string storageFilePath)
        {
            try
            {
                isUploadingFile = true;
                uploadProgress = 0f;
                errorMessage = "";
                successMessage = "";
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
                
                // Start a progress tracking coroutine
                EditorApplication.update += UpdateUploadProgress;
                
                // Upload the file
                FileObject file = await storageService.UploadFile(bucketName, filePath, storageFilePath);
                
                // Update the file list
                if (!files.Exists(f => f.Name == file.Name))
                {
                    files.Add(file);
                }
                else
                {
                    // Update the existing file entry
                    int index = files.FindIndex(f => f.Name == file.Name);
                    if (index >= 0)
                    {
                        files[index] = file;
                    }
                }
                
                successMessage = $"File '{Path.GetFileName(filePath)}' uploaded successfully!";
                
                // Clear the file path fields
                this.filePath = "";
                this.storageFilePath = "";
                
                // Refresh the file list
                await Task.Delay(500); // Small delay to ensure the file is available
                ListFilesAsync(bucketName, currentFolder);
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to upload file: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isUploadingFile = false;
                uploadProgress = 0f;
                EditorApplication.update -= UpdateUploadProgress;
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
            }
        }
        
        /// <summary>
        /// Updates the upload progress for UI display.
        /// </summary>
        private void UpdateUploadProgress()
        {
            if (isUploadingFile)
            {
                // Simulate progress for now
                // In a real implementation, we would get progress from the upload operation
                uploadProgress = Mathf.Min(0.99f, uploadProgress + 0.01f);
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
            }
        }
        
        /// <summary>
        /// Lists files in a bucket asynchronously.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="folderPath">Optional folder path to filter files</param>
        private async void ListFilesAsync(string bucketName, string folderPath = "")
        {
            try
            {
                isListingFiles = true;
                errorMessage = "";
                successMessage = "";
                selectedFile = "";
                previewTexture = null;
                previewText = "";
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
                
                // Update the current folder
                currentFolder = folderPath ?? "";
                
                // List the files
                files = await storageService.ListFiles(bucketName, currentFolder);
                
                // Count actual files (not folders)
                int fileCount = 0;
                HashSet<string> folders = new HashSet<string>();
                
                foreach (var file in files)
                {
                    string relativePath = file.Name;
                    
                    // Skip files that don't match the current folder
                    if (!string.IsNullOrEmpty(currentFolder) && !relativePath.StartsWith(currentFolder + "/"))
                    {
                        continue;
                    }
                    
                    // Extract the relative path from the current folder
                    if (!string.IsNullOrEmpty(currentFolder) && relativePath.StartsWith(currentFolder + "/"))
                    {
                        relativePath = relativePath.Substring(currentFolder.Length + 1);
                    }
                    
                    // Check if this is a file in a subfolder
                    int slashIndex = relativePath.IndexOf('/');
                    if (slashIndex >= 0)
                    {
                        // This is a file in a subfolder, extract the folder name
                        string folderName = relativePath.Substring(0, slashIndex);
                        folders.Add(folderName);
                    }
                    else
                    {
                        // This is a file in the current folder
                        fileCount++;
                    }
                }
                
                successMessage = $"Found {fileCount} file(s) and {folders.Count} folder(s) in bucket '{bucketName}'{(string.IsNullOrEmpty(currentFolder) ? "" : $"/{currentFolder}")}.";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to list files: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isListingFiles = false;
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
            }
        }
        
        /// <summary>
        /// Gets a file URL asynchronously.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The path of the file in the bucket</param>
        private async void GetFileUrlAsync(string bucketName, string filePath)
        {
            try
            {
                isGettingFileUrl = true;
                errorMessage = "";
                successMessage = "";
                fileUrl = "";
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
                
                // Get the file URL
                fileUrl = await storageService.GetFileUrl(bucketName, filePath);
                
                successMessage = "File URL generated successfully!";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to get file URL: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isGettingFileUrl = false;
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
            }
        }
        
        /// <summary>
        /// Deletes a file asynchronously.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The path of the file in the bucket</param>
        private async void DeleteFileAsync(string bucketName, string filePath)
        {
            try
            {
                isDeletingFile = true;
                errorMessage = "";
                successMessage = "";
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
                
                // Delete the file
                await storageService.DeleteFile(bucketName, filePath);
                
                // Remove the file from the list
                files.RemoveAll(f => f.Name == filePath);
                
                // Clear the selected file
                if (selectedFile == filePath)
                {
                    selectedFile = "";
                    fileUrl = "";
                }
                
                successMessage = $"File '{filePath}' deleted successfully!";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to delete file: {ex.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                isDeletingFile = false;
                
                // Force a repaint to update the UI
                EditorUtility.SetDirty(SupabaseEditorWindow.GetWindow<SupabaseEditorWindow>());
            }
        }
        
        #endregion
    }
}