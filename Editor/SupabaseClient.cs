using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Client for interacting with the Supabase API in the Unity Editor.
    /// </summary>
    public class SupabaseClient : IDisposable
    {
        private HttpClient httpClient;
        private readonly string supabaseUrl;
        private readonly string supabaseKey;
        private string accessToken;
        private const int DEFAULT_TIMEOUT_SECONDS = 30;
        private bool isDisposed = false;

        /// <summary>
        /// Gets or sets the timeout for HTTP requests in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = DEFAULT_TIMEOUT_SECONDS;

        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(accessToken);

        /// <summary>
        /// Gets the base URL of the Supabase API.
        /// </summary>
        /// <returns>The base URL</returns>
        public string GetBaseUrl()
        {
            return supabaseUrl;
        }

        /// <summary>
        /// Initializes a new instance of the SupabaseClient class.
        /// </summary>
        /// <param name="supabaseUrl">The Supabase URL</param>
        /// <param name="supabaseKey">The Supabase API key</param>
        public SupabaseClient(string supabaseUrl, string supabaseKey)
        {
            if (string.IsNullOrEmpty(supabaseUrl))
            {
                throw new ArgumentNullException(nameof(supabaseUrl), "Supabase URL cannot be null or empty");
            }

            if (string.IsNullOrEmpty(supabaseKey))
            {
                throw new ArgumentNullException(nameof(supabaseKey), "Supabase API key cannot be null or empty");
            }

            this.supabaseUrl = supabaseUrl;
            this.supabaseKey = supabaseKey;
            
            InitializeHttpClient();
        }

        /// <summary>
        /// Initializes the HTTP client.
        /// </summary>
        private void InitializeHttpClient()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
            
            UpdateAuthorizationHeader();
        }

        /// <summary>
        /// Updates the authorization header with the current access token.
        /// </summary>
        private void UpdateAuthorizationHeader()
        {
            if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                httpClient.DefaultRequestHeaders.Remove("Authorization");
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            }
        }

        /// <summary>
        /// Sets the access token for authenticated requests.
        /// </summary>
        /// <param name="token">The access token</param>
        public void SetAccessToken(string token)
        {
            accessToken = token;
            UpdateAuthorizationHeader();
        }

        /// <summary>
        /// Clears the access token.
        /// </summary>
        public void ClearAccessToken()
        {
            accessToken = null;
            UpdateAuthorizationHeader();
        }

        /// <summary>
        /// Sends a GET request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="queryParams">The query parameters</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response as a string</returns>
        public async Task<string> Get(string endpoint, Dictionary<string, string> queryParams = null, CancellationToken cancellationToken = default)
        {
            try
            {
                string url = BuildUrl(endpoint, queryParams);
                
                HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
                await HandleResponseErrors(response);
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw CreateSupabaseException("GET request failed", ex);
            }
        }

        /// <summary>
        /// Sends a POST request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="jsonContent">The request body as JSON</param>
        /// <param name="queryParams">The query parameters</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response as a string</returns>
        public async Task<string> Post(string endpoint, string jsonContent, Dictionary<string, string> queryParams = null, CancellationToken cancellationToken = default)
        {
            try
            {
                string url = BuildUrl(endpoint, queryParams);
                
                StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(url, content, cancellationToken);
                await HandleResponseErrors(response);
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw CreateSupabaseException("POST request failed", ex);
            }
        }

        /// <summary>
        /// Sends a PUT request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="jsonContent">The request body as JSON</param>
        /// <param name="queryParams">The query parameters</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response as a string</returns>
        public async Task<string> Put(string endpoint, string jsonContent, Dictionary<string, string> queryParams = null, CancellationToken cancellationToken = default)
        {
            try
            {
                string url = BuildUrl(endpoint, queryParams);
                
                StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PutAsync(url, content, cancellationToken);
                await HandleResponseErrors(response);
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw CreateSupabaseException("PUT request failed", ex);
            }
        }

        /// <summary>
        /// Sends a PATCH request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="jsonContent">The request body as JSON</param>
        /// <param name="queryParams">The query parameters</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response as a string</returns>
        public async Task<string> Patch(string endpoint, string jsonContent, Dictionary<string, string> queryParams = null, CancellationToken cancellationToken = default)
        {
            try
            {
                string url = BuildUrl(endpoint, queryParams);
                
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                
                HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                await HandleResponseErrors(response);
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw CreateSupabaseException("PATCH request failed", ex);
            }
        }

        /// <summary>
        /// Sends a DELETE request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="queryParams">The query parameters</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response as a string</returns>
        public async Task<string> Delete(string endpoint, Dictionary<string, string> queryParams = null, CancellationToken cancellationToken = default)
        {
            try
            {
                string url = BuildUrl(endpoint, queryParams);
                
                HttpResponseMessage response = await httpClient.DeleteAsync(url, cancellationToken);
                await HandleResponseErrors(response);
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw CreateSupabaseException("DELETE request failed", ex);
            }
        }

        /// <summary>
        /// Sends a multipart form data request to the Supabase API.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="formData">The form data</param>
        /// <param name="method">The HTTP method (defaults to POST)</param>
        /// <param name="queryParams">The query parameters</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response as a string</returns>
        public async Task<string> SendMultipartFormData(
            string endpoint, 
            MultipartFormDataContent formData, 
            HttpMethod method = null,
            Dictionary<string, string> queryParams = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                string url = BuildUrl(endpoint, queryParams);
                
                HttpRequestMessage request = new HttpRequestMessage(method ?? HttpMethod.Post, url)
                {
                    Content = formData
                };
                
                HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
                await HandleResponseErrors(response);
                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw CreateSupabaseException("Multipart form data request failed", ex);
            }
        }

        /// <summary>
        /// Builds a URL from an endpoint and query parameters.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="queryParams">The query parameters</param>
        /// <returns>The built URL</returns>
        private string BuildUrl(string endpoint, Dictionary<string, string> queryParams = null)
        {
            UriBuilder builder = new UriBuilder(supabaseUrl);
            
            // Ensure the path starts with a slash
            if (!endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }
            
            builder.Path += endpoint;
            
            // Add query parameters
            if (queryParams != null && queryParams.Count > 0)
            {
                StringBuilder query = new StringBuilder();
                foreach (var param in queryParams)
                {
                    if (query.Length > 0)
                    {
                        query.Append("&");
                    }
                    
                    query.Append($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}");
                }
                
                builder.Query = query.ToString();
            }
            
            return builder.Uri.ToString();
        }

        /// <summary>
        /// Handles errors in the HTTP response.
        /// </summary>
        /// <param name="response">The HTTP response</param>
        private async Task HandleResponseErrors(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                string errorMessage = $"HTTP error {(int)response.StatusCode} ({response.ReasonPhrase})";
                
                // Try to extract error message from response
                try
                {
                    // Attempt to parse JSON error response
                    var jsonError = JsonUtility.FromJson<SupabaseErrorResponse>(responseContent);
                    if (!string.IsNullOrEmpty(jsonError?.error))
                    {
                        errorMessage = $"{errorMessage}: {jsonError.error}";
                    }
                    else if (!string.IsNullOrEmpty(jsonError?.message))
                    {
                        errorMessage = $"{errorMessage}: {jsonError.message}";
                    }
                    else if (!string.IsNullOrEmpty(responseContent))
                    {
                        errorMessage = $"{errorMessage}: {responseContent}";
                    }
                }
                catch
                {
                    // If JSON parsing fails, use raw response
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        errorMessage = $"{errorMessage}: {responseContent}";
                    }
                }
                
                throw new SupabaseException(errorMessage, (int)response.StatusCode, "API_ERROR");
            }
        }

        /// <summary>
        /// Creates a SupabaseException from an exception.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="ex">The exception</param>
        /// <returns>A SupabaseException</returns>
        private SupabaseException CreateSupabaseException(string message, Exception ex)
        {
            int statusCode = 0;
            string errorCode = "UNKNOWN_ERROR";
            
            if (ex is SupabaseException supabaseEx)
            {
                return supabaseEx;
            }
            else if (ex is HttpRequestException)
            {
                message = $"{message}: {ex.Message}";
                errorCode = "NETWORK_ERROR";
            }
            else if (ex is TaskCanceledException)
            {
                message = "Request timed out";
                errorCode = "TIMEOUT";
            }
            else if (ex is WebException webEx)
            {
                message = $"{message}: {webEx.Message}";
                errorCode = "WEB_ERROR";
                
                if (webEx.Response is HttpWebResponse response)
                {
                    statusCode = (int)response.StatusCode;
                }
            }
            
            return new SupabaseException(message, statusCode, errorCode, ex);
        }

        /// <summary>
        /// Disposes the HTTP client.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the HTTP client.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    httpClient?.Dispose();
                }

                httpClient = null;
                isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a Supabase error response.
    /// </summary>
    [Serializable]
    public class SupabaseErrorResponse
    {
        public string error;
        public string message;
        public string code;
    }
}