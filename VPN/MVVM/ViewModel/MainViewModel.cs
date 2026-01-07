// csharp
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Diagnostics;
using VPN.Core;

namespace VPN.MVVM.ViewModel;

internal class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    private static void DisconnectVpn()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "rasdial.exe",
                Arguments = "/disconnect",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();
            
            MessageBox.Show("VPN déconnecté avec succès.", "Déconnexion", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Erreur lors de la déconnexion : {e.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private static void StopVpnProcess()
    {
        try
        {
            var currentId = Environment.ProcessId;
            var procs = Process.GetProcessesByName("VPN");
            foreach (var p in procs)
            {
                if (p.Id == currentId) continue;

                try
                {
                    if (p.HasExited) continue;

                    // Essaie une fermeture propre
                    if (p.CloseMainWindow())
                    {
                        if (!p.WaitForExit(2000))
                            p.Kill();
                    }
                    else
                    {
                        // Si pas d'interface principale, tue le processus
                        p.Kill();
                    }
                }
                catch
                {
                    // Ignorer les erreurs individuelles de processus
                }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    /* commands */
    public RelayCommand MoveWindowCommand { get; set; }
    public RelayCommand ShutWindow { get; set; }
    public RelayCommand MaxWindow { get; set; }
    public RelayCommand MinWindow { get; set; }
    public RelayCommand ShowProtectionViewCommand { get; set; }
    public RelayCommand ShowSettingsViewCommand { get; set; }
    public RelayCommand DisconnectVpnCommand { get; set; }
    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    private ProtectionViewModel ProtectionVM { get; set; }
    private SettingsViewModel SettingsVM { get; set; }

    public MainViewModel()
    {
        ProtectionVM = new ProtectionViewModel();
        SettingsVM = new SettingsViewModel();
        CurrentView = ProtectionVM;
        
        
        var main = Application.Current?.MainWindow;
        if (main != null)
            main.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

        MoveWindowCommand = new RelayCommand(_ =>
        {
            var wnd = Application.Current?.MainWindow;
            if (wnd == null) return;
            
            if (wnd.WindowState == WindowState.Maximized)
                wnd.WindowState = WindowState.Normal;
            
            try
            {
                wnd.DragMove();
            }
            catch
            {
                // DragMove peut échouer si la souris n'est pas enfoncée
            }
        });
        
        ShutWindow = new RelayCommand(_ =>
        {
            DisconnectVpn();
            StopVpnProcess();
            Application.Current?.MainWindow?.Close();
        });
        
        MaxWindow = new RelayCommand(_ =>
        {
            var wnd = Application.Current?.MainWindow;
            if (wnd == null) return;
            wnd.WindowState = wnd.WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        });
        
        MinWindow = new RelayCommand(_ =>
        {
            var wnd = Application.Current?.MainWindow;
            if (wnd == null) return;
            wnd.WindowState = WindowState.Minimized;
        });
        
        ShowProtectionViewCommand = new RelayCommand(_ =>
        {
            CurrentView = ProtectionVM;
        });
        
        ShowSettingsViewCommand = new RelayCommand(_ =>
        {
            CurrentView = SettingsVM;
        });
        
        DisconnectVpnCommand = new RelayCommand(_ =>
        {
            DisconnectVpn();
        });
    }
}
