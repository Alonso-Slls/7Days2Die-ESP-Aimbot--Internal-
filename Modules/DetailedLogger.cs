using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Game_7D2D.Modules
{
    /// <summary>
    /// Enhanced logging system for detailed injection and runtime debugging.
    /// Provides comprehensive logging for DLL injection, initialization, and runtime events.
    /// </summary>
    public static class DetailedLogger
    {
        private static string LogPath => Path.Combine(Application.dataPath, "..", "mod_detailed.log");
        private static string InjectionLogPath => Path.Combine(Application.dataPath, "..", "injection_log.txt");
        private static List<LogEntry> _allLogs = new List<LogEntry>();
        private static readonly object _lockObject = new object();
        
        [Serializable]
        public class LogEntry
        {
            public DateTime Timestamp;
            public LogLevel Level;
            public string Message;
            public string Context;
            public string StackTrace;
            public string ThreadInfo;
            
            public LogEntry(LogLevel level, string message, string context = "", string stackTrace = "")
            {
                Timestamp = DateTime.Now;
                Level = level;
                Message = message;
                Context = context;
                StackTrace = stackTrace;
                ThreadInfo = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
            }
            
            public string Format()
            {
                return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [T{ThreadInfo}] [{Context}] {Message}" +
                       (string.IsNullOrEmpty(StackTrace) ? "" : $"\nStackTrace: {StackTrace}");
            }
        }
        
        public enum LogLevel
        {
            DEBUG = 0,
            INFO = 1,
            WARNING = 2,
            ERROR = 3,
            CRITICAL = 4
        }
        
        /// <summary>
        /// Initialize the detailed logging system.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                Log(LogLevel.INFO, "DetailedLogger", "=== MOD INITIALIZATION STARTED ===");
                Log(LogLevel.INFO, "DetailedLogger", $"Game Version: {Application.version}");
                Log(LogLevel.INFO, "DetailedLogger", $"Unity Version: {Application.unityVersion}");
                Log(LogLevel.INFO, "DetailedLogger", $"Data Path: {Application.dataPath}");
                Log(LogLevel.INFO, "DetailedLogger", $"Log File: {LogPath}");
                
                // Check if this is a fresh injection
                CreateInjectionMarker();
            }
            catch (Exception ex)
            {
                Log(LogLevel.CRITICAL, "DetailedLogger", $"Failed to initialize: {ex.Message}", ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Create injection marker file.
        /// </summary>
        private static void CreateInjectionMarker()
        {
            try
            {
                string marker = $"=== DLL INJECTION at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n";
                marker += $"Process: {System.Diagnostics.Process.GetCurrentProcess().ProcessName}\n";
                marker += $"PID: {System.Diagnostics.Process.GetCurrentProcess().Id}\n";
                marker += $"Working Directory: {Environment.CurrentDirectory}\n";
                marker += $"Command Line: {Environment.CommandLine}\n";
                marker += "=== INJECTION DETAILS ===\n";
                
                File.WriteAllText(InjectionLogPath, marker);
                Log(LogLevel.INFO, "DetailedLogger", "Injection marker created");
            }
            catch (Exception ex)
            {
                Log(LogLevel.ERROR, "DetailedLogger", $"Failed to create injection marker: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Log a message with detailed context.
        /// </summary>
        public static void Log(LogLevel level, string context, string message, string stackTrace = "")
        {
            lock (_lockObject)
            {
                try
                {
                    var entry = new LogEntry(level, message, context, stackTrace);
                    _allLogs.Add(entry);
                    
                    // Keep only recent logs (prevent memory issues)
                    if (_allLogs.Count > 5000)
                    {
                        _allLogs.RemoveAt(0);
                    }
                    
                    // Write to file immediately for critical errors
                    if (level >= LogLevel.ERROR)
                    {
                        WriteToFile(entry);
                    }
                    
                    // Also log to Unity console
                    string unityMessage = $"[MOD] [{context}] {message}";
                    switch (level)
                    {
                        case LogLevel.DEBUG:
                        case LogLevel.INFO:
                            Debug.Log(unityMessage);
                            break;
                        case LogLevel.WARNING:
                            Debug.LogWarning(unityMessage);
                            break;
                        case LogLevel.ERROR:
                        case LogLevel.CRITICAL:
                            Debug.LogError(unityMessage);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Fallback logging
                    Debug.LogError($"[DETAILED_LOGGER_ERROR] {ex.Message}");
                }
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
                Debug.LogError($"[DETAILED_LOGGER] Failed to write to file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Flush all logs to file.
        /// </summary>
        public static void FlushToFile()
        {
            lock (_lockObject)
            {
                try
                {
                    using (var writer = new StreamWriter(LogPath, append: true))
                    {
                        foreach (var entry in _allLogs.Where(e => e.Level >= LogLevel.WARNING))
                        {
                            writer.WriteLine(entry.Format());
                        }
                    }
                    Log(LogLevel.INFO, "DetailedLogger", $"Flushed {_allLogs.Count(e => e.Level >= LogLevel.WARNING)} entries to file");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DETAILED_LOGGER] Failed to flush logs: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Get recent logs by level.
        /// </summary>
        public static List<LogEntry> GetRecentLogs(LogLevel minLevel = LogLevel.DEBUG, int count = 100)
        {
            lock (_lockObject)
            {
                return _allLogs
                    .Where(e => e.Level >= minLevel)
                    .TakeLast(count)
                    .ToList();
            }
        }
        
        /// <summary>
        /// Get logs by context.
        /// </summary>
        public static List<LogEntry> GetLogsByContext(string context, LogLevel minLevel = LogLevel.DEBUG)
        {
            lock (_lockObject)
            {
                return _allLogs
                    .Where(e => e.Level >= minLevel && 
                               (string.IsNullOrEmpty(context) || e.Context.Contains(context)))
                    .ToList();
            }
        }
        
        /// <summary>
        /// Export logs to a formatted text file.
        /// </summary>
        public static void ExportLogs(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(Application.dataPath, "..", $"mod_export_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            }
            
            lock (_lockObject)
            {
                try
                {
                    using (var writer = new StreamWriter(filePath))
                    {
                        writer.WriteLine("=== MOD DETAILED LOG EXPORT ===");
                        writer.WriteLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        writer.WriteLine($"Total Entries: {_allLogs.Count}");
                        writer.WriteLine();
                        
                        foreach (var entry in _allLogs)
                        {
                            writer.WriteLine(entry.Format());
                            writer.WriteLine();
                        }
                    }
                    
                    Log(LogLevel.INFO, "DetailedLogger", $"Logs exported to: {filePath}");
                }
                catch (Exception ex)
                {
                    Log(LogLevel.ERROR, "DetailedLogger", $"Failed to export logs: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Get system information for debugging.
        /// </summary>
        public static string GetSystemInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== SYSTEM INFORMATION ===");
            info.AppendLine($"OS: {Environment.OSVersion}");
            info.AppendLine($"Processor Count: {Environment.ProcessorCount}");
            info.AppendLine($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
            info.AppendLine($"System Directory: {Environment.SystemDirectory}");
            info.AppendLine($"User Name: {Environment.UserName}");
            info.AppendLine($"Machine Name: {Environment.MachineName}");
            info.AppendLine();
            info.AppendLine("=== UNITY INFORMATION ===");
            info.AppendLine($"Unity Version: {Application.unityVersion}");
            info.AppendLine($"Game Version: {Application.version}");
            info.AppendLine($"Platform: {Application.platform}");
            info.AppendLine($"Data Path: {Application.dataPath}");
            info.AppendLine($"Persistent Path: {Application.persistentDataPath}");
            info.AppendLine($"Temporary Cache Path: {Application.temporaryCachePath}");
            info.AppendLine($"Streaming Assets Path: {Application.streamingAssetsPath}");
            
            return info.ToString();
        }
        
        /// <summary>
        /// Log component initialization.
        /// </summary>
        public static void LogComponentInit(string componentName, bool success = true, string details = "")
        {
            string message = success ? 
                $"Component '{componentName}' initialized successfully" : 
                $"Component '{componentName}' initialization FAILED";
            
            if (!string.IsNullOrEmpty(details))
                message += $": {details}";
            
            Log(success ? LogLevel.INFO : LogLevel.ERROR, "ComponentInit", message);
        }
        
        /// <summary>
        /// Log method entry/exit for debugging.
        /// </summary>
        public static void LogMethodEntry(string className, string methodName, params object[] parameters)
        {
            string paramStr = parameters.Length > 0 ? 
                $"Params: {string.Join(", ", parameters.Select(p => p?.ToString() ?? "null"))}" : 
                "No parameters";
            
            Log(LogLevel.DEBUG, "MethodTrace", $"ENTER {className}.{methodName} - {paramStr}");
        }
        
        public static void LogMethodExit(string className, string methodName, object result = null)
        {
            string resultStr = result != null ? $"Result: {result}" : "Void";
            Log(LogLevel.DEBUG, "MethodTrace", $"EXIT {className}.{methodName} - {resultStr}");
        }
        
        /// <summary>
        /// Cleanup on mod unload.
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                Log(LogLevel.INFO, "DetailedLogger", "=== MOD UNLOAD STARTED ===");
                FlushToFile();
                Log(LogLevel.INFO, "DetailedLogger", "=== MOD UNLOAD COMPLETED ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DETAILED_LOGGER] Cleanup failed: {ex.Message}");
            }
        }
    }
}
