using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Tests.Mocks
{
    /// <summary>
    /// A mock implementation of SupabaseClient for testing purposes.
    /// </summary>
    public class MockSupabaseClient : SupabaseClient
    {
        private Dictionary<string, string> mockResponses = new Dictionary<string, string>();
        private Dictionary<string, Exception> mockExceptions = new Dictionary<string, Exception>();
        private Dictionary<string, int> delayMilliseconds = new Dictionary<string, int>();
        private Dictionary<string, int> callCounts = new Dictionary<string, int>();
        private string accessToken;
        private readonly string baseUrl;
        private bool simulateNetworkDelay = false;
        
        /// <summary>
        /// Initializes a new instance of the MockSupabaseClient class.
        /// </summary>
        /// <param name="url">The base URL</param>
        /// <param name="key">The API key</param>
        public MockSupabaseClient(string url = "https://mock-project.supabase.co", string key = "mock-api-key")
            : base(url, key)
        {
            this.baseUrl = url;
        }
        
        /// <summary>
        /// Sets a mock response for a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        /// <param name="response">The mock response</param>
        public void SetMockResponse(string endpoint, string response)
        {
            mockResponses[endpoint] = response;
        }
        
        /// <summary>
        /// Sets a mock exception for a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        /// <param name="exception">The mock exception</param>
        public void SetMockException(string endpoint, Exception exception)
        {
            mockExceptions[endpoint] = exception;
        }
        
        /// <summary>
        /// Clears all mock responses and exceptions.
        /// </summary>
        public void ClearMocks()
        {
            mockResponses.Clear();
            mockExceptions.Clear();
            delayMilliseconds.Clear();
            callCounts.Clear();
        }
        
        /// <summary>
        /// Sets the access token for authenticated requests.
        /// </summary>
        /// <param name="token">The access token</param>
        public new void SetAccessToken(string token)
        {
            this.accessToken = token;
            base.SetAccessToken(token);
        }
        
        /// <summary>
        /// Gets the current access token.
        /// </summary>
        /// <returns>The access token</returns>
        public string GetAccessToken()
        {
            return accessToken;
        }
        
        /// <summary>
        /// Sets whether to simulate network delay for API calls.
        /// </summary>
        /// <param name="simulate">Whether to simulate delay</param>
        public void SetSimulateNetworkDelay(bool simulate)
        {
            simulateNetworkDelay = simulate;
        }
        
        /// <summary>
        /// Sets the delay for a specific endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        /// <param name="milliseconds">The delay in milliseconds</param>
        public void SetEndpointDelay(string endpoint, int milliseconds)
        {
            delayMilliseconds[endpoint] = milliseconds;
        }
        
        /// <summary>
        /// Gets the number of times an endpoint has been called.
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        /// <returns>The call count</returns>
        public int GetCallCount(string endpoint)
        {
            return callCounts.TryGetValue(endpoint, out int count) ? count : 0;
        }
        
        /// <summary>
        /// Resets all call counts.
        /// </summary>
        public void ResetCallCounts()
        {
            callCounts.Clear();
        }
        
        /// <summary>
        /// Simulates network delay for an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        private async Task SimulateNetworkDelay(string endpoint)
        {
            // 호출 횟수 증가
            if (callCounts.ContainsKey(endpoint))
            {
                callCounts[endpoint]++;
            }
            else
            {
                callCounts[endpoint] = 1;
            }
            
            if (simulateNetworkDelay)
            {
                int delay = 100; // 기본 지연 100ms
                
                if (delayMilliseconds.TryGetValue(endpoint, out int customDelay))
                {
                    delay = customDelay;
                }
                
                await Task.Delay(delay);
            }
            else
            {
                await Task.Yield(); // 최소한의 비동기 작업
            }
        }
        
        /// <summary>
        /// Sends a mock GET request to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="queryParams">Optional query parameters</param>
        /// <returns>The mock response</returns>
        public override async Task<string> Get(string endpoint, Dictionary<string, string> queryParams = null)
        {
            // 네트워크 지연 시뮬레이션
            await SimulateNetworkDelay(endpoint);
            
            // Check if there's a mock exception for this endpoint
            if (mockExceptions.TryGetValue(endpoint, out Exception exception))
            {
                throw exception;
            }
            
            // Check if there's a mock response for this endpoint
            if (mockResponses.TryGetValue(endpoint, out string response))
            {
                return response;
            }
            
            // Return a default mock response
            return "{ \"mock\": \"response\" }";
        }
        
        /// <summary>
        /// Sends a mock POST request to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="jsonBody">The JSON body</param>
        /// <param name="queryParams">Optional query parameters</param>
        /// <returns>The mock response</returns>
        public override async Task<string> Post(string endpoint, string jsonBody, Dictionary<string, string> queryParams = null)
        {
            // 네트워크 지연 시뮬레이션
            await SimulateNetworkDelay(endpoint);
            
            // Check if there's a mock exception for this endpoint
            if (mockExceptions.TryGetValue(endpoint, out Exception exception))
            {
                throw exception;
            }
            
            // Check if there's a mock response for this endpoint
            if (mockResponses.TryGetValue(endpoint, out string response))
            {
                return response;
            }
            
            // Return a default mock response
            return "{ \"mock\": \"response\" }";
        }
        
        /// <summary>
        /// Sends a mock PATCH request to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="jsonBody">The JSON body</param>
        /// <param name="queryParams">Optional query parameters</param>
        /// <returns>The mock response</returns>
        public override async Task<string> Patch(string endpoint, string jsonBody, Dictionary<string, string> queryParams = null)
        {
            // 네트워크 지연 시뮬레이션
            await SimulateNetworkDelay(endpoint);
            
            // Check if there's a mock exception for this endpoint
            if (mockExceptions.TryGetValue(endpoint, out Exception exception))
            {
                throw exception;
            }
            
            // Check if there's a mock response for this endpoint
            if (mockResponses.TryGetValue(endpoint, out string response))
            {
                return response;
            }
            
            // Return a default mock response
            return "{ \"mock\": \"response\" }";
        }
        
        /// <summary>
        /// Sends a mock DELETE request to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="queryParams">Optional query parameters</param>
        /// <returns>The mock response</returns>
        public override async Task<string> Delete(string endpoint, Dictionary<string, string> queryParams = null)
        {
            // 네트워크 지연 시뮬레이션
            await SimulateNetworkDelay(endpoint);
            
            // Check if there's a mock exception for this endpoint
            if (mockExceptions.TryGetValue(endpoint, out Exception exception))
            {
                throw exception;
            }
            
            // Check if there's a mock response for this endpoint
            if (mockResponses.TryGetValue(endpoint, out string response))
            {
                return response;
            }
            
            // Return a default mock response
            return "{ \"mock\": \"response\" }";
        }
        
        /// <summary>
        /// Uploads a mock file to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint</param>
        /// <param name="fileData">The file data</param>
        /// <param name="fileName">The file name</param>
        /// <param name="contentType">The content type</param>
        /// <param name="queryParams">Optional query parameters</param>
        /// <returns>The mock response</returns>
        public override async Task<string> UploadFile(string endpoint, byte[] fileData, string fileName, string contentType, Dictionary<string, string> queryParams = null)
        {
            // 네트워크 지연 시뮬레이션
            await SimulateNetworkDelay(endpoint);
            
            // Check if there's a mock exception for this endpoint
            if (mockExceptions.TryGetValue(endpoint, out Exception exception))
            {
                throw exception;
            }
            
            // Check if there's a mock response for this endpoint
            if (mockResponses.TryGetValue(endpoint, out string response))
            {
                return response;
            }
            
            // Return a default mock response
            return "{ \"mock\": \"file_upload_response\" }";
        }
        
        /// <summary>
        /// Downloads a mock file from the specified URL.
        /// </summary>
        /// <param name="url">The file URL</param>
        /// <returns>The mock file data</returns>
        public override async Task<byte[]> DownloadFile(string url)
        {
            // 네트워크 지연 시뮬레이션
            await SimulateNetworkDelay(url);
            
            // Check if there's a mock exception for this URL
            if (mockExceptions.TryGetValue(url, out Exception exception))
            {
                throw exception;
            }
            
            // Return mock file data
            return System.Text.Encoding.UTF8.GetBytes("Mock file content");
        }
    }
} 