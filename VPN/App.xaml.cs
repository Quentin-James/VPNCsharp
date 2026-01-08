﻿using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using VPN.Core;

namespace VPN;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Enregistrer les gestionnaires d'exceptions globaux
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        
        // Log du démarrage
        Logger.LogInfo("=== Application VPN démarrée ===");
        Logger.LogInfo($"Dossier de logs: {Logger.GetLogFolder()}");
        
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.LogInfo("=== Application VPN fermée ===");
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.LogException(e.Exception, "DispatcherUnhandledException");
        
        MessageBox.Show(
            $"Une erreur inattendue est survenue.\n\nConsultez le fichier de log:\n{Logger.GetLogFilePath()}", 
            "Erreur", 
            MessageBoxButton.OK, 
            MessageBoxImage.Error);
        
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Logger.LogException(ex, "CurrentDomain_UnhandledException");
        }
        else
        {
            Logger.LogError($"CurrentDomain_UnhandledException: {e.ExceptionObject}");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.LogException(e.Exception, "UnobservedTaskException");
        e.SetObserved();
    }
}