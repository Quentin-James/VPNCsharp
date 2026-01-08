using System;
using System.IO;
using System.Text;

namespace VPN.Core;

/// <summary>
/// Service de logging thread-safe pour l'application VPN.
/// Les logs sont stockés dans %APPDATA%/VPNApp/logs/
/// </summary>
public static class Logger
{
    private static readonly object _lock = new();
    
    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VPNApp", "logs");
    
    private static readonly string LogFile = Path.Combine(LogFolder, "vpn.log");
    
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB max par fichier

    /// <summary>
    /// Écrit un message dans le fichier de log avec horodatage.
    /// </summary>
    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(LogFolder);
                RotateIfNeeded();
                
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var line = $"[{timestamp}] {message}{Environment.NewLine}";
                
                File.AppendAllText(LogFile, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Ne pas lever d'exception depuis le logger
        }
    }

    /// <summary>
    /// Log une exception avec son contexte.
    /// </summary>
    public static void LogException(Exception ex, string? context = null)
    {
        var msg = string.IsNullOrEmpty(context) 
            ? $"EXCEPTION: {ex.GetType().FullName} - {ex.Message}" 
            : $"EXCEPTION [{context}]: {ex.GetType().FullName} - {ex.Message}";
        
        Log(msg);
        Log($"StackTrace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            Log($"InnerException: {ex.InnerException.Message}");
        }
    }

    /// <summary>
    /// Log la sortie d'un processus (stdout/stderr).
    /// </summary>
    public static void LogProcess(string processName, string arguments, int exitCode, string? stdout, string? stderr)
    {
        Log($"PROCESS: {processName} {arguments}");
        Log($"  ExitCode: {exitCode}");
        
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Log($"  stdout: {stdout.Trim()}");
        }
        
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Log($"  stderr: {stderr.Trim()}");
        }
    }

    /// <summary>
    /// Log un message d'information.
    /// </summary>
    public static void LogInfo(string message)
    {
        Log($"INFO: {message}");
    }

    /// <summary>
    /// Log un message d'erreur.
    /// </summary>
    public static void LogError(string message)
    {
        Log($"ERROR: {message}");
    }

    /// <summary>
    /// Log un message de warning.
    /// </summary>
    public static void LogWarning(string message)
    {
        Log($"WARN: {message}");
    }

    /// <summary>
    /// Retourne le chemin du dossier de logs.
    /// </summary>
    public static string GetLogFolder() => LogFolder;

    /// <summary>
    /// Retourne le chemin du fichier de log actuel.
    /// </summary>
    public static string GetLogFilePath() => LogFile;

    /// <summary>
    /// Rotation du fichier de log si trop volumineux.
    /// </summary>
    private static void RotateIfNeeded()
    {
        try
        {
            if (!File.Exists(LogFile)) return;
            
            var fileInfo = new FileInfo(LogFile);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                var archiveName = $"vpn_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var archivePath = Path.Combine(LogFolder, archiveName);
                File.Move(LogFile, archivePath);
            }
        }
        catch
        {
            // Ignore rotation errors
        }
    }
}

