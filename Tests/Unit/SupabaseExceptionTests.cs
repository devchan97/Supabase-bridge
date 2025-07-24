using System;
using NUnit.Framework;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Tests.Unit
{
    public class SupabaseExceptionTests
    {
        [Test]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Arrange
            string message = "Test error message";
            int statusCode = 404;
            string errorCode = "NOT_FOUND";
            
            // Act
            var exception = new SupabaseException(message, statusCode, errorCode);
            
            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(statusCode, exception.StatusCode);
            Assert.AreEqual(errorCode, exception.ErrorCode);
            Assert.AreEqual(ErrorCategory.NotFound, exception.Category);
        }
        
        [Test]
        public void Constructor_WithInnerException_SetsInnerException()
        {
            // Arrange
            string message = "Test error message";
            int statusCode = 500;
            string errorCode = "SERVER_ERROR";
            var innerException = new Exception("Inner exception");
            
            // Act
            var exception = new SupabaseException(message, statusCode, errorCode, innerException);
            
            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(statusCode, exception.StatusCode);
            Assert.AreEqual(errorCode, exception.ErrorCode);
            Assert.AreEqual(innerException, exception.InnerException);
            Assert.AreEqual(ErrorCategory.ServerError, exception.Category);
        }
        
        [Test]
        public void GetUserFriendlyMessage_ReturnsFormattedMessage()
        {
            // Arrange
            string message = "Test error message";
            int statusCode = 401;
            string errorCode = "AUTH_INVALID_CREDENTIALS";
            var exception = new SupabaseException(message, statusCode, errorCode);
            
            // Act
            string friendlyMessage = exception.GetUserFriendlyMessage();
            
            // Assert
            Assert.IsTrue(friendlyMessage.Contains("인증 오류"));
            Assert.IsTrue(friendlyMessage.Contains(message));
        }
        
        [Test]
        public void Category_WithAuthStatusCode_ReturnsAuthenticationCategory()
        {
            // Arrange & Act
            var exception401 = new SupabaseException("Unauthorized", 401, "AUTH_ERROR");
            var exception403 = new SupabaseException("Forbidden", 403, "AUTH_ERROR");
            
            // Assert
            Assert.AreEqual(ErrorCategory.Authentication, exception401.Category);
            Assert.AreEqual(ErrorCategory.Authentication, exception403.Category);
        }
        
        [Test]
        public void Category_WithNotFoundStatusCode_ReturnsNotFoundCategory()
        {
            // Arrange & Act
            var exception = new SupabaseException("Not Found", 404, "NOT_FOUND");
            
            // Assert
            Assert.AreEqual(ErrorCategory.NotFound, exception.Category);
        }
        
        [Test]
        public void Category_WithClientErrorStatusCode_ReturnsClientErrorCategory()
        {
            // Arrange & Act
            var exception = new SupabaseException("Bad Request", 400, "BAD_REQUEST");
            
            // Assert
            Assert.AreEqual(ErrorCategory.ClientError, exception.Category);
        }
        
        [Test]
        public void Category_WithServerErrorStatusCode_ReturnsServerErrorCategory()
        {
            // Arrange & Act
            var exception = new SupabaseException("Internal Server Error", 500, "SERVER_ERROR");
            
            // Assert
            Assert.AreEqual(ErrorCategory.ServerError, exception.Category);
        }
        
        [Test]
        public void Category_WithClientSideErrorCode_ReturnsAppropriateCategory()
        {
            // Arrange & Act
            var authException = new SupabaseException("Auth Error", 0, "AUTH_ERROR");
            var dbException = new SupabaseException("DB Error", 0, "DB_ERROR");
            var storageException = new SupabaseException("Storage Error", 0, "STORAGE_ERROR");
            var configException = new SupabaseException("Config Error", 0, "CONFIG_ERROR");
            var networkException = new SupabaseException("Network Error", 0, "NETWORK_ERROR");
            var parseException = new SupabaseException("Parse Error", 0, "PARSE_ERROR");
            
            // Assert
            Assert.AreEqual(ErrorCategory.Authentication, authException.Category);
            Assert.AreEqual(ErrorCategory.Database, dbException.Category);
            Assert.AreEqual(ErrorCategory.Storage, storageException.Category);
            Assert.AreEqual(ErrorCategory.Configuration, configException.Category);
            Assert.AreEqual(ErrorCategory.Network, networkException.Category);
            Assert.AreEqual(ErrorCategory.Parsing, parseException.Category);
        }
        
        [Test]
        public void Category_WithUnknownErrorCode_ReturnsUnknownCategory()
        {
            // Arrange & Act
            var exception = new SupabaseException("Unknown Error", 0, "UNKNOWN_ERROR");
            
            // Assert
            Assert.AreEqual(ErrorCategory.Unknown, exception.Category);
        }
    }
} 