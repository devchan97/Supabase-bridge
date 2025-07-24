using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SupabaseBridge.Runtime
{
    /// <summary>
    /// Service for managing Supabase storage operations at runtime.
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
                string jsonBody = JsonUtility.ToJson(new DictionaryWrapper(requestData));
                
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
        /// <param name="fileData">The file data to upload</param>
        /// <param name="fileName">The name of the file</param>
        /// <param name="storageFilePath">The path where the file should be stored in the bucket</param>
        /// <param name="contentType">The content type of the file</param>
        /// <returns>The uploaded file information</returns>
        public async Task<FileObject> UploadFile(string bucketName, byte[] fileData, string fileName, string storageFilePath, string contentType = null)
        {
            try
            {
                if (string.IsNullOrEmpty(bucketName))
                    throw new ArgumentException("Bucket name is required", nameof(bucketName));
                
                if (fileData == null || fileData.Length == 0)
                    throw new ArgumentException("File data is required", nameof(fileData));
                
                if (string.IsNullOrEmpty(fileName))
                    throw new ArgumentException("File name is required", nameof(fileName));
                
                if (string.IsNullOrEmpty(storageFilePath))
                    throw new ArgumentException("Storage file path is required", nameof(storageFilePath));
                
                // Determine content type if not provided
                if (string.IsNullOrEmpty(contentType))
                {
                    contentType = GetContentType(fileName);
                }
                
                // Send the request
                string response = await client.UploadFile(
                    $"{OBJECTS_ENDPOINT}/{bucketName}/{storageFilePath}",
                    fileData,
                    fileName,
                    contentType
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
                    var responseObj = JsonUtility.FromJson<SignedUrlResponse>(response);
                    
                    if (string.IsNullOrEmpty(responseObj.signedURL))
                    {
                        throw new SupabaseException("Failed to get signed URL", 0, "SIGNED_URL_ERROR");
                    }
                    
                    return responseObj.signedURL;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting file URL: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Downloads a file from a bucket.
        /// </summary>
        /// <param name="bucketName">The name of the bucket</param>
        /// <param name="filePath">The path of the file in the bucket</param>
        /// <returns>The file data</returns>
        public async Task<byte[]> DownloadFile(string bucketName, string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(bucketName))
                    throw new ArgumentException("Bucket name is required", nameof(bucketName));
                
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path is required", nameof(filePath));
                
                // Get the file URL
                string fileUrl = await GetFileUrl(bucketName, filePath);
                
                // Download the file
                return await client.DownloadFile(fileUrl);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error downloading file: {ex.Message}");
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
                var bucketInfo = ParseBucketResponse(response);
                
                return bucketInfo.IsPublic;
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
                // Parse the JSON response
                var bucketResponse = JsonUtility.FromJson<BucketResponse>(json);
                
                return new BucketInfo
                {
                    Name = bucketResponse.name,
                    IsPublic = bucketResponse.public_bucket,
                    CreatedAt = DateTime.Parse(bucketResponse.created_at)
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
                // For simplicity, we'll use a basic parsing approach
                var buckets = new List<BucketInfo>();
                
                // Check if the response is an array
                if (json.StartsWith("[") && json.EndsWith("]"))
                {
                    // Remove the outer brackets
                    json = json.Substring(1, json.Length - 2);
                    
                    // Split the array into objects
                    List<string> objectStrings = SplitJsonArray(json);
                    
                    // Parse each object
                    foreach (var objStr in objectStrings)
                    {
                        try
                        {
                            var bucketResponse = JsonUtility.FromJson<BucketResponse>("{" + objStr + "}");
                            
                            buckets.Add(new BucketInfo
                            {
                                Name = bucketResponse.name,
                                IsPublic = bucketResponse.public_bucket,
                                CreatedAt = DateTime.Parse(bucketResponse.created_at)
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to parse bucket: {ex.Message}");
                        }
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
                // Parse the JSON response
                var fileResponse = JsonUtility.FromJson<FileResponse>(json);
                
                return new FileObject
                {
                    Name = fileResponse.name,
                    BucketName = fileResponse.bucket_id,
                    ContentType = fileResponse.mime_type,
                    Size = fileResponse.size,
                    CreatedAt = DateTime.Parse(fileResponse.created_at),
                    UpdatedAt = DateTime.Parse(fileResponse.updated_at)
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
                // For simplicity, we'll use a basic parsing approach
                var files = new List<FileObject>();
                
                // Check if the response is an array
                if (json.StartsWith("[") && json.EndsWith("]"))
                {
                    // Remove the outer brackets
                    json = json.Substring(1, json.Length - 2);
                    
                    // Split the array into objects
                    List<string> objectStrings = SplitJsonArray(json);
                    
                    // Parse each object
                    foreach (var objStr in objectStrings)
                    {
                        try
                        {
                            var fileResponse = JsonUtility.FromJson<FileResponse>("{" + objStr + "}");
                            
                            files.Add(new FileObject
                            {
                                Name = fileResponse.name,
                                BucketName = fileResponse.bucket_id,
                                ContentType = fileResponse.mime_type,
                                Size = fileResponse.size,
                                CreatedAt = DateTime.Parse(fileResponse.created_at),
                                UpdatedAt = DateTime.Parse(fileResponse.updated_at)
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to parse file: {ex.Message}");
                        }
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
        
        /// <summary>
        /// Splits a JSON array string into individual object strings.
        /// </summary>
        /// <param name="json">The JSON array string</param>
        /// <returns>A list of JSON object strings</returns>
        private List<string> SplitJsonArray(string json)
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
                    if (braceCount == 1)
                    {
                        startIndex = i + 1; // Start after the opening brace
                    }
                }
                else if (c == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        // Extract the object string (without outer braces)
                        string objStr = json.Substring(startIndex, i - startIndex);
                        result.Add(objStr);
                    }
                }
                else if (c == '[')
                {
                    bracketCount++;
                }
                else if (c == ']')
                {
                    bracketCount--;
                }
            }
            
            return result;
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
                    else if (kvp.Value is bool)
                    {
                        sb.Append(kvp.Value.ToString().ToLower());
                    }
                    else
                    {
                        sb.Append(kvp.Value.ToString());
                    }
                    
                    first = false;
                }
                
                sb.Append("}");
                data = sb.ToString();
            }
        }
        
        /// <summary>
        /// Response class for bucket information.
        /// </summary>
        [Serializable]
        private class BucketResponse
        {
            public string id;
            public string name;
            public string owner;
            public bool public_bucket;
            public string created_at;
            public string updated_at;
        }
        
        /// <summary>
        /// Response class for file information.
        /// </summary>
        [Serializable]
        private class FileResponse
        {
            public string name;
            public string id;
            public string bucket_id;
            public string owner;
            public string mime_type;
            public long size;
            public string created_at;
            public string updated_at;
        }
        
        /// <summary>
        /// Response class for signed URL.
        /// </summary>
        [Serializable]
        private class SignedUrlResponse
        {
            public string signedURL;
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