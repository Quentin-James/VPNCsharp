// csharp
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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

    /* commands */
    public RelayCommand MoveWindowCommand { get; set; }
    public RelayCommand ShutWindow { get; set; }
    public RelayCommand MaxWindow { get; set; }
    public RelayCommand MinWindow { get; set; }
    public RelayCommand ShowProtectionViewCommand { get; set; }
    public RelayCommand ShowSettingsViewCommand { get; set; }

    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }
    
    public ProtectionViewModel ProtectionVM { get; set; }
    public SettingsViewModel SettingsVM { get; set; }

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
    }
}
