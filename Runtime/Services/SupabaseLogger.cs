using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SupabaseBridge.Runtime
{
    /// <summary>
    /// Provides logging functionality for Supabase operations.
    /// </summary>
    public static class SupabaseLogger
    {
        private static LogLevel _logLevel = LogLevel.Info;
        private static bool _logToFile = false;
        private static string _logFilePath = "";
        private static readonly List<LogEntry> _logHistory = new List<LogEntry>();
        private static readonly int _maxLogHistorySize = 1000;
        
        /// <summary>
        /// Gets or sets the current log level.
        /// </summary>
        public static LogLevel LogLevel
        {
            get => _logLevel;
            set => _logLevel = value;
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether to log to a file.
        /// </summary>
        public static bool LogToFile
        {
            get => _logToFile;
            set => _logToFile = value;
        }
        
        /// <summary>
        /// Gets or sets the log file path.
        /// </summary>
        public static string LogFilePath
        {
            get => _logFilePath;
            set => _logFilePath = value;
        }
        
        /// <summary>
        /// Gets the log history.
        /// </summary>
        public static IReadOnlyList<LogEntry> LogHistory => _logHistory.AsReadOnly();
        
        /// <summary>
        /// Initializes the logger.
        /// </summary>
        /// <param name="logLevel">The log level</param>
        /// <param name="logToFile">Whether to log to a file</param>
        /// <param name="logFilePath">The log file path</param>
        public static void Initialize(LogLevel logLevel = LogLevel.Info, bool logToFile = false, string logFilePath = "")
        {
            _logLevel = logLevel;
            _logToFile = logToFile;
            
            if (logToFile)
            {
                if (string.IsNullOrEmpty(logFilePath))
                {
                    _logFilePath = Path.Combine(Application.persistentDataPath, "SupabaseLogs.txt");
                }
                else
                {
                    _logFilePath = logFilePath;
                }
                
                // Create or clear the log file
                try
                {
                    File.WriteAllText(_logFilePath, $"Supabase Bridge Log - Started at {DateTime.Now}\n\n");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to initialize log file: {ex.Message}");
                    _logToFile = false;
                }
            }
        }
        
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">The context of the log</param>
        public static void LogDebug(string message, string context = "Supabase")
        {
            if (_logLevel <= LogLevel.Debug)
            {
                Log(LogLevel.Debug, message, context);
            }
        }
        
        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">The context of the log</param>
        public static void Info(string message, string context = "Supabase")
        {
            if (_logLevel <= LogLevel.Info)
            {
                Log(LogLevel.Info, message, context);
            }
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">The context of the log</param>
        public static void Warning(string message, string context = "Supabase")
        {
            if (_logLevel <= LogLevel.Warning)
            {
                Log(LogLevel.Warning, message, context);
            }
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">The context of the log</param>
        public static void Error(string message, string context = "Supabase")
        {
            if (_logLevel <= LogLevel.Error)
            {
                Log(LogLevel.Error, message, context);
            }
        }
        
        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="exception">The exception to log</param>
        /// <param name="context">The context of the log</param>
        public static void Exception(Exception exception, string context = "Supabase")
        {
            if (_logLevel <= LogLevel.Error)
            {
                string message;
                
                if (exception is SupabaseException supabaseEx)
                {
                    message = $"{supabaseEx.GetUserFriendlyMessage()} (StatusCode: {supabaseEx.StatusCode}, ErrorCode: {supabaseEx.ErrorCode})";
                }
                else
                {
                    message = $"{exception.Message}\n{exception.StackTrace}";
                }
                
                Log(LogLevel.Error, message, context);
            }
        }
        
        /// <summary>
        /// Clears the log history.
        /// </summary>
        public static void ClearLogHistory()
        {
            _logHistory.Clear();
        }
        
        /// <summary>
        /// Gets the log history as a string.
        /// </summary>
        /// <returns>The log history as a string</returns>
        public static string GetLogHistoryAsString()
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (var entry in _logHistory)
            {
                sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] [{entry.Context}] {entry.Message}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="message">The message to log</param>
        /// <param name="context">The context of the log</param>
        private static void Log(LogLevel level, string message, string context)
        {
            // Create log entry
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Context = context
            };
            
            // Add to history
            _logHistory.Add(entry);
            
            // Trim history if needed
            if (_logHistory.Count > _maxLogHistorySize)
            {
                _logHistory.RemoveAt(0);
            }
            
            // Log to Unity console
            string logMessage = $"[{context}] {message}";
            
            switch (level)
            {
                case LogLevel.Debug:
                    UnityEngine.Debug.Log(logMessage);
                    break;
                case LogLevel.Info:
                    UnityEngine.Debug.Log(logMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(logMessage);
                    break;
            }
            
            // Log to file if enabled
            if (_logToFile && !string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    string logLine = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] [{entry.Context}] {entry.Message}\n";
                    File.AppendAllText(_logFilePath, logLine);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to write to log file: {ex.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// Represents a log entry.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the log level of the log entry.
        /// </summary>
        public LogLevel Level { get; set; }
        
        /// <summary>
        /// Gets or sets the message of the log entry.
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the context of the log entry.
        /// </summary>
        public string Context { get; set; }
    }
    
    /// <summary>
    /// Represents the log level.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level - most verbose.
        /// </summary>
        Debug = 0,
        
        /// <summary>
        /// Info level - normal operation information.
        /// </summary>
        Info = 1,
        
        /// <summary>
        /// Warning level - potential issues.
        /// </summary>
        Warning = 2,
        
        /// <summary>
        /// Error level - operation failures.
        /// </summary>
        Error = 3,
        
        /// <summary>
        /// None level - no logging.
        /// </summary>
        None = 4
    }
} 