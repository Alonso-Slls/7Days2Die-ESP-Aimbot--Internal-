using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Comprehensive error handling and logging system.
    /// Provides structured logging, crash recovery, and graceful degradation.
    /// </summary>
    public static class ErrorHandler
    {
        private static string LogPath => Path.Combine(@"C:\Users\anoni\OneDrive\Escritorio\SharpMonoInjector.Console\logs", "mod_errors.log");
        private static Queue<LogEntry> _logQueue = new Queue<LogEntry>();
        private static readonly object _lockObject = new object();
        
        // Error tracking
        private static int _errorCount;
        private static int _warningCount;
        private static DateTime _lastError;
        
        // Configuration
        private const int MAX_LOG_ENTRIES = 1000;
        private const int MAX_LOG_FILE_SIZE = 1024 * 1024; // 1MB
        
        /// <summary>
        /// Log entry structure for structured logging.
        /// </summary>
        [Serializable]
        public class LogEntry
        {
            public DateTime Timestamp;
            public LogLevel Level;
            public string Message;
            public string StackTrace;
            public string Context;
            
            public LogEntry(LogLevel level, string message, string context = "", string stackTrace = "")
            {
                Timestamp = DateTime.Now;
                Level = level;
                Message = message;
                StackTrace = stackTrace;
                Context = context;
            }
            
            public string Format()
            {
                return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level}] [{Context}] {Message}\n{StackTrace}";
            }
        }
        
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error,
            Critical
        }
        
        /// <summary>
        /// Initialize error handling system.
        /// </summary>
        public static void Initialize()
        {
            Application.logMessageReceived += HandleUnityLog;
            
            // Clean up old log file if it's too large
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MAX_LOG_FILE_SIZE)
            {
                try
                {
                    File.Delete(LogPath);
                    LogInfo("ErrorHandler", "Deleted oversized log file");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to delete log file: {ex.Message}");
                }
            }
            
            LogInfo("ErrorHandler", "Error handling system initialized");
        }
        
        /// <summary>
        /// Handle Unity's log messages.
        /// </summary>
        private static void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            LogLevel level = type switch
            {
                LogType.Error => LogLevel.Error,
                LogType.Assert => LogLevel.Error,
                LogType.Warning => LogLevel.Warning,
                LogType.Exception => LogLevel.Critical,
                LogType.Log => LogLevel.Debug,
                _ => LogLevel.Info
            };
            
            Log(level, "Unity", condition, stackTrace);
        }
        
        /// <summary>
        /// Log a debug message.
        /// </summary>
        public static void LogDebug(string context, string message)
        {
            Log(LogLevel.Debug, context, message);
        }
        
        /// <summary>
        /// Log an info message.
        /// </summary>
        public static void LogInfo(string context, string message)
        {
            Log(LogLevel.Info, context, message);
        }
        
        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void LogWarning(string context, string message)
        {
            Log(LogLevel.Warning, context, message);
        }
        
        /// <summary>
        /// Log an error message.
        /// </summary>
        public static void LogError(string context, string message)
        {
            Log(LogLevel.Error, context, message);
        }
        
        /// <summary>
        /// Log a critical error message.
        /// </summary>
        public static void LogCritical(string context, string message)
        {
            Log(LogLevel.Critical, context, message);
        }
        
        /// <summary>
        /// Log an exception with full details.
        /// </summary>
        public static void LogException(string context, Exception exception)
        {
            Log(LogLevel.Critical, context, exception.Message, exception.StackTrace);
        }
        
        /// <summary>
        /// Core logging method.
        /// </summary>
        private static void Log(LogLevel level, string context, string message, string stackTrace = "")
        {
            try
            {
                var entry = new LogEntry(level, message, context, stackTrace);
                
                lock (_lockObject)
                {
                    _logQueue.Enqueue(entry);
                    
                    // Update counters
                    if (level >= LogLevel.Error)
                    {
                        _errorCount++;
                        _lastError = DateTime.Now;
                    }
                    else if (level == LogLevel.Warning)
                    {
                        _warningCount++;
                    }
                    
                    // Limit queue size
                    while (_logQueue.Count > MAX_LOG_ENTRIES)
                    {
                        _logQueue.Dequeue();
                    }
                }
                
                // Write to file
                WriteToFile(entry);
                
                // Also log to Unity console for debugging
                if (level >= LogLevel.Warning)
                {
                    Debug.LogError($"[{context}] {message}");
                }
                else if (level == LogLevel.Info)
                {
                    Debug.Log($"[{context}] {message}");
                }
            }
            catch (Exception ex)
            {
                // Last resort - can't log the error itself
                Debug.LogError($"ErrorHandler failed to log: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Write log entry to file.
        /// </summary>
        private static void WriteToFile(LogEntry entry)
        {
            try
            {
                File.AppendAllText(LogPath, entry.Format() + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write to log file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Execute an action with error handling and recovery.
        /// </summary>
        public static T SafeExecute<T>(string context, Func<T> action, T defaultValue = default)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                LogException(context, ex);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Execute an action with error handling.
        /// </summary>
        public static void SafeExecute(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogException(context, ex);
            }
        }
        
        /// <summary>
        /// Get recent log entries.
        /// </summary>
        public static List<LogEntry> GetRecentLogs(int count = 50)
        {
            lock (_lockObject)
            {
                var entries = _logQueue.ToArray();
                return entries.Skip(Math.Max(0, entries.Length - count)).ToList();
            }
        }
        
        /// <summary>
        /// Get error statistics.
        /// </summary>
        public static string GetErrorStats()
        {
            lock (_lockObject)
            {
                return $"Errors: {_errorCount}, Warnings: {_warningCount}, Last Error: {_lastError:HH:mm:ss}";
            }
        }
        
        /// <summary>
        /// Check if too many errors have occurred recently.
        /// </summary>
        public static bool IsErrorThresholdExceeded(int threshold = 10, TimeSpan timeWindow = default)
        {
            if (timeWindow == default)
                timeWindow = TimeSpan.FromMinutes(5);
            
            lock (_lockObject)
            {
                var entries = _logQueue.ToArray();
                var recentErrors = entries.Count(e => 
                    e.Level >= LogLevel.Error && 
                    DateTime.Now - e.Timestamp <= timeWindow);
                
                return recentErrors >= threshold;
            }
        }
        
        /// <summary>
        /// Clear all logs.
        /// </summary>
        public static void ClearLogs()
        {
            lock (_lockObject)
            {
                _logQueue.Clear();
                _errorCount = 0;
                _warningCount = 0;
            }
            
            try
            {
                if (File.Exists(LogPath))
                {
                    File.Delete(LogPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to clear log file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Cleanup on application exit.
        /// </summary>
        public static void Cleanup()
        {
            Application.logMessageReceived -= HandleUnityLog;
            LogInfo("ErrorHandler", "Error handling system shutdown");
        }
    }
}
