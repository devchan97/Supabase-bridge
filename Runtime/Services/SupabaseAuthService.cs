using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

namespace SupabaseBridge.Runtime
{
    /// <summary>
    /// Service for managing Supabase authentication operations at runtime.
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
                string jsonBody = JsonUtility.ToJson(new RequestWrapper(requestData));
                
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
                Debug.LogError($"Sign up failed: {ex.Message}");
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
                string jsonBody = JsonUtility.ToJson(new RequestWrapper(requestData));
                
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
                Debug.LogError($"Sign in failed: {ex.Message}");
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
                string jsonBody = JsonUtility.ToJson(new RequestWrapper(requestData));
                
                // Send the request
                await client.Post(SIGNOUT_ENDPOINT, jsonBody);
                
                // Clear the current user
                currentUser = null;
                
                // Clear the access token in the client
                client.SetAccessToken(null);
                
                // Raise the auth state changed event
                OnAuthStateChanged?.Invoke(null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Sign out failed: {ex.Message}");
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
                Debug.LogError($"Get current user failed: {ex.Message}");
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
                // Check if there is a current user with a refresh token
                if (currentUser == null || string.IsNullOrEmpty(currentUser.RefreshToken))
                {
                    Debug.LogWarning("No refresh token available");
                    throw new SupabaseException("No refresh token available. Please sign in again.", 0, "AUTH_NO_REFRESH_TOKEN");
                }
                
                // Create the request body
                var requestData = new Dictionary<string, object>
                {
                    { "refresh_token", currentUser.RefreshToken }
                };
                
                // Convert to JSON
                string jsonBody = JsonUtility.ToJson(new RequestWrapper(requestData));
                
                // Send the request with a timeout
                var tokenSource = new System.Threading.CancellationTokenSource();
                tokenSource.CancelAfter(TimeSpan.FromSeconds(10)); // 10초 타임아웃
                
                Task<string> requestTask = client.Post(REFRESH_TOKEN_ENDPOINT, jsonBody);
                
                // 타임아웃 처리
                var completedTask = await Task.WhenAny(requestTask, Task.Delay(10000, tokenSource.Token));
                
                if (completedTask != requestTask)
                {
                    tokenSource.Cancel();
                    throw new SupabaseException("Token refresh operation timed out", 0, "AUTH_REFRESH_TIMEOUT");
                }
                
                string response = await requestTask;
                tokenSource.Cancel();
                
                // Parse the response
                var authResponse = ParseAuthResponse(response);
                
                // Update the current user's tokens
                currentUser.AccessToken = authResponse.AccessToken;
                currentUser.RefreshToken = authResponse.RefreshToken;
                
                // Set the new access token in the client
                client.SetAccessToken(currentUser.AccessToken);
                
                // Notify listeners that the auth state has changed
                OnAuthStateChanged?.Invoke(currentUser);
                
                return currentUser;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Refresh token failed: {ex.Message}");
                
                // 인증 상태 무효화 - 재로그인 필요
                if (ex is SupabaseException supabaseEx && 
                    (supabaseEx.StatusCode == 401 || supabaseEx.ErrorCode == "AUTH_INVALID_REFRESH_TOKEN"))
                {
                    // 인증 상태 초기화
                    currentUser = null;
                    client.SetAccessToken(null);
                    
                    // 인증 상태 변경 알림
                    OnAuthStateChanged?.Invoke(null);
                }
                
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
                // Parse the JSON response
                AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(json);
                
                // Create the user object
                User user = ParseUserResponse(JsonUtility.ToJson(authResponse.user));
                
                // Set the tokens
                user.AccessToken = authResponse.access_token;
                user.RefreshToken = authResponse.refresh_token;
                
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
                // Parse the JSON response
                UserResponse userResponse = JsonUtility.FromJson<UserResponse>(json);
                
                // Create the user object
                var user = new User
                {
                    Id = userResponse.id,
                    Email = userResponse.email,
                    UserMetadata = new Dictionary<string, object>()
                };
                
                // Parse the user metadata if available
                if (userResponse.user_metadata != null)
                {
                    // For simplicity, we're storing the raw JSON
                    user.UserMetadata["raw"] = JsonUtility.ToJson(userResponse.user_metadata);
                }
                
                return user;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse user response: {ex.Message}");
                throw new SupabaseException("Failed to parse user response", 0, "USER_PARSE_ERROR", ex);
            }
        }
        
        /// <summary>
        /// Helper class for wrapping dictionary data in a serializable format.
        /// </summary>
        [Serializable]
        private class RequestWrapper
        {
            public string data;
            
            public RequestWrapper(Dictionary<string, object> dict)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                
                bool first = true;
                foreach (var kvp in dict)
                {
                    if (!first)
                        sb.Append(",");
                    
                    sb.Append($"\"{kvp.Key}\":");
                    
                    if (kvp.Value is string)
                        sb.Append($"\"{kvp.Value}\"");
                    else if (kvp.Value is Dictionary<string, object>)
                        sb.Append(JsonUtility.ToJson(new RequestWrapper((Dictionary<string, object>)kvp.Value)));
                    else
                        sb.Append(kvp.Value.ToString());
                    
                    first = false;
                }
                
                sb.Append("}");
                data = sb.ToString();
            }
        }
        
        /// <summary>
        /// Response class for authentication.
        /// </summary>
        [Serializable]
        private class AuthResponse
        {
            public string access_token;
            public string refresh_token;
            public UserResponse user;
        }
        
        /// <summary>
        /// Response class for user data.
        /// </summary>
        [Serializable]
        private class UserResponse
        {
            public string id;
            public string email;
            public UserMetadata user_metadata;
        }
        
        /// <summary>
        /// Class for user metadata.
        /// </summary>
        [Serializable]
        private class UserMetadata
        {
            // This is a placeholder for any user metadata fields
            // In a real implementation, you would define specific fields here
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
} 