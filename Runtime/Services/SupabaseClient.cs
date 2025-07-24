using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SupabaseBridge.Runtime
{
    /// <summary>
    /// Client for interacting with the Supabase API.
    /// </summary>
    public class SupabaseClient
    {
        private readonly string baseUrl;
        private readonly string apiKey;
        private string accessToken;
        
        // Configuration path
        private const string CONFIG_PATH = "SupabaseBridge/Config";
        
        /// <summary>
        /// Initializes a new instance of the SupabaseClient class using configuration from resources.
        /// </summary>
        public SupabaseClient()
        {
            try
            {
                // Load configuration from resources
                SupabaseConfig config = LoadConfiguration();
                
                if (config != null)
                {
                    baseUrl = config.url;
                    apiKey = config.key;
                    
                    SupabaseLogger.Info($"Initialized SupabaseClient with URL: {baseUrl}", "SupabaseClient");
                }
                else
                {
                    throw new SupabaseException(
                        "Failed to load Supabase configuration from resources",
                        0,
                        "CONFIG_NOT_FOUND");
                }
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.Constructor");
                throw;
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the SupabaseClient class.
        /// </summary>
        /// <param name="url">The Supabase URL</param>
        /// <param name="key">The Supabase API key</param>
        public SupabaseClient(string url, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    throw new ArgumentNullException(nameof(url), "Supabase URL cannot be null or empty");
                }
                
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key), "Supabase API key cannot be null or empty");
                }
                
                baseUrl = url;
                apiKey = key;
                
                SupabaseLogger.Info($"Initialized SupabaseClient with URL: {baseUrl}", "SupabaseClient");
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.Constructor");
                throw;
            }
        }
        
        /// <summary>
        /// Sets the access token for authenticated requests.
        /// </summary>
        /// <param name="token">The access token</param>
        public void SetAccessToken(string token)
        {
            try
            {
                accessToken = token;
                SupabaseLogger.LogDebug("Access token set", "SupabaseClient");
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.SetAccessToken");
            }
        }
        
        /// <summary>
        /// Gets the base URL of the Supabase API.
        /// </summary>
        /// <returns>The base URL</returns>
        public string GetBaseUrl()
        {
            try
            {
                return baseUrl;
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.GetBaseUrl");
                return null;
            }
        }
        
        /// <summary>
        /// Sends a GET request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="queryParams">The query parameters</param>
        /// <returns>The response as a string</returns>
        public virtual async Task<string> Get(string endpoint, Dictionary<string, string> queryParams = null)
        {
            try
            {
                SupabaseLogger.LogDebug($"GET {endpoint}", "SupabaseClient");
                
                string url = BuildUrl(endpoint, queryParams);
                
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    AddHeaders(request);
                    
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new SupabaseException(
                            $"GET request failed: {request.error}",
                            (int)request.responseCode,
                            "REQUEST_FAILED");
                    }
                    
                    return request.downloadHandler.text;
                }
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.Get");
                throw;
            }
        }
        
        /// <summary>
        /// Sends a POST request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="jsonBody">The request body as JSON</param>
        /// <param name="queryParams">The query parameters</param>
        /// <returns>The response as a string</returns>
        public virtual async Task<string> Post(string endpoint, string jsonBody, Dictionary<string, string> queryParams = null)
        {
            try
            {
                SupabaseLogger.LogDebug($"POST {endpoint}", "SupabaseClient");
                
                string url = BuildUrl(endpoint, queryParams);
                
                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    
                    AddHeaders(request);
                    
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new SupabaseException(
                            $"POST request failed: {request.error}",
                            (int)request.responseCode,
                            "REQUEST_FAILED");
                    }
                    
                    return request.downloadHandler.text;
                }
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.Post");
                throw;
            }
        }
        
        /// <summary>
        /// Sends a PATCH request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="jsonBody">The request body as JSON</param>
        /// <param name="queryParams">The query parameters</param>
        /// <returns>The response as a string</returns>
        public virtual async Task<string> Patch(string endpoint, string jsonBody, Dictionary<string, string> queryParams = null)
        {
            try
            {
                SupabaseLogger.LogDebug($"PATCH {endpoint}", "SupabaseClient");
                
                string url = BuildUrl(endpoint, queryParams);
                
                using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    
                    AddHeaders(request);
                    
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new SupabaseException(
                            $"PATCH request failed: {request.error}",
                            (int)request.responseCode,
                            "REQUEST_FAILED");
                    }
                    
                    return request.downloadHandler.text;
                }
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.Patch");
                throw;
            }
        }
        
        /// <summary>
        /// Sends a DELETE request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="queryParams">The query parameters</param>
        /// <returns>The response as a string</returns>
        public virtual async Task<string> Delete(string endpoint, Dictionary<string, string> queryParams = null)
        {
            try
            {
                SupabaseLogger.LogDebug($"DELETE {endpoint}", "SupabaseClient");
                
                string url = BuildUrl(endpoint, queryParams);
                
                using (UnityWebRequest request = new UnityWebRequest(url, "DELETE"))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    
                    AddHeaders(request);
                    
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new SupabaseException(
                            $"DELETE request failed: {request.error}",
                            (int)request.responseCode,
                            "REQUEST_FAILED");
                    }
                    
                    return request.downloadHandler.text;
                }
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.Delete");
                throw;
            }
        }
        
        /// <summary>
        /// Uploads a file to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="fileData">The file data</param>
        /// <param name="fileName">The file name</param>
        /// <param name="contentType">The content type</param>
        /// <param name="queryParams">The query parameters</param>
        /// <returns>The response as a string</returns>
        public virtual async Task<string> UploadFile(string endpoint, byte[] fileData, string fileName, string contentType, Dictionary<string, string> queryParams = null)
        {
            try
            {
                SupabaseLogger.LogDebug($"UPLOAD {endpoint}", "SupabaseClient");
                
                string url = BuildUrl(endpoint, queryParams);
                
                WWWForm form = new WWWForm();
                form.AddBinaryData("file", fileData, fileName, contentType);
                
                using (UnityWebRequest request = UnityWebRequest.Post(url, form))
                {
                    AddHeaders(request);
                    
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new SupabaseException(
                            $"Upload failed: {request.error}",
                            (int)request.responseCode,
                            "UPLOAD_FAILED");
                    }
                    
                    return request.downloadHandler.text;
                }
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.UploadFile");
                throw;
            }
        }
        
        /// <summary>
        /// Downloads a file from the specified URL.
        /// </summary>
        /// <param name="url">The URL to download from</param>
        /// <returns>The file data as a byte array</returns>
        public virtual async Task<byte[]> DownloadFile(string url)
        {
            try
            {
                SupabaseLogger.LogDebug($"DOWNLOAD {url}", "SupabaseClient");
                
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new SupabaseException(
                            $"Download failed: {request.error}",
                            (int)request.responseCode,
                            "DOWNLOAD_FAILED");
                    }
                    
                    return request.downloadHandler.data;
                }
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.DownloadFile");
                throw;
            }
        }
        
        /// <summary>
        /// Builds a URL from an endpoint and query parameters.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="queryParams">The query parameters</param>
        /// <returns>The built URL</returns>
        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams)
        {
            string url = baseUrl;
            
            // Ensure the URL ends with a slash if the endpoint doesn't start with one
            if (!url.EndsWith("/") && !endpoint.StartsWith("/"))
            {
                url += "/";
            }
            
            // Remove leading slash from endpoint if the URL already ends with one
            if (url.EndsWith("/") && endpoint.StartsWith("/"))
            {
                endpoint = endpoint.Substring(1);
            }
            
            url += endpoint;
            
            // Add query parameters
            if (queryParams != null && queryParams.Count > 0)
            {
                url += "?";
                
                foreach (var param in queryParams)
                {
                    url += $"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}&";
                }
                
                // Remove the trailing &
                url = url.TrimEnd('&');
            }
            
            return url;
        }
        
        /// <summary>
        /// Adds the required headers to a request.
        /// </summary>
        /// <param name="request">The request</param>
        private void AddHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("apikey", apiKey);
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            }
        }
        
        /// <summary>
        /// Loads the Supabase configuration from resources.
        /// </summary>
        /// <returns>The configuration</returns>
        private SupabaseConfig LoadConfiguration()
        {
            try
            {
                // Try to load the configuration from resources
                TextAsset configAsset = Resources.Load<TextAsset>($"{CONFIG_PATH}/supabase-config");
                
                if (configAsset == null)
                {
                    SupabaseLogger.Warning("Supabase configuration file not found in Resources.");
                    return null;
                }
                
                // Parse the JSON configuration
                return JsonUtility.FromJson<SupabaseConfig>(configAsset.text);
            }
            catch (Exception ex)
            {
                SupabaseErrorHandler.HandleException(ex, "SupabaseClient.LoadConfiguration");
                return null;
            }
        }
    }
    
    /// <summary>
    /// Represents a Supabase configuration.
    /// </summary>
    [Serializable]
    public class SupabaseConfig
    {
        public string url;
        public string key;
        public bool isProduction;
    }
}