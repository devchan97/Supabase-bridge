using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Threading;
using System.Linq;
using Process = System.Diagnostics.Process;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Editor
{
    /// <summary>
    /// Service for managing Supabase authentication operations.
    /// Provides methods for sign up, sign in, sign out, and user management.
    /// </summary>
    public class SupabaseAuthService
    {
        private readonly SupabaseClient client;
        private User currentUser;
        
        // Auth endpoints
        private const string SIGNUP_ENDPOINT = "/auth/v1/signup";
        private const string SIGNIN_ENDPOINT = "/auth/v1/token?grant_type=password";
        private const string SIGNOUT_ENDPOINT = "/auth/v1/logout";
        private const string USER_ENDPOINT = "/auth/v1/user";
        private const string REFRESH_TOKEN_ENDPOINT = "/auth/v1/token?grant_type=refresh_token";
        
        // Social login endpoints
        private const string AUTHORIZE_URL = "/auth/v1/authorize";
        private const string TOKEN_ENDPOINT = "/auth/v1/token";
        
        // Local callback server
        private HttpListener httpListener;
        private CancellationTokenSource callbackCancellationToken;
        private const int CALLBACK_PORT = 54321;
        private const string CALLBACK_PATH = "/auth/callback";
        private const string CALLBACK_URL = "http://localhost:54321/auth/callback";
        
        /// <summary>
        /// Gets the currently authenticated user.
        /// </summary>
        public User CurrentUser => currentUser;
        
        /// <summary>
        /// Gets a value indicating whether a user is currently authenticated.
        /// </summary>
        public bool IsAuthenticated => currentUser != null && !string.IsNullOrEmpty(currentUser.AccessToken);
        
        /// <summary>
        /// Event raised when the authentication state changes.
        /// </summary>
        public event Action<User> OnAuthStateChanged;
        
        /// <summary>
        /// Initializes a new instance of the SupabaseAuthService class.
        /// </summary>
        /// <param name="client">The Supabase client to use for API requests</param>
        public SupabaseAuthService(SupabaseClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }
        
        /// <summary>
        /// Signs up a new user with email and password.
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <param name="password">The user's password</param>
        /// <param name="userData">Optional additional user data</param>
        /// <returns>The newly created user</returns>
        public async Task<User> SignUp(string email, string password, Dictionary<string, object> userData = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    throw new ArgumentException("Email is required", nameof(email));
                
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Password is required", nameof(password));
                
                // Create the request body
                var requestData = new Dictionary<string, object>
                {
                    { "email", email },
                    { "password", password }
                };
                
                // Add additional user data if provided
                if (userData != null)
                {
                    requestData["data"] = userData;
                }
                
                // Convert to JSON
                string jsonBody = SupabaseJsonUtility.ToJson(requestData);
                
                // Send the request
                string response = await client.Post(SIGNUP_ENDPOINT, jsonBody);
                
                // Parse the response
                var authResponse = ParseAuthResponse(response);
                
                // Update the current user
                currentUser = authResponse;
                
                // Set the access token in the client
                client.SetAccessToken(currentUser.AccessToken);
                
                // Raise the auth state changed event
                OnAuthStateChanged?.Invoke(currentUser);
                
                return currentUser;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Sign up failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Signs in a user with email and password.
        /// </summary>
        /// <param name="email">The user's email</param>
        /// <param name="password">The user's password</param>
        /// <returns>The authenticated user</returns>
        public async Task<User> SignIn(string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    throw new ArgumentException("Email is required", nameof(email));
                
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Password is required", nameof(password));
                
                // Create the request body
                var requestData = new Dictionary<string, object>
                {
                    { "email", email },
                    { "password", password }
                };
                
                // Convert to JSON
                string jsonBody = SupabaseJsonUtility.ToJson(requestData);
                
                // Send the request
                string response = await client.Post(SIGNIN_ENDPOINT, jsonBody);
                
                // Parse the response
                var authResponse = ParseAuthResponse(response);
                
                // Update the current user
                currentUser = authResponse;
                
                // Set the access token in the client
                client.SetAccessToken(currentUser.AccessToken);
                
                // Raise the auth state changed event
                OnAuthStateChanged?.Invoke(currentUser);
                
                return currentUser;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Sign in failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Signs out the current user.
        /// </summary>
        /// <returns>A task that completes when the sign out operation is done</returns>
        public async Task SignOut()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    Debug.LogWarning("No user is currently signed in");
                    return;
                }
                
                // Create the request body
                var requestData = new Dictionary<string, object>
                {
                    { "refresh_token", currentUser.RefreshToken }
                };
                
                // Convert to JSON
                string jsonBody = SupabaseJsonUtility.ToJson(requestData);
                
                // Send the request
                await client.Post(SIGNOUT_ENDPOINT, jsonBody);
                
                // Clear the current user
                currentUser = null;
                
                // Clear the access token in the client
                client.ClearAccessToken();
                
                // Raise the auth state changed event
                OnAuthStateChanged?.Invoke(null);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Sign out failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the current user's information.
        /// </summary>
        /// <returns>The current user</returns>
        public async Task<User> GetCurrentUser()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    Debug.LogWarning("No user is currently signed in");
                    return null;
                }
                
                // Send the request
                string response = await client.Get(USER_ENDPOINT);
                
                // Parse the response
                var userResponse = ParseUserResponse(response);
                
                // Update the current user's information
                if (currentUser != null)
                {
                    currentUser.Id = userResponse.Id;
                    currentUser.Email = userResponse.Email;
                    currentUser.UserMetadata = userResponse.UserMetadata;
                    
                    // Raise the auth state changed event
                    OnAuthStateChanged?.Invoke(currentUser);
                }
                
                return currentUser;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Get current user failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Refreshes the access token using the refresh token.
        /// </summary>
        /// <returns>The updated user with new tokens</returns>
        public async Task<User> RefreshToken()
        {
            try
            {
                if (currentUser == null || string.IsNullOrEmpty(currentUser.RefreshToken))
                {
                    Debug.LogWarning("No refresh token available");
                    return null;
                }
                
                // Create the request body
                var requestData = new Dictionary<string, object>
                {
                    { "refresh_token", currentUser.RefreshToken }
                };
                
                // Convert to JSON
                string jsonBody = SupabaseJsonUtility.ToJson(requestData);
                
                // Send the request
                string response = await client.Post(REFRESH_TOKEN_ENDPOINT, jsonBody);
                
                // Parse the response
                var authResponse = ParseAuthResponse(response);
                
                // Update the current user's tokens
                currentUser.AccessToken = authResponse.AccessToken;
                currentUser.RefreshToken = authResponse.RefreshToken;
                
                // Set the new access token in the client
                client.SetAccessToken(currentUser.AccessToken);
                
                return currentUser;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Refresh token failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Initiates a social login process.
        /// </summary>
        /// <param name="provider">The social provider to use</param>
        /// <returns>A task that completes when the social login process is done</returns>
        public async Task<User> InitiateSocialLogin(SocialProvider provider)
        {
            try
            {
                // Start the local callback server
                StartCallbackServer();
                
                // Build the authorization URL
                string providerName = GetProviderName(provider);
                string authUrl = BuildAuthorizationUrl(providerName);
                
                // Open the browser
                OpenBrowser(authUrl);
                
                // Wait for the callback
                string code = await WaitForCallback();
                
                // Exchange the code for tokens
                return await ExchangeCodeForTokens(code);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Social login failed: {ex.Message}");
                throw;
            }
            finally
            {
                // Stop the callback server
                StopCallbackServer();
            }
        }
        
        /// <summary>
        /// Gets the provider name for a social provider.
        /// </summary>
        /// <param name="provider">The social provider</param>
        /// <returns>The provider name</returns>
        private string GetProviderName(SocialProvider provider)
        {
            switch (provider)
            {
                case SocialProvider.Google:
                    return "google";
                case SocialProvider.Facebook:
                    return "facebook";
                case SocialProvider.Twitter:
                    return "twitter";
                case SocialProvider.GitHub:
                    return "github";
                case SocialProvider.Discord:
                    return "discord";
                default:
                    throw new ArgumentException($"Unsupported provider: {provider}");
            }
        }
        
        /// <summary>
        /// Builds the authorization URL for a social provider.
        /// </summary>
        /// <param name="providerName">The provider name</param>
        /// <returns>The authorization URL</returns>
        private string BuildAuthorizationUrl(string providerName)
        {
            // Get the base URL from the client
            string baseUrl = client.GetBaseUrl();
            
            // Build the query parameters
            var queryParams = new Dictionary<string, string>
            {
                { "provider", providerName },
                { "redirect_to", CALLBACK_URL }
            };
            
            // Build the URL
            string queryString = string.Join("&", queryParams.Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));
            return $"{baseUrl}{AUTHORIZE_URL}?{queryString}";
        }
        
        /// <summary>
        /// Opens the default browser with the specified URL.
        /// </summary>
        /// <param name="url">The URL to open</param>
        private void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to open browser: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Starts the local callback server.
        /// </summary>
        private void StartCallbackServer()
        {
            try
            {
                // Create a new cancellation token
                callbackCancellationToken = new CancellationTokenSource();
                
                // Create a new HTTP listener
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://localhost:{CALLBACK_PORT}/");
                httpListener.Start();
                
                Debug.Log("Callback server started");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to start callback server: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Stops the local callback server.
        /// </summary>
        private void StopCallbackServer()
        {
            try
            {
                // Cancel any pending operations
                callbackCancellationToken?.Cancel();
                callbackCancellationToken?.Dispose();
                callbackCancellationToken = null;
                
                // Stop the HTTP listener
                if (httpListener != null)
                {
                    httpListener.Stop();
                    httpListener.Close();
                    httpListener = null;
                    
                    Debug.Log("Callback server stopped");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error stopping callback server: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Waits for a callback from the social provider.
        /// </summary>
        /// <returns>The authorization code</returns>
        private async Task<string> WaitForCallback()
        {
            try
            {
                // Wait for a request
                var context = await httpListener.GetContextAsync();
                
                // Get the request URL
                string requestUrl = context.Request.Url.ToString();
                
                // Parse the query string
                var queryParams = ParseQueryString(context.Request.Url.Query);
                
                // Get the code
                string code = queryParams.ContainsKey("code") ? queryParams["code"] : null;
                
                // Send a response
                string responseHtml = "<html><body><h1>Authentication Successful</h1><p>You can now close this window and return to Unity.</p></body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
                
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/html";
                
                var responseOutput = context.Response.OutputStream;
                await responseOutput.WriteAsync(buffer, 0, buffer.Length);
                responseOutput.Close();
                
                if (string.IsNullOrEmpty(code))
                {
                    throw new SupabaseException("No authorization code received", 0, "AUTH_CODE_MISSING");
                }
                
                return code;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error waiting for callback: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Parses a query string into a dictionary.
        /// </summary>
        /// <param name="queryString">The query string to parse</param>
        /// <returns>A dictionary of query parameters</returns>
        private Dictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(queryString))
                return result;
            
            // Remove the leading '?' if present
            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);
            
            // Split the query string by '&'
            string[] pairs = queryString.Split('&');
            
            foreach (string pair in pairs)
            {
                // Split the pair by '='
                string[] keyValue = pair.Split('=');
                
                if (keyValue.Length == 2)
                {
                    // Decode the key and value
                    string key = WebUtility.UrlDecode(keyValue[0]);
                    string value = WebUtility.UrlDecode(keyValue[1]);
                    
                    // Add to the dictionary
                    result[key] = value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Exchanges an authorization code for access and refresh tokens.
        /// </summary>
        /// <param name="code">The authorization code</param>
        /// <returns>The authenticated user</returns>
        private async Task<User> ExchangeCodeForTokens(string code)
        {
            try
            {
                // Create the request body
                var requestData = new Dictionary<string, object>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", CALLBACK_URL }
                };
                
                // Convert to JSON
                string jsonBody = SupabaseJsonUtility.ToJson(requestData);
                
                // Send the request
                string response = await client.Post(TOKEN_ENDPOINT, jsonBody);
                
                // Parse the response
                var authResponse = ParseAuthResponse(response);
                
                // Update the current user
                currentUser = authResponse;
                
                // Set the access token in the client
                client.SetAccessToken(currentUser.AccessToken);
                
                // Raise the auth state changed event
                OnAuthStateChanged?.Invoke(currentUser);
                
                return currentUser;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to exchange code for tokens: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Parses an authentication response from the Supabase API.
        /// </summary>
        /// <param name="json">The JSON response</param>
        /// <returns>A User object with authentication information</returns>
        private User ParseAuthResponse(string json)
        {
            try
            {
                // Extract the access token
                string accessToken = SupabaseResponseParser.ExtractJsonProperty(json, "access_token");
                
                // Extract the refresh token
                string refreshToken = SupabaseResponseParser.ExtractJsonProperty(json, "refresh_token");
                
                // Extract the user data
                string userJson = SupabaseResponseParser.ExtractJsonProperty(json, "user");
                
                // Parse the user data
                User user = ParseUserResponse(userJson);
                
                // Set the tokens
                user.AccessToken = accessToken;
                user.RefreshToken = refreshToken;
                
                return user;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse auth response: {ex.Message}");
                throw new SupabaseException("Failed to parse authentication response", 0, "AUTH_PARSE_ERROR", ex);
            }
        }
        
        /// <summary>
        /// Parses a user response from the Supabase API.
        /// </summary>
        /// <param name="json">The JSON response</param>
        /// <returns>A User object with user information</returns>
        private User ParseUserResponse(string json)
        {
            try
            {
                // Extract the user ID
                string id = SupabaseResponseParser.ExtractJsonProperty(json, "id");
                
                // Extract the email
                string email = SupabaseResponseParser.ExtractJsonProperty(json, "email");
                
                // Extract the user metadata
                string metadataJson = SupabaseResponseParser.ExtractJsonProperty(json, "user_metadata");
                
                // Create the user object
                var user = new User
                {
                    Id = id,
                    Email = email,
                    UserMetadata = new Dictionary<string, object>()
                };
                
                // Parse the user metadata if available
                if (!string.IsNullOrEmpty(metadataJson))
                {
                    // For simplicity, we're not parsing the metadata in detail
                    // In a real implementation, you would parse this into a proper dictionary
                    user.UserMetadata["raw"] = metadataJson;
                }
                
                return user;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse user response: {ex.Message}");
                throw new SupabaseException("Failed to parse user response", 0, "USER_PARSE_ERROR", ex);
            }
        }
    }
    
    /// <summary>
    /// Represents a Supabase user with authentication information.
    /// </summary>
    [Serializable]
    public class User
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the user's email.
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Gets or sets the user's metadata.
        /// </summary>
        public Dictionary<string, object> UserMetadata { get; set; }
        
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; }
        
        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        public string RefreshToken { get; set; }
    }
    
    /// <summary>
    /// Represents a social login provider.
    /// </summary>
    public enum SocialProvider
    {
        Google,
        Facebook,
        Twitter,
        GitHub,
        Discord
    }
}