using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Service for managing Supabase storage operations.
    /// Provides methods for bucket management and file operations.
    /// </summary>
    public class SupabaseStorageService
    {
        private readonly SupabaseClient client;
        
        // Storage endpoints
        private const string STORAGE_ENDPOINT = "/storage/v1";
        private const string BUCKETS_ENDPOINT = "/storage/v1/bucket";
        private const string OBJECTS_ENDPOINT = "/storage/v1/object";
        
        /// <summary>
        /// Initializes a new instance of the SupabaseStorageService class.
        /// </summary>
        /// <param name="client">The Supabase client to use for API requests</param>
        public SupabaseStorageService(SupabaseClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }
        
        /// <summary>
        /// Checks if a bucket exists.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <returns>True if the bucket exists, false otherwise</returns>
        public async Task<bool> BucketExists(string bucketName)
        {
            try
            {
                if (string.IsNullOrEmpty(bucketName))
                    throw new ArgumentException("Bucket name is required", nameof(bucketName));
                
                // Send the request to get the bucket
                await client.Get($"{BUCKETS_ENDPOINT}/{bucketName}");
                
                // If no exception was thrown, the bucket exists
                return true;
            }
            catch (SupabaseException ex)
            {
                // If the bucket doesn't exist, we'll get a 404 error
                if (ex.StatusCode == 404)
                {
                    return false;
                }
                
                // For other errors, rethrow
                Debug.LogError($"Error checking if bucket exists: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error checking if bucket exists: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Creates a new bucket.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="isPublic">Whether the bucket should be public</param>
        /// <returns>The created bucket information</returns>
        public async Task<BucketInfo> CreateBucket(string bucketName, bool isPublic = false)
        {
            try
            {
                if (string.IsNullOrEmpty(bucketName))
                    throw new ArgumentException("Bucket name is required", nameof(bucketName));
                
                // Create the request body
                var requestData = new Dictionary<string, object>
                {
                    { "name", bucketName },
                    { "public", isPublic }
                };
                
                // Convert to JSON
                string jsonBody = SupabaseJsonUtility.ToJson(requestData);
                
                // Send the request
                string response = await client.Post(BUCKETS_ENDPOINT, jsonBody);
                
                // Parse the response
                return ParseBucketResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating bucket: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Lists all buckets.
        /// </summary>
        /// <returns>A list of bucket information</returns>
        public async Task<List<BucketInfo>> ListBuckets()
        {
            try
            {
                // Send the request
                string response = await client.Get(BUCKETS_ENDPOINT);
                
                // Parse the response
                return ParseBucketsResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error listing buckets: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Uploads a file to a bucket.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The local path of the file to upload</param>
        /// <param name="storageFilePath">The path where the file should be stored in the bucket</param>
        /// <returns>The uploaded file information</returns>
        public async Task<FileObject> UploadFile(string bucketName, string filePath, string storageFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(bucketName))
                    throw new ArgumentException("Bucket name is required", nameof(bucketName));
                
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path is required", nameof(filePath));
                
                if (string.IsNullOrEmpty(storageFilePath))
                    throw new ArgumentException("Storage file path is required", nameof(storageFilePath));
                
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("File not found", filePath);
                
                // Read the file
                byte[] fileBytes = File.ReadAllBytes(filePath);
                
                // Get the file name from the path
                string fileName = Path.GetFileName(filePath);
                
                // Create a multipart form data content
                var formData = new MultipartFormDataContent();
                
                // Add the file content
                var fileContent = new ByteArrayContent(fileBytes);
                
                // Determine content type based on file extension
                string contentType = GetContentType(fileName);
                if (!string.IsNullOrEmpty(contentType))
                {
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                }
                
                formData.Add(fileContent, "file", fileName);
                
                // Send the request
                string response = await client.SendMultipartFormData(
                    $"{OBJECTS_ENDPOINT}/{bucketName}/{storageFilePath}",
                    formData,
                    HttpMethod.Post
                );
                
                // Parse the response
                return ParseFileResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error uploading file: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Lists files in a bucket.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="path">Optional path prefix to filter files</param>
        /// <param name="limit">Optional limit on the number of files to return</param>
        /// <param name="offset">Optional offset for pagination</param>
        /// <returns>A list of file information</returns>
        public async Task<List<FileObject>> ListFiles(string bucketName, string path = "", int? limit = null, int? offset = null)
        {
            try
            {
                if (string.IsNullOrEmpty(bucketName))
                    throw new ArgumentException("Bucket name is required", nameof(bucketName));
                
                // Build query parameters
                var queryParams = new Dictionary<string, string>();
                
                if (!string.IsNullOrEmpty(path))
                {
                    queryParams["prefix"] = path;
                }
                
                if (limit.HasValue)
                {
                    queryParams["limit"] = limit.Value.ToString();
                }
                
                if (offset.HasValue)
                {
                    queryParams["offset"] = offset.Value.ToString();
                }
                
                // Send the request
                string response = await client.Get($"{OBJECTS_ENDPOINT}/list/{bucketName}", queryParams);
                
                // Parse the response
                return ParseFilesResponse(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error listing files: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets a public URL for a file.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The path of the file in the bucket</param>
        /// <param name="expiresIn">Optional expiration time in seconds (for signed URLs)</param>
        /// <returns>The public URL of the file</returns>
        public async Task<string> GetFileUrl(string bucketName, string filePath, int? expiresIn = null)
        {
            try
            {
                if (string.IsNullOrEmpty(bucketName))
                    throw new ArgumentException("Bucket name is required", nameof(bucketName));
                
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path is required", nameof(filePath));
                
                // Check if the bucket is public
                bool isPublic = await IsBucketPublic(bucketName);
                
                if (isPublic)
                {
                    // For public buckets, we can just return the direct URL
                    return $"{client.GetBaseUrl()}{STORAGE_ENDPOINT}/public/{bucketName}/{filePath}";
                }
                else
                {
                    // For private buckets, we need to get a signed URL
                    var queryParams = new Dictionary<string, string>();
                    
                    if (expiresIn.HasValue)
                    {
                        queryParams["expiresIn"] = expiresIn.Value.ToString();
                    }
                    
                    string response = await client.Post(
                        $"{OBJECTS_ENDPOINT}/sign/{bucketName}/{filePath}",
                        "{}",
                        queryParams
                    );
                    
                    // Parse the response to get the signed URL
                    string signedUrl = SupabaseResponseParser.ExtractJsonProperty(response, "signedURL");
                    
                    if (string.IsNullOrEmpty(signedUrl))
                    {
                        throw new SupabaseException("Failed to get signed URL", 0, "SIGNED_URL_ERROR");
                    }
                    
                    return signedUrl;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting file URL: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Deletes a file from a bucket.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The path of the file in the bucket</param>
        /// <returns>A task that completes when the file is deleted</returns>
        public async Task DeleteFile(string bucketName, string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(bucketName))
                    throw new ArgumentException("Bucket name is required", nameof(bucketName));
                
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path is required", nameof(filePath));
                
                // Send the request
                await client.Delete($"{OBJECTS_ENDPOINT}/{bucketName}/{filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deleting file: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the content type based on file extension.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>The content type string</returns>
        private string GetContentType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";
            
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".webp":
                    return "image/webp";
                case ".pdf":
                    return "application/pdf";
                case ".txt":
                    return "text/plain";
                case ".html":
                case ".htm":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
                case ".json":
                    return "application/json";
                case ".xml":
                    return "application/xml";
                case ".zip":
                    return "application/zip";
                case ".mp4":
                    return "video/mp4";
                case ".mp3":
                    return "audio/mpeg";
                case ".wav":
                    return "audio/wav";
                default:
                    return "application/octet-stream";
            }
        }
        
        /// <summary>
        /// Checks if a bucket is public.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <returns>True if the bucket is public, false otherwise</returns>
        private async Task<bool> IsBucketPublic(string bucketName)
        {
            try
            {
                // Get the bucket information
                string response = await client.Get($"{BUCKETS_ENDPOINT}/{bucketName}");
                
                // Parse the response to check if the bucket is public
                string publicStr = SupabaseResponseParser.ExtractJsonProperty(response, "public");
                
                return publicStr != null && publicStr.ToLower() == "true";
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error checking if bucket is public: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Parses a bucket response from the Supabase API.
        /// </summary>
        /// <param name="json">The JSON response</param>
        /// <returns>A BucketInfo object</returns>
        private BucketInfo ParseBucketResponse(string json)
        {
            try
            {
                // Extract the bucket information
                string name = SupabaseResponseParser.ExtractJsonProperty(json, "name");
                string publicStr = SupabaseResponseParser.ExtractJsonProperty(json, "public");
                string createdAtStr = SupabaseResponseParser.ExtractJsonProperty(json, "created_at");
                
                bool isPublic = publicStr != null && publicStr.ToLower() == "true";
                DateTime createdAt = DateTime.Parse(createdAtStr ?? DateTime.UtcNow.ToString());
                
                return new BucketInfo
                {
                    Name = name,
                    IsPublic = isPublic,
                    CreatedAt = createdAt
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse bucket response: {ex.Message}");
                throw new SupabaseException("Failed to parse bucket response", 0, "BUCKET_PARSE_ERROR", ex);
            }
        }
        
        /// <summary>
        /// Parses a buckets list response from the Supabase API.
        /// </summary>
        /// <param name="json">The JSON response</param>
        /// <returns>A list of BucketInfo objects</returns>
        private List<BucketInfo> ParseBucketsResponse(string json)
        {
            try
            {
                var buckets = new List<BucketInfo>();
                
                // Extract array of bucket objects from JSON
                var bucketObjects = SupabaseResponseParser.ExtractJsonArray(json);
                
                foreach (string bucketJson in bucketObjects)
                {
                    string name = SupabaseResponseParser.ExtractJsonProperty(bucketJson, "name");
                    string publicStr = SupabaseResponseParser.ExtractJsonProperty(bucketJson, "public");
                    string createdAtStr = SupabaseResponseParser.ExtractJsonProperty(bucketJson, "created_at");
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        bool isPublic = publicStr != null && publicStr.ToLower() == "true";
                        DateTime createdAt = DateTime.UtcNow;
                        
                        if (!string.IsNullOrEmpty(createdAtStr))
                        {
                            DateTime.TryParse(createdAtStr, out createdAt);
                        }
                        
                        buckets.Add(new BucketInfo
                        {
                            Name = name,
                            IsPublic = isPublic,
                            CreatedAt = createdAt
                        });
                    }
                }
                
                return buckets;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse buckets response: {ex.Message}");
                throw new SupabaseException("Failed to parse buckets response", 0, "BUCKETS_PARSE_ERROR", ex);
            }
        }
        
        /// <summary>
        /// Parses a file response from the Supabase API.
        /// </summary>
        /// <param name="json">The JSON response</param>
        /// <returns>A FileObject</returns>
        private FileObject ParseFileResponse(string json)
        {
            try
            {
                // Extract the file information
                string name = SupabaseResponseParser.ExtractJsonProperty(json, "name");
                string bucketName = SupabaseResponseParser.ExtractJsonProperty(json, "bucket_id");
                string contentType = SupabaseResponseParser.ExtractJsonProperty(json, "mime_type");
                string sizeStr = SupabaseResponseParser.ExtractJsonProperty(json, "size");
                string createdAtStr = SupabaseResponseParser.ExtractJsonProperty(json, "created_at");
                string updatedAtStr = SupabaseResponseParser.ExtractJsonProperty(json, "updated_at");
                
                long size = 0;
                if (!string.IsNullOrEmpty(sizeStr))
                {
                    long.TryParse(sizeStr, out size);
                }
                
                DateTime createdAt = DateTime.Parse(createdAtStr ?? DateTime.UtcNow.ToString());
                DateTime updatedAt = DateTime.Parse(updatedAtStr ?? DateTime.UtcNow.ToString());
                
                return new FileObject
                {
                    Name = name,
                    BucketName = bucketName,
                    ContentType = contentType,
                    Size = size,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse file response: {ex.Message}");
                throw new SupabaseException("Failed to parse file response", 0, "FILE_PARSE_ERROR", ex);
            }
        }
        
        /// <summary>
        /// Parses a files list response from the Supabase API.
        /// </summary>
        /// <param name="json">The JSON response</param>
        /// <returns>A list of FileObject objects</returns>
        private List<FileObject> ParseFilesResponse(string json)
        {
            try
            {
                var files = new List<FileObject>();
                
                // Extract array of file objects from JSON
                var fileObjects = SupabaseResponseParser.ExtractJsonArray(json);
                
                foreach (string fileJson in fileObjects)
                {
                    string name = SupabaseResponseParser.ExtractJsonProperty(fileJson, "name");
                    string bucketName = SupabaseResponseParser.ExtractJsonProperty(fileJson, "bucket_id");
                    string contentType = SupabaseResponseParser.ExtractJsonProperty(fileJson, "mime_type");
                    string sizeStr = SupabaseResponseParser.ExtractJsonProperty(fileJson, "size");
                    string createdAtStr = SupabaseResponseParser.ExtractJsonProperty(fileJson, "created_at");
                    string updatedAtStr = SupabaseResponseParser.ExtractJsonProperty(fileJson, "updated_at");
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        long size = 0;
                        if (!string.IsNullOrEmpty(sizeStr))
                        {
                            long.TryParse(sizeStr, out size);
                        }
                        
                        DateTime createdAt = DateTime.UtcNow;
                        DateTime updatedAt = DateTime.UtcNow;
                        
                        if (!string.IsNullOrEmpty(createdAtStr))
                        {
                            DateTime.TryParse(createdAtStr, out createdAt);
                        }
                        
                        if (!string.IsNullOrEmpty(updatedAtStr))
                        {
                            DateTime.TryParse(updatedAtStr, out updatedAt);
                        }
                        
                        files.Add(new FileObject
                        {
                            Name = name,
                            BucketName = bucketName ?? "",
                            ContentType = contentType ?? "",
                            Size = size,
                            CreatedAt = createdAt,
                            UpdatedAt = updatedAt
                        });
                    }
                }
                
                return files;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse files response: {ex.Message}");
                throw new SupabaseException("Failed to parse files response", 0, "FILES_PARSE_ERROR", ex);
            }
        }
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
}