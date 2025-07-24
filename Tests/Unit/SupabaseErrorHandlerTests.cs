using System;
using NUnit.Framework;
using SupabaseBridge.Runtime;

namespace SupabaseBridge.Tests.Unit
{
    public class SupabaseErrorHandlerTests
    {
        [SetUp]
        public void SetUp()
        {
            // Initialize the error handler before each test
            SupabaseErrorHandler.Initialize();
            
            // Clear the log history
            SupabaseLogger.ClearLogHistory();
        }
        
        [Test]
        public void HandleException_WithSupabaseException_ReturnsUserFriendlyMessage()
        {
            // Arrange
            var exception = new SupabaseException("Test exception", 404, "NOT_FOUND");
            
            // Act
            string message = SupabaseErrorHandler.HandleException(exception);
            
            // Assert
            Assert.IsTrue(message.Contains("리소스를 찾을 수 없음"));
        }
        
        [Test]
        public void HandleException_WithStandardException_ReturnsGenericMessage()
        {
            // Arrange
            var exception = new Exception("Standard exception");
            
            // Act
            string message = SupabaseErrorHandler.HandleException(exception);
            
            // Assert
            Assert.IsTrue(message.Contains("오류가 발생했습니다"));
            Assert.IsTrue(message.Contains("Standard exception"));
        }
        
        [Test]
        public void HandleException_LogsException()
        {
            // Arrange
            var exception = new Exception("Test exception");
            
            // Act
            SupabaseErrorHandler.HandleException(exception);
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Error, logEntry.Level);
            Assert.IsTrue(logEntry.Message.Contains("Test exception"));
        }
        
        [Test]
        public void HandleException_WithCallback_InvokesCallback()
        {
            // Arrange
            var exception = new SupabaseException("Test exception", 401, "AUTH_ERROR");
            string callbackMessage = null;
            ErrorCategory callbackCategory = ErrorCategory.Unknown;
            
            SupabaseErrorHandler.SetErrorCallback((message, category) => {
                callbackMessage = message;
                callbackCategory = category;
            });
            
            // Act
            SupabaseErrorHandler.HandleException(exception);
            
            // Assert
            Assert.IsNotNull(callbackMessage);
            Assert.IsTrue(callbackMessage.Contains("인증 오류"));
            Assert.AreEqual(ErrorCategory.Authentication, callbackCategory);
        }
        
        [Test]
        public void ShowError_LogsErrorMessage()
        {
            // Arrange
            string errorMessage = "Test error message";
            
            // Act
            SupabaseErrorHandler.ShowError(errorMessage, ErrorCategory.Database);
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Error, logEntry.Level);
            Assert.AreEqual(errorMessage, logEntry.Message);
            Assert.AreEqual("Database", logEntry.Context);
        }
        
        [Test]
        public void ShowError_WithCallback_InvokesCallback()
        {
            // Arrange
            string errorMessage = "Test error message";
            string callbackMessage = null;
            ErrorCategory callbackCategory = ErrorCategory.Unknown;
            
            SupabaseErrorHandler.SetErrorCallback((message, category) => {
                callbackMessage = message;
                callbackCategory = category;
            });
            
            // Act
            SupabaseErrorHandler.ShowError(errorMessage, ErrorCategory.Storage);
            
            // Assert
            Assert.AreEqual(errorMessage, callbackMessage);
            Assert.AreEqual(ErrorCategory.Storage, callbackCategory);
        }
        
        [Test]
        public void ShowWarning_LogsWarningMessage()
        {
            // Arrange
            string warningMessage = "Test warning message";
            
            // Act
            SupabaseErrorHandler.ShowWarning(warningMessage, "TestContext");
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Warning, logEntry.Level);
            Assert.AreEqual(warningMessage, logEntry.Message);
            Assert.AreEqual("TestContext", logEntry.Context);
        }
    }
} 