using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SupabaseBridge.Runtime;
using UnityEngine;

namespace SupabaseBridge.Tests.Unit
{
    public class SupabaseLoggerTests
    {
        private string testLogFilePath;
        
        [SetUp]
        public void SetUp()
        {
            // Clear log history before each test
            SupabaseLogger.ClearLogHistory();
            
            // Set up a test log file path
            testLogFilePath = Path.Combine(Application.temporaryCachePath, "SupabaseTestLog.txt");
            
            // Delete the test log file if it exists
            if (File.Exists(testLogFilePath))
            {
                File.Delete(testLogFilePath);
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            if (File.Exists(testLogFilePath))
            {
                File.Delete(testLogFilePath);
            }
        }
        
        [Test]
        public void Initialize_SetsLogLevelAndFileLogging()
        {
            // Act
            SupabaseLogger.Initialize(LogLevel.Debug, true, testLogFilePath);
            
            // Assert
            Assert.AreEqual(LogLevel.Debug, SupabaseLogger.LogLevel);
            Assert.IsTrue(SupabaseLogger.LogToFile);
            Assert.AreEqual(testLogFilePath, SupabaseLogger.LogFilePath);
        }
        
        [Test]
        public void LogDebug_WithDebugLevel_AddsToLogHistory()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Debug);
            string message = "Test debug message";
            string context = "TestContext";
            
            // Act
            SupabaseLogger.LogDebug(message, context);
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Debug, logEntry.Level);
            Assert.AreEqual(message, logEntry.Message);
            Assert.AreEqual(context, logEntry.Context);
        }
        
        [Test]
        public void LogDebug_WithInfoLevel_DoesNotAddToLogHistory()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Info);
            
            // Act
            SupabaseLogger.LogDebug("Test debug message");
            
            // Assert
            Assert.AreEqual(0, SupabaseLogger.LogHistory.Count);
        }
        
        [Test]
        public void Info_WithInfoLevel_AddsToLogHistory()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Info);
            string message = "Test info message";
            
            // Act
            SupabaseLogger.Info(message);
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Info, logEntry.Level);
            Assert.AreEqual(message, logEntry.Message);
            Assert.AreEqual("Supabase", logEntry.Context);
        }
        
        [Test]
        public void Warning_WithWarningLevel_AddsToLogHistory()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Warning);
            string message = "Test warning message";
            
            // Act
            SupabaseLogger.Warning(message);
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Warning, logEntry.Level);
            Assert.AreEqual(message, logEntry.Message);
        }
        
        [Test]
        public void Error_WithErrorLevel_AddsToLogHistory()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Error);
            string message = "Test error message";
            
            // Act
            SupabaseLogger.Error(message);
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Error, logEntry.Level);
            Assert.AreEqual(message, logEntry.Message);
        }
        
        [Test]
        public void Exception_WithSupabaseException_LogsUserFriendlyMessage()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Error);
            var exception = new SupabaseException("Test exception", 404, "NOT_FOUND");
            
            // Act
            SupabaseLogger.Exception(exception);
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Error, logEntry.Level);
            Assert.IsTrue(logEntry.Message.Contains("리소스를 찾을 수 없음"));
            Assert.IsTrue(logEntry.Message.Contains("Test exception"));
        }
        
        [Test]
        public void Exception_WithStandardException_LogsMessageAndStackTrace()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Error);
            var exception = new Exception("Standard exception");
            
            // Act
            SupabaseLogger.Exception(exception);
            
            // Assert
            Assert.AreEqual(1, SupabaseLogger.LogHistory.Count);
            var logEntry = SupabaseLogger.LogHistory[0];
            Assert.AreEqual(LogLevel.Error, logEntry.Level);
            Assert.IsTrue(logEntry.Message.Contains("Standard exception"));
        }
        
        [Test]
        public void ClearLogHistory_RemovesAllEntries()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Debug);
            SupabaseLogger.LogDebug("Test message 1");
            SupabaseLogger.Info("Test message 2");
            Assert.AreEqual(2, SupabaseLogger.LogHistory.Count);
            
            // Act
            SupabaseLogger.ClearLogHistory();
            
            // Assert
            Assert.AreEqual(0, SupabaseLogger.LogHistory.Count);
        }
        
        [Test]
        public void GetLogHistoryAsString_ReturnsFormattedHistory()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Debug);
            SupabaseLogger.LogDebug("Debug message");
            SupabaseLogger.Info("Info message");
            
            // Act
            string history = SupabaseLogger.GetLogHistoryAsString();
            
            // Assert
            Assert.IsTrue(history.Contains("[Debug]"));
            Assert.IsTrue(history.Contains("Debug message"));
            Assert.IsTrue(history.Contains("[Info]"));
            Assert.IsTrue(history.Contains("Info message"));
        }
        
        [Test]
        public void LogToFile_WritesToSpecifiedFile()
        {
            // Arrange
            SupabaseLogger.Initialize(LogLevel.Info, true, testLogFilePath);
            
            // Act
            SupabaseLogger.Info("Test file log message");
            
            // Assert
            Assert.IsTrue(File.Exists(testLogFilePath));
            string fileContent = File.ReadAllText(testLogFilePath);
            Assert.IsTrue(fileContent.Contains("Test file log message"));
        }
    }
} 