using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using SupabaseBridge.Runtime;
using SupabaseBridge.Tests.Mocks;

namespace SupabaseBridge.Tests.Unit
{
    public class SupabaseAuthServiceTests
    {
        private MockSupabaseClient mockClient;
        private SupabaseAuthService authService;
        
        [SetUp]
        public void SetUp()
        {
            // Initialize the mock client
            mockClient = new MockSupabaseClient();
            
            // Initialize the auth service with the mock client
            authService = new SupabaseAuthService(mockClient);
            
            // Clear log history
            SupabaseLogger.ClearLogHistory();
        }
        
        [Test]
        public async Task SignUp_WithValidCredentials_ReturnsUser()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            
            // Set up mock response for sign up
            string mockResponse = @"{
                ""access_token"": ""mock-access-token"",
                ""refresh_token"": ""mock-refresh-token"",
                ""user"": {
                    ""id"": ""mock-user-id"",
                    ""email"": ""test@example.com"",
                    ""user_metadata"": {}
                }
            }";
            mockClient.SetMockResponse("/auth/v1/signup", mockResponse);
            
            // Act
            User user = await authService.SignUp(email, password);
            
            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("mock-user-id", user.Id);
            Assert.AreEqual(email, user.Email);
            Assert.AreEqual("mock-access-token", user.AccessToken);
            Assert.AreEqual("mock-refresh-token", user.RefreshToken);
            
            // Verify that the access token was set on the client
            Assert.AreEqual("mock-access-token", mockClient.GetAccessToken());
        }
        
        [Test]
        public void SignUp_WithEmptyEmail_ThrowsArgumentException()
        {
            // Arrange
            string email = "";
            string password = "password123";
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await authService.SignUp(email, password));
            Assert.AreEqual("Email is required (Parameter 'email')", ex.Message);
        }
        
        [Test]
        public void SignUp_WithEmptyPassword_ThrowsArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            string password = "";
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await authService.SignUp(email, password));
            Assert.AreEqual("Password is required (Parameter 'password')", ex.Message);
        }
        
        [Test]
        public async Task SignIn_WithValidCredentials_ReturnsUser()
        {
            // Arrange
            string email = "test@example.com";
            string password = "password123";
            
            // Set up mock response for sign in
            string mockResponse = @"{
                ""access_token"": ""mock-access-token"",
                ""refresh_token"": ""mock-refresh-token"",
                ""user"": {
                    ""id"": ""mock-user-id"",
                    ""email"": ""test@example.com"",
                    ""user_metadata"": {}
                }
            }";
            mockClient.SetMockResponse("/auth/v1/token?grant_type=password", mockResponse);
            
            // Act
            User user = await authService.SignIn(email, password);
            
            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("mock-user-id", user.Id);
            Assert.AreEqual(email, user.Email);
            Assert.AreEqual("mock-access-token", user.AccessToken);
            Assert.AreEqual("mock-refresh-token", user.RefreshToken);
            
            // Verify that the access token was set on the client
            Assert.AreEqual("mock-access-token", mockClient.GetAccessToken());
        }
        
        [Test]
        public void SignIn_WithInvalidCredentials_ThrowsException()
        {
            // Arrange
            string email = "test@example.com";
            string password = "wrong-password";
            
            // Set up mock exception for sign in
            var mockException = new SupabaseException("Invalid login credentials", 401, "AUTH_INVALID_CREDENTIALS");
            mockClient.SetMockException("/auth/v1/token?grant_type=password", mockException);
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<SupabaseException>(async () => await authService.SignIn(email, password));
            Assert.AreEqual(401, ex.StatusCode);
            Assert.AreEqual("AUTH_INVALID_CREDENTIALS", ex.ErrorCode);
        }
        
        [Test]
        public async Task SignOut_WhenAuthenticated_ClearsUser()
        {
            // Arrange
            // First sign in to set up the authenticated state
            string mockSignInResponse = @"{
                ""access_token"": ""mock-access-token"",
                ""refresh_token"": ""mock-refresh-token"",
                ""user"": {
                    ""id"": ""mock-user-id"",
                    ""email"": ""test@example.com"",
                    ""user_metadata"": {}
                }
            }";
            mockClient.SetMockResponse("/auth/v1/token?grant_type=password", mockSignInResponse);
            await authService.SignIn("test@example.com", "password123");
            
            // Set up mock response for sign out
            string mockSignOutResponse = "{}";
            mockClient.SetMockResponse("/auth/v1/logout", mockSignOutResponse);
            
            // Act
            await authService.SignOut();
            
            // Assert
            Assert.IsFalse(authService.IsAuthenticated);
            Assert.IsNull(authService.CurrentUser);
        }
        
        [Test]
        public async Task GetCurrentUser_WhenAuthenticated_ReturnsUser()
        {
            // Arrange
            // First sign in to set up the authenticated state
            string mockSignInResponse = @"{
                ""access_token"": ""mock-access-token"",
                ""refresh_token"": ""mock-refresh-token"",
                ""user"": {
                    ""id"": ""mock-user-id"",
                    ""email"": ""test@example.com"",
                    ""user_metadata"": {}
                }
            }";
            mockClient.SetMockResponse("/auth/v1/token?grant_type=password", mockSignInResponse);
            await authService.SignIn("test@example.com", "password123");
            
            // Set up mock response for get user
            string mockUserResponse = @"{
                ""id"": ""mock-user-id"",
                ""email"": ""test@example.com"",
                ""user_metadata"": {
                    ""name"": ""Test User""
                }
            }";
            mockClient.SetMockResponse("/auth/v1/user", mockUserResponse);
            
            // Act
            User user = await authService.GetCurrentUser();
            
            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("mock-user-id", user.Id);
            Assert.AreEqual("test@example.com", user.Email);
            Assert.IsNotNull(user.UserMetadata);
        }
        
        [Test]
        public async Task RefreshToken_WhenAuthenticated_UpdatesTokens()
        {
            // 타임아웃 설정
            var cts = new System.Threading.CancellationTokenSource(5000); // 5초 타임아웃
            
            try
            {
                // Arrange
                // First sign in to set up the authenticated state
                string mockSignInResponse = @"{
                    ""access_token"": ""old-access-token"",
                    ""refresh_token"": ""old-refresh-token"",
                    ""user"": {
                        ""id"": ""mock-user-id"",
                        ""email"": ""test@example.com"",
                        ""user_metadata"": {}
                    }
                }";
                mockClient.SetMockResponse("/auth/v1/token?grant_type=password", mockSignInResponse);
                await authService.SignIn("test@example.com", "password123");
                
                // Set up mock response for refresh token
                string mockRefreshResponse = @"{
                    ""access_token"": ""new-access-token"",
                    ""refresh_token"": ""new-refresh-token"",
                    ""user"": {
                        ""id"": ""mock-user-id"",
                        ""email"": ""test@example.com"",
                        ""user_metadata"": {}
                    }
                }";
                mockClient.SetMockResponse("/auth/v1/token?grant_type=refresh_token", mockRefreshResponse);
                
                // Act - 타임아웃 적용
                var refreshTask = authService.RefreshToken();
                var completedTask = await Task.WhenAny(refreshTask, Task.Delay(5000, cts.Token));
                
                if (completedTask != refreshTask)
                {
                    Assert.Fail("RefreshToken 작업이 타임아웃되었습니다.");
                    return;
                }
                
                User user = await refreshTask;
                
                // Assert
                Assert.IsNotNull(user);
                Assert.AreEqual("new-access-token", user.AccessToken);
                Assert.AreEqual("new-refresh-token", user.RefreshToken);
                
                // Verify that the new access token was set on the client
                Assert.AreEqual("new-access-token", mockClient.GetAccessToken());
            }
            finally
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
        
        [Test]
        public void RefreshToken_WithoutCurrentUser_ThrowsException()
        {
            // Arrange - 사용자 로그인 없이 바로 토큰 갱신 시도
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<SupabaseException>(async () => await authService.RefreshToken());
            Assert.AreEqual("AUTH_NO_REFRESH_TOKEN", ex.ErrorCode);
        }
        
        [Test]
        public void RefreshToken_WithInvalidToken_ThrowsException()
        {
            // Arrange
            // 먼저 로그인하여 사용자 설정
            string mockSignInResponse = @"{
                ""access_token"": ""old-access-token"",
                ""refresh_token"": ""invalid-refresh-token"",
                ""user"": {
                    ""id"": ""mock-user-id"",
                    ""email"": ""test@example.com"",
                    ""user_metadata"": {}
                }
            }";
            mockClient.SetMockResponse("/auth/v1/token?grant_type=password", mockSignInResponse);
            authService.SignIn("test@example.com", "password123").Wait();
            
            // 토큰 갱신 실패 설정
            var mockException = new SupabaseException("Invalid refresh token", 401, "AUTH_INVALID_REFRESH_TOKEN");
            mockClient.SetMockException("/auth/v1/token?grant_type=refresh_token", mockException);
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<SupabaseException>(async () => await authService.RefreshToken());
            Assert.AreEqual("AUTH_INVALID_REFRESH_TOKEN", ex.ErrorCode);
            
            // 인증 상태가 초기화되었는지 확인
            Assert.IsFalse(authService.IsAuthenticated);
            Assert.IsNull(authService.CurrentUser);
        }
        
        [Test]
        public void RefreshToken_WithNetworkTimeout_ThrowsTimeoutException()
        {
            // Arrange
            // 먼저 로그인하여 사용자 설정
            string mockSignInResponse = @"{
                ""access_token"": ""old-access-token"",
                ""refresh_token"": ""old-refresh-token"",
                ""user"": {
                    ""id"": ""mock-user-id"",
                    ""email"": ""test@example.com"",
                    ""user_metadata"": {}
                }
            }";
            mockClient.SetMockResponse("/auth/v1/token?grant_type=password", mockSignInResponse);
            authService.SignIn("test@example.com", "password123").Wait();
            
            // 네트워크 지연 시뮬레이션 - MockSupabaseClient에 지연 설정 메서드가 있다고 가정
            mockClient.SetEndpointDelay("/auth/v1/token?grant_type=refresh_token", 15000); // 15초 지연 (타임아웃보다 길게)
            mockClient.SetSimulateNetworkDelay(true);
            
            // Act & Assert
            var ex = Assert.ThrowsAsync<SupabaseException>(async () => await authService.RefreshToken());
            Assert.AreEqual("AUTH_REFRESH_TIMEOUT", ex.ErrorCode);
        }
    }
} 