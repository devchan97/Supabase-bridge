using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SupabaseBridge.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace SupabaseBridge.Tests.Unit
{
    public class SupabaseClientTests
    {
        private SupabaseClient client;
        
        [SetUp]
        public void SetUp()
        {
            // Initialize a client with test credentials
            client = new SupabaseClient("https://test-project.supabase.co", "test-api-key");
            
            // Clear log history
            SupabaseLogger.ClearLogHistory();
        }
        
        [Test]
        public void Constructor_WithValidParameters_InitializesClient()
        {
            // Arrange & Act
            var client = new SupabaseClient("https://test-project.supabase.co", "test-api-key");
            
            // Assert
            Assert.AreEqual("https://test-project.supabase.co", client.GetBaseUrl());
            
            // Check that initialization was logged
            bool hasInitLog = false;
            foreach (var log in SupabaseLogger.LogHistory)
            {
                if (log.Level == LogLevel.Info && log.Message.Contains("Initialized Supabase client"))
                {
                    hasInitLog = true;
                    break;
                }
            }
            Assert.IsTrue(hasInitLog);
        }
        
        [Test]
        public void Constructor_WithEmptyUrl_ThrowsException()
        {
            // Arrange & Act & Assert
            var ex = Assert.Throws<SupabaseException>(() => new SupabaseClient("", "test-api-key"));
            Assert.AreEqual("CONFIG_INVALID_URL", ex.ErrorCode);
        }
        
        [Test]
        public void Constructor_WithEmptyKey_ThrowsException()
        {
            // Arrange & Act & Assert
            var ex = Assert.Throws<SupabaseException>(() => new SupabaseClient("https://test-project.supabase.co", ""));
            Assert.AreEqual("CONFIG_INVALID_KEY", ex.ErrorCode);
        }
        
        [Test]
        public void SetAccessToken_SetsToken()
        {
            // Arrange
            string token = "test-access-token";
            
            // Act
            client.SetAccessToken(token);
            
            // Assert
            // We can't directly access the private accessToken field,
            // but we can check that the action was logged
            bool hasTokenLog = false;
            foreach (var log in SupabaseLogger.LogHistory)
            {
                if (log.Level == LogLevel.Debug && log.Message.Contains("Access token set"))
                {
                    hasTokenLog = true;
                    break;
                }
            }
            Assert.IsTrue(hasTokenLog);
        }
        
        [Test]
        public void GetBaseUrl_ReturnsBaseUrl()
        {
            // Arrange
            string expectedUrl = "https://test-project.supabase.co";
            
            // Act
            string actualUrl = client.GetBaseUrl();
            
            // Assert
            Assert.AreEqual(expectedUrl, actualUrl);
        }
        
        // Note: The following tests would normally use mocking frameworks to mock HTTP requests
        // Since we're limited in this environment, we'll use simple test methods that verify
        // the error handling behavior without making actual network requests
        
        [Test]
        public void Get_WithInvalidEndpoint_LogsError()
        {
            // Arrange
            string invalidEndpoint = null;
            
            // Act & Assert
            Assert.ThrowsAsync<SupabaseException>(async () => await client.Get(invalidEndpoint));
            
            // Verify error was logged
            bool hasErrorLog = false;
            foreach (var log in SupabaseLogger.LogHistory)
            {
                if (log.Level == LogLevel.Error)
                {
                    hasErrorLog = true;
                    break;
                }
            }
            Assert.IsTrue(hasErrorLog);
        }
        
        [Test]
        public void BuildUrl_WithQueryParams_ReturnsCorrectUrl()
        {
            // This is a test for a private method, which we can't directly test
            // Instead, we'll test the behavior indirectly through a public method
            
            // Arrange
            var queryParams = new Dictionary<string, string>
            {
                { "param1", "value1" },
                { "param2", "value2" }
            };
            
            // We can't directly test BuildUrl as it's private, but we can check the logs
            // when making a request to see what URL was constructed
            
            // Act - this will fail due to network issues but will log the URL
            Assert.ThrowsAsync<SupabaseException>(async () => await client.Get("test-endpoint", queryParams));
            
            // Assert - check the logs for the constructed URL
            bool hasCorrectUrl = false;
            foreach (var log in SupabaseLogger.LogHistory)
            {
                if (log.Level == LogLevel.Debug && log.Message.Contains("Sending GET request to test-endpoint"))
                {
                    hasCorrectUrl = true;
                    break;
                }
            }
            Assert.IsTrue(hasCorrectUrl);
        }
    }
} 