# Surfhub VPN Client — Complete Technical Documentation

> **Application Name:** Surfhub  
> **Technology:** WPF (.NET 10), C#, MVVM Architecture  
> **Purpose:** Desktop VPN client for Windows with server management and PPTP connection support  
> **Repository:** `C:\git\Projets\VPN\VPN\VPN`

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture & Design Patterns](#2-architecture--design-patterns)
3. [Project Structure](#3-project-structure)
4. [Core Layer — Detailed Analysis](#4-core-layer--detailed-analysis)
5. [Model Layer — Data Structures](#5-model-layer--data-structures)
6. [ViewModel Layer — Business Logic](#6-viewmodel-layer--business-logic)
7. [View Layer — User Interface](#7-view-layer--user-interface)
8. [Data Persistence](#8-data-persistence)
9. [VPN Connection Logic](#9-vpn-connection-logic)
10. [Build, Run & Publish](#10-build-run--publish)
11. [Troubleshooting Guide](#11-troubleshooting-guide)

---

## 1. Project Overview

**Surfhub** is a modern WPF desktop application that allows users to:

- ✅ Manage a list of VPN servers (add, remove, persist)
- ✅ Select country flags for visual identification of servers
- ✅ Connect to VPN servers using Windows built-in PPTP protocol
- ✅ Disconnect from VPN with a single click
- ✅ Auto-save servers to JSON for persistence across sessions

### Key Technologies

| Component | Technology |
|-----------|------------|
| Framework | .NET 10 (Windows) |
| UI Framework | WPF (Windows Presentation Foundation) |
| Architecture | MVVM (Model-View-ViewModel) |
| VPN Protocol | PPTP via `rasdial.exe` and `Add-VpnConnection` |
| Persistence | JSON file in AppData |
| Styling | Custom Nord-themed ResourceDictionaries |

---

## 2. Architecture & Design Patterns

### 2.1 MVVM Pattern Implementation

The application follows the **Model-View-ViewModel (MVVM)** pattern strictly:

```
┌─────────────────────────────────────────────────────────────┐
│                         VIEW LAYER                          │
│  MainWindow.xaml │ ProtectionView.xaml │ SettingsView.xaml  │
└──────────────────────────┬──────────────────────────────────┘
                           │ DataBinding
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                      VIEWMODEL LAYER                        │
│  MainViewModel │ ProtectionViewModel │ SettingsViewModel    │
└──────────────────────────┬──────────────────────────────────┘
                           │ References
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                       MODEL LAYER                           │
│         ServerEntry │ CountryFlag │ ServerModel             │
└──────────────────────────┬──────────────────────────────────┘
                           │ Managed by
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                       CORE LAYER                            │
│  ProtectionManager (Singleton) │ ServerPersistenceService   │
│  ObservableObject │ RelayCommand                            │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Singleton Pattern

The `ProtectionManager` class uses the Singleton pattern to ensure a single source of truth for server data:

```csharp
// From Core/ProtectionManager.cs
public class ProtectionManager
{
    private static ProtectionManager? _instance;
    public static ProtectionManager Instance => _instance ??= new ProtectionManager();

    private readonly ObservableCollection<ServerEntry> _servers = new();
    public ReadOnlyObservableCollection<ServerEntry> Servers { get; }

    private ProtectionManager()
    {
        Servers = new ReadOnlyObservableCollection<ServerEntry>(_servers);
        LoadServers();
    }
}
```

**Why Singleton?**
- Both `SettingsViewModel` and `ProtectionViewModel` need access to the same server list
- Changes in one view must reflect immediately in the other
- Centralized persistence logic (save on every change)

### 2.3 Command Pattern

All user actions are handled via `RelayCommand`, implementing `ICommand`:

```csharp
// From Core/RelayCommand.cs
public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    : ICommand
{
    private Action<object?> execute = execute;
    private Func<object?, bool>? canExecute = canExecute;    
    
    public bool CanExecute(object? parameter)
    {
        return this.canExecute == null || canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        execute(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
```

**Usage Example — Connect Button:**
```csharp
// From ProtectionViewModel.cs
ConnectCommand = new RelayCommand(_ =>
{
    if (SelectedServer == null)
    {
        MessageBox.Show("Please select a server first.", "No Server Selected", 
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
    }
    
    CreateVpnConnection(SelectedServer);
    ConnectToVPN(SelectedServer);
}, _ => SelectedServer != null && !IsConnected);  // CanExecute condition
```

### 2.4 Observer Pattern (INotifyPropertyChanged)

The `ObservableObject` base class provides property change notification:

```csharp
// From Core/ObservableObject.cs
public class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

**Usage Example — ConnectionStatus property:**
```csharp
// From ProtectionViewModel.cs
private string _connectionStatus = "Disconnected";
public string ConnectionStatus
{
    get => _connectionStatus;
    private set
    {
        _connectionStatus = value;
        OnPropertyChanged();  // Notifies UI to update
    }
}
```

---

## 3. Project Structure

```
VPN/
├── App.xaml                          # Application entry point & resources
├── App.xaml.cs                       # Application code-behind
├── VPN.csproj                        # Project configuration
│
├── Core/                             # Infrastructure layer
│   ├── ObservableObject.cs           # INotifyPropertyChanged base class
│   ├── RelayCommand.cs               # ICommand implementation
│   ├── ProtectionManager.cs          # Singleton server manager
│   └── ServerPersistenceService.cs   # JSON persistence service
│
├── MVVM/
│   ├── Model/                        # Data structures
│   │   ├── ServerEntry.cs            # Main server model
│   │   ├── CountryFlag.cs            # Country/flag representation
│   │   ├── ServerModel.cs            # Legacy model
│   │   └── ServersEntry.cs           # Legacy model
│   │
│   ├── ViewModel/                    # Business logic
│   │   ├── MainViewModel.cs          # Main window logic & navigation
│   │   ├── ProtectionViewModel.cs    # VPN connection logic
│   │   └── SettingsViewModel.cs      # Server management logic
│   │
│   ├── View/                         # UI (XAML)
│   │   ├── MainWindow.xaml           # Main window layout
│   │   ├── ProtectionView.xaml       # VPN connection view
│   │   ├── ProtectionView.xaml.cs
│   │   ├── SettingsView.xaml         # Server management view
│   │   └── SettingsView.xaml.cs
│   │
│   └── Themes/                       # Custom styles
│       ├── TitleButton.Nord.xaml
│       ├── MenuButton.Nord.xaml
│       ├── ConnectButton.Nord.xaml
│       └── ServerListTheme.Nord.xaml
│
├── Img/                              # Assets
│   ├── MapWorld.jpg                  # Background image
│   └── Flags/                        # Country flag images
│       ├── US.png
│       ├── CA.png
│       ├── FR.png
│       └── ... (31 countries)
│
└── MainWindow.xaml.cs                # Main window code-behind
```

---

## 4. Core Layer — Detailed Analysis

### 4.1 ProtectionManager (Singleton)

**File:** `Core/ProtectionManager.cs`

This is the **central hub** for all server data. It:
- Holds the master `ObservableCollection<ServerEntry>`
- Exposes a `ReadOnlyObservableCollection` to prevent external modification
- Auto-saves to JSON on every add/remove operation
- Auto-loads from JSON on instantiation

```csharp
public class ProtectionManager
{
    private static ProtectionManager? _instance;
    public static ProtectionManager Instance => _instance ??= new ProtectionManager();

    private readonly ObservableCollection<ServerEntry> _servers = new();
    public ReadOnlyObservableCollection<ServerEntry> Servers { get; }

    private ProtectionManager()
    {
        Servers = new ReadOnlyObservableCollection<ServerEntry>(_servers);
        LoadServers();  // Load from JSON at startup
    }

    private void LoadServers()
    {
        var savedServers = ServerPersistenceService.LoadServers();
        foreach (var server in savedServers)
        {
            _servers.Add(server);
        }
    }

    public void AddServer(ServerEntry entry)
    {
        if (entry == null) return;
        _servers.Add(entry);
        SaveServers();  // Persist immediately
    }

    public void RemoveServer(ServerEntry entry)
    {
        if (entry == null) return;
        _servers.Remove(entry);
        SaveServers();  // Persist immediately
    }

    private void SaveServers()
    {
        ServerPersistenceService.SaveServers(_servers.ToList());
    }
}
```

**Key Design Decisions:**
- `ReadOnlyObservableCollection` prevents ViewModels from directly modifying the list
- All modifications go through `AddServer()` / `RemoveServer()` methods
- Persistence is automatic and transparent

### 4.2 ServerPersistenceService (Static Service)

**File:** `Core/ServerPersistenceService.cs`

Handles JSON serialization/deserialization:

```csharp
public class ServerPersistenceService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VPNApp"
    );
    
    private static readonly string ServersFilePath = Path.Combine(AppDataFolder, "servers.json");

    public static void SaveServers(IEnumerable<ServerEntry> servers)
    {
        try
        {
            Directory.CreateDirectory(AppDataFolder);  // Create folder if needed
            
            var json = JsonSerializer.Serialize(servers, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(ServersFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving servers: {ex.Message}");
        }
    }

    public static List<ServerEntry> LoadServers()
    {
        try
        {
            if (!File.Exists(ServersFilePath))
                return new List<ServerEntry>();
            
            var json = File.ReadAllText(ServersFilePath);
            var servers = JsonSerializer.Deserialize<List<ServerEntry>>(json);
            
            return servers ?? new List<ServerEntry>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading servers: {ex.Message}");
            return new List<ServerEntry>();
        }
    }
}
```

**Storage Location:**
```
%APPDATA%\VPNApp\servers.json
```

**Example JSON Output:**
```json
[
  {
    "Id": 1,
    "Country": "United States (US16)",
    "Address": "us16.vpnbook.com",
    "Username": "vpnbook",
    "Password": "m4mkacr",
    "ConnectionName": "VPNBook_US16",
    "FlagCode": "US"
  },
  {
    "Id": 1738943542,
    "Country": "ca149.vpnbook.com",
    "Address": "ca149.vpnbook.com",
    "Username": "vpnbook",
    "Password": "m4mkacr",
    "ConnectionName": "VPN_ca149_vpnbook_com",
    "FlagCode": "CA"
  }
]
```

---

## 5. Model Layer — Data Structures

### 5.1 ServerEntry (Primary Model)

**File:** `MVVM/Model/ServerEntry.cs`

This is the main data model for VPN servers:

```csharp
public class ServerEntry
{
    public int Id { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConnectionName { get; set; } = string.Empty;
    public string FlagCode { get; set; } = "US";
    
    // Computed property for flag image path
    public string FlagPath => $"../../Img/Flags/{FlagCode}.png";
}
```

**Properties Explained:**

| Property | Purpose | Example |
|----------|---------|---------|
| `Id` | Unique identifier (timestamp-based) | `1738943542` |
| `Country` | Display name in UI | `"United States (US16)"` |
| `Address` | VPN server hostname | `"us16.vpnbook.com"` |
| `Username` | Authentication username | `"vpnbook"` |
| `Password` | Authentication password | `"m4mkacr"` |
| `ConnectionName` | Windows VPN connection name | `"VPNBook_US16"` |
| `FlagCode` | ISO country code for flag | `"US"`, `"FR"`, `"CA"` |
| `FlagPath` | Computed path to flag image | `"../../Img/Flags/US.png"` |

### 5.2 CountryFlag (UI Helper Model)

**File:** `MVVM/Model/CountryFlag.cs`

Used for the country selection ComboBox:

```csharp
public class CountryFlag
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FlagPath => $"../../Img/Flags/{Code}.png";
}
```

**Usage in SettingsViewModel:**
```csharp
public ObservableCollection<CountryFlag> AvailableCountries { get; } = new()
{
    new CountryFlag { Code = "US", Name = "United States" },
    new CountryFlag { Code = "CA", Name = "Canada" },
    new CountryFlag { Code = "GB", Name = "United Kingdom" },
    new CountryFlag { Code = "FR", Name = "France" },
    // ... 31 countries total
};
```

---

## 6. ViewModel Layer — Business Logic

### 6.1 MainViewModel (Navigation & Window Control)

**File:** `MVVM/ViewModel/MainViewModel.cs`

Manages:
- Window operations (minimize, maximize, close)
- View navigation (Protection ↔ Settings)
- VPN disconnection on app close

```csharp
internal class MainViewModel : INotifyPropertyChanged
{
    // Child ViewModels
    private ProtectionViewModel ProtectionVM { get; set; }
    private SettingsViewModel SettingsVM { get; set; }
    
    // Current view (for ContentPresenter binding)
    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    // Commands
    public RelayCommand ShutWindow { get; set; }
    public RelayCommand MaxWindow { get; set; }
    public RelayCommand MinWindow { get; set; }
    public RelayCommand ShowProtectionViewCommand { get; set; }
    public RelayCommand ShowSettingsViewCommand { get; set; }
    public RelayCommand DisconnectVpnCommand { get; set; }

    public MainViewModel()
    {
        ProtectionVM = new ProtectionViewModel();
        SettingsVM = new SettingsViewModel();
        CurrentView = ProtectionVM;  // Default view
        
        // Navigation commands
        ShowProtectionViewCommand = new RelayCommand(_ => CurrentView = ProtectionVM);
        ShowSettingsViewCommand = new RelayCommand(_ => CurrentView = SettingsVM);
        
        // Window commands
        ShutWindow = new RelayCommand(_ =>
        {
            DisconnectVpn();           // Disconnect VPN before closing
            StopVpnProcess();          // Kill any orphan processes
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
        
        DisconnectVpnCommand = new RelayCommand(_ => DisconnectVpn());
    }
```

**VPN Disconnection on Close:**
```csharp
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
        
        MessageBox.Show("VPN déconnecté avec succès.", "Déconnexion", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception e)
    {
        MessageBox.Show($"Erreur lors de la déconnexion : {e.Message}", "Erreur", 
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### 6.2 ProtectionViewModel (VPN Connection)

**File:** `MVVM/ViewModel/ProtectionViewModel.cs`

Handles:
- Server list display
- Server selection
- VPN connection/disconnection
- Connection status tracking
- Logging

```csharp
public class ProtectionViewModel : ObservableObject
{
    // Uses ProtectionManager's collection (synchronized with SettingsView)
    public ReadOnlyObservableCollection<ServerEntry> Servers => ProtectionManager.Instance.Servers;
    
    // Selected server for connection
    private ServerEntry? _selectedServer;
    public ServerEntry? SelectedServer
    {
        get => _selectedServer;
        set
        {
            _selectedServer = value;
            OnPropertyChanged();
            ConnectCommand?.RaiseCanExecuteChanged();
        }
    }
    
    // Connection state
    private string _connectionStatus = "Disconnected";
    private bool _isConnected = false;
    private string _logMessages = "Application started. Ready to connect.";
    
    public string ConnectionStatus { get => _connectionStatus; /* ... */ }
    public bool IsConnected { get => _isConnected; /* ... */ }
    public string LogMessages { get => _logMessages; /* ... */ }
    
    // Commands
    public RelayCommand ConnectCommand { get; set; }
    public RelayCommand DisconnectCommand { get; set; }
```

**Constructor — Default Server & Commands:**
```csharp
public ProtectionViewModel()
{
    // Add default server if list is empty
    if (ProtectionManager.Instance.Servers.Count == 0)
    {
        ProtectionManager.Instance.AddServer(new ServerEntry
        {
            Id = 1,
            Country = "United States (US16)",
            Address = "us16.vpnbook.com",
            Username = "vpnbook",
            Password = "m4mkacr",
            ConnectionName = "VPNBook_US16",
            FlagCode = "US"
        });
    }
    
    // Select first server by default
    SelectedServer = Servers.FirstOrDefault();

    // Connect command with CanExecute validation
    ConnectCommand = new RelayCommand(_ =>
    {
        if (SelectedServer == null)
        {
            MessageBox.Show("Please select a server first.", "No Server Selected", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            LogMessage($"Attempting to connect to {SelectedServer.Address}...");
            LogMessage($"Username: {SelectedServer.Username}");
            
            CreateVpnConnection(SelectedServer);
            ConnectToVPN(SelectedServer);
        }
        catch (Exception ex)
        {
            LogMessage($"Error: {ex.Message}");
            MessageBox.Show($"Connection failed: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }, _ => SelectedServer != null && !IsConnected);

    // Disconnect command
    DisconnectCommand = new RelayCommand(_ =>
    {
        if (SelectedServer == null) return;
        
        try
        {
            LogMessage("Disconnecting from VPN...");
            DisconnectFromVPN(SelectedServer.ConnectionName);
        }
        catch (Exception ex)
        {
            LogMessage($"Error during disconnection: {ex.Message}");
            MessageBox.Show($"Disconnection failed: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }, _ => IsConnected);
}
```

### 6.3 SettingsViewModel (Server Management)

**File:** `MVVM/ViewModel/SettingsViewModel.cs`

Handles:
- Form input (address, username, password, country)
- Adding new servers
- Removing existing servers
- Country flag selection

```csharp
public class SettingsViewModel : ObservableObject
{
    // Form fields
    private string _serverAddress = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    
    public string ServerAddress
    {
        get => _serverAddress;
        set
        {
            if (SetField(ref _serverAddress, value))
                AddServerCommand.RaiseCanExecuteChanged();  // Re-evaluate CanExecute
        }
    }
    
    // Country selection
    public ObservableCollection<CountryFlag> AvailableCountries { get; } = new()
    {
        new CountryFlag { Code = "US", Name = "United States" },
        new CountryFlag { Code = "CA", Name = "Canada" },
        // ... 31 countries
    };
    
    private CountryFlag? _selectedCountry;
    public CountryFlag? SelectedCountry
    {
        get => _selectedCountry;
        set => SetField(ref _selectedCountry, value);
    }

    // Server list (synchronized with ProtectionView)
    public ReadOnlyObservableCollection<ServerEntry> Servers => ProtectionManager.Instance.Servers;

    // Commands
    public RelayCommand AddServerCommand { get; }
    public RelayCommand ClearFormCommand { get; }
    public RelayCommand RemoveServerCommand { get; }
```

**Add Server Logic:**
```csharp
private void AddServer()
{
    // Generate unique ID from timestamp
    var id = (int)(DateTime.Now.Ticks % int.MaxValue);
    
    // Generate unique connection name (sanitize dots and colons)
    var connectionName = $"VPN_{ServerAddress?.Replace(".", "_").Replace(":", "_") ?? $"Server_{id}"}";
    
    var entry = new ServerEntry
    {
        Id = id,
        Country = ServerAddress ?? "Unknown Server",
        Address = ServerAddress ?? string.Empty,
        Username = Username ?? string.Empty,
        Password = Password ?? string.Empty,
        ConnectionName = connectionName,
        FlagCode = SelectedCountry?.Code ?? "US"
    };

    // Add via ProtectionManager (automatically persists)
    ProtectionManager.Instance.AddServer(entry);

    ClearForm();
}
```

---

## 7. View Layer — User Interface

### 7.1 App.xaml (Application Bootstrap)

**File:** `App.xaml`

Defines:
- Startup window
- Resource dictionaries (themes)
- DataTemplate mappings (ViewModel → View)

```xml
<Application x:Class="VPN.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:viewModel="clr-namespace:VPN.MVVM.ViewModel"
             xmlns:view="clr-namespace:VPN.MVVM.View"
             StartupUri="MVVM/View/MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <!-- Theme styles -->
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="MVVM/Themes/TitleButton.Nord.xaml"/>
                <ResourceDictionary Source="MVVM/Themes/MenuButton.Nord.xaml"/>
                <ResourceDictionary Source="MVVM/Themes/ConnectButton.Nord.xaml"/>
                <ResourceDictionary Source="MVVM/Themes/ServerListTheme.Nord.xaml"/>
            </ResourceDictionary.MergedDictionaries>
            
            <!-- ViewModel to View mappings -->
            <DataTemplate DataType="{x:Type viewModel:ProtectionViewModel}">
                <view:ProtectionView/>
            </DataTemplate>
            <DataTemplate DataType="{x:Type viewModel:SettingsViewModel}">
                <view:SettingsView/>
            </DataTemplate>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**How DataTemplates Work:**

When `MainViewModel.CurrentView` is set to a `ProtectionViewModel` instance, WPF automatically renders a `ProtectionView`. This enables **view navigation without explicit view references** in the ViewModel.

### 7.2 MainWindow.xaml (Shell)

**File:** `MVVM/View/MainWindow.xaml`

Features:
- Custom title bar (draggable)
- Window controls (minimize, maximize, close)
- Navigation sidebar (radio buttons)
- ContentPresenter for dynamic view switching

```xml
<Window x:Class="VPN.MainWindow"
        Title="MainWindow" Height="450" Width="800"
        WindowStyle="None" Background="Transparent"
        AllowsTransparency="True" ResizeMode="CanResize">
  <Window.DataContext>
    <viewmodel:MainViewModel xmlns:viewmodel="clr-namespace:VPN.MVVM.ViewModel"/>
  </Window.DataContext>
  
  <DockPanel Background="#1E1E1E" Margin="7">
    <!-- Title Bar -->
    <Border Height="28" Background="#252525" DockPanel.Dock="Top">
      <Grid Background="#272E2E" MouseLeftButtonDown="TitleBar_MouseLeftButtonDown">
        <TextBlock Text="Surfhub" Foreground="LightGray" FontFamily="Consolas"
                   VerticalAlignment="Top" Margin="12,2,0,0"/>
        
        <!-- Window buttons -->
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,2,8,0">
          <Button Content="-" Command="{Binding MinWindow}" />
          <Button Content="🗖" Command="{Binding MaxWindow}" />
          <Button Content="X" Command="{Binding ShutWindow}" />
        </StackPanel>
      </Grid>
    </Border>
    
    <!-- Sidebar navigation -->
    <Border Width="60" Background="#272E2E">
      <StackPanel>
        <RadioButton Content="🛡️" IsChecked="True" 
                     Command="{Binding ShowProtectionViewCommand}" />
        <RadioButton Content="⚙️" 
                     Command="{Binding ShowSettingsViewCommand}"/>
        <RadioButton Content="⛔VPN" 
                     Command="{Binding DisconnectVpnCommand}"/>
      </StackPanel>
    </Border>
    
    <!-- Dynamic content area -->
    <ContentPresenter Content="{Binding CurrentView}"/>
  </DockPanel>
</Window>
```

### 7.3 ProtectionView.xaml (VPN Connection UI)

**File:** `MVVM/View/ProtectionView.xaml`

Features:
- World map background with blur effect
- Server list with flags
- Selected server highlighting
- Connection status display
- Connect button

```xml
<UserControl x:Class="VPN.MVVM.View.ProtectionView"
             Background="#252525">
    <Grid>
        <!-- Background image -->
        <Image Source="../../Img/MapWorld.jpg" Stretch="UniformToFill">
            <Image.OpacityMask>
                <LinearGradientBrush StartPoint="0.5,1" EndPoint="0.5,0">
                    <GradientStop Offset="0" Color="#FF000000" />
                    <GradientStop Offset="1" Color="#00000000" />
                </LinearGradientBrush>
            </Image.OpacityMask>
            <Image.Effect>
                <BlurEffect Radius="10"/>
            </Image.Effect>
        </Image>
        
        <!-- Server list with flag icons -->
        <ListView ItemsSource="{Binding Servers}"
                  SelectedItem="{Binding SelectedServer, Mode=TwoWay}">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <Image Width="34" Source="{Binding FlagPath}" />
                        <TextBlock Text="{Binding Country}" Foreground="LightGray" />
                    </StackPanel>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        
        <!-- Connection status -->
        <TextBlock Text="{Binding ConnectionStatus}" Foreground="white"/>
        
        <!-- Connect button -->
        <Button Content="Connect" 
                Style="{StaticResource ConnectButtonNordStyle}"
                Command="{Binding ConnectCommand}"/>
    </Grid>
</UserControl>
```

### 7.4 SettingsView.xaml (Server Management UI)

**File:** `MVVM/View/SettingsView.xaml`

Features:
- Form with placeholders (server address, username, password)
- Country selection ComboBox with flags
- Add/Clear buttons
- Server list with Remove button

```xml
<UserControl x:Class="VPN.MVVM.View.SettingsView" Background="#252525">
    <Grid Margin="12">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*" />  <!-- Form -->
            <ColumnDefinition Width="3*" />  <!-- Server list -->
        </Grid.ColumnDefinitions>

        <!-- Add Server Form -->
        <Border Grid.Column="0" Background="#2E2E2E" CornerRadius="6" Padding="12">
            <StackPanel>
                <TextBlock Text="Add server" Foreground="LightGray" FontSize="16"/>

                <!-- Server address with placeholder -->
                <Grid Margin="0,6">
                    <TextBox x:Name="ServerTextBox" 
                             Text="{Binding ServerAddress, UpdateSourceTrigger=PropertyChanged}" 
                             Background="#3A3A3A" Foreground="White"/>
                    <TextBlock Text="Server address (ex: vpn.example.com)"
                               Foreground="#9A9A9A" IsHitTestVisible="False">
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Setter Property="Visibility" Value="Collapsed"/>
                                <Style.Triggers>
                                    <MultiDataTrigger>
                                        <MultiDataTrigger.Conditions>
                                            <Condition Binding="{Binding Text, ElementName=ServerTextBox}" Value=""/>
                                            <Condition Binding="{Binding IsFocused, ElementName=ServerTextBox}" Value="False"/>
                                        </MultiDataTrigger.Conditions>
                                        <Setter Property="Visibility" Value="Visible"/>
                                    </MultiDataTrigger>
                                </Style.Triggers>
                            </Style>
                        </TextBlock.Style>
                    </TextBlock>
                </Grid>

                <!-- Country/Flag selection -->
                <ComboBox ItemsSource="{Binding AvailableCountries}"
                          SelectedItem="{Binding SelectedCountry}">
                    <ComboBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Image Source="{Binding FlagPath}" Width="24" Height="16" />
                                <TextBlock Text="{Binding Name}" Foreground="White"/>
                            </StackPanel>
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>

                <!-- Buttons -->
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <Button x:Name="AddServerButton" Content="Add" Background="#4A90E2"/>
                    <Button x:Name="ClearFormButton" Content="Delete" Background="#666"/>
                </StackPanel>
            </StackPanel>
        </Border>

        <!-- Server List -->
        <Border Grid.Column="1" Background="#2E2E2E" CornerRadius="6" Padding="12">
            <ListBox x:Name="ServersListBox" Background="#2E2E2E">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Grid>
                            <StackPanel>
                                <TextBlock Text="{Binding Address}" FontWeight="SemiBold"/>
                                <TextBlock Text="{Binding Username}" Foreground="#BFBFBF"/>
                            </StackPanel>
                            <Button Content="Supprimer" Background="#A94442"
                                    Command="{Binding DataContext.RemoveServerCommand, 
                                             RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding}" />
                        </Grid>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </Border>
    </Grid>
</UserControl>
```

### 7.5 SettingsView.xaml.cs (Code-Behind)

**File:** `MVVM/View/SettingsView.xaml.cs`

Handles PasswordBox binding (not supported in pure XAML):

```csharp
public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _vm;

    public SettingsView()
    {
        InitializeComponent();
        _vm = new SettingsViewModel();
        DataContext = _vm;

        // PasswordBox doesn't support binding, so we handle it manually
        PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;

        // Wire up commands
        AddServerButton.Command = _vm.AddServerCommand;
        ClearFormButton.Command = _vm.ClearFormCommand;

        // Bind server list
        ServersListBox.ItemsSource = _vm.Servers;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.Password = PasswordBox.Password;
    }
}
```

---

## 8. Data Persistence

### 8.1 Storage Location

```
%APPDATA%\VPNApp\servers.json
```

Example path:
```
C:\Users\YourName\AppData\Roaming\VPNApp\servers.json
```

### 8.2 JSON Schema

```json
[
  {
    "Id": 1,
    "Country": "United States (US16)",
    "Address": "us16.vpnbook.com",
    "Username": "vpnbook",
    "Password": "m4mkacr",
    "ConnectionName": "VPNBook_US16",
    "FlagCode": "US"
  }
]
```

### 8.3 Persistence Flow

```
User clicks "Add" in SettingsView
         │
         ▼
SettingsViewModel.AddServer()
         │
         ▼
ProtectionManager.AddServer(entry)
         │
         ├──► _servers.Add(entry)     // Updates ObservableCollection
         │
         └──► SaveServers()           // Persists to JSON
                   │
                   ▼
         ServerPersistenceService.SaveServers()
                   │
                   ▼
         File.WriteAllText(ServersFilePath, json)
```

---

## 9. VPN Connection Logic

### 9.1 Connection Flow

```
User clicks "Connect" button
         │
         ▼
ConnectCommand.Execute()
         │
         ├──► CreateVpnConnection(server)
         │         │
         │         ▼
         │    powershell.exe Add-VpnConnection
         │    Creates Windows VPN profile
         │
         └──► ConnectToVPN(server)
                   │
                   ▼
              rasdial.exe "{ConnectionName}" "{Username}" "{Password}"
                   │
                   ├──► Success: IsConnected = true
                   │
                   └──► Failure: Show error message
```

### 9.2 CreateVpnConnection Method

Uses PowerShell `Add-VpnConnection` cmdlet:

```csharp
private void CreateVpnConnection(ServerEntry server)
{
    var psi = new ProcessStartInfo
    {
        FileName = "powershell.exe",
        Arguments = $"-Command \"Add-VpnConnection " +
                    $"-Name '{server.ConnectionName}' " +
                    $"-ServerAddress '{server.Address}' " +
                    $"-TunnelType Pptp " +
                    $"-EncryptionLevel Optional " +
                    $"-AuthenticationMethod MSChapv2 " +
                    $"-RememberCredential -Force\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    var process = Process.Start(psi);
    if (process != null)
    {
        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            LogMessage("VPN connection configured successfully.");
        }
        else
        {
            string error = process.StandardError.ReadToEnd();
            LogMessage($"Configuration warning: {error}");
        }
    }
}
```

### 9.3 ConnectToVPN Method

Uses Windows `rasdial.exe`:

```csharp
private void ConnectToVPN(ServerEntry server)
{
    ConnectionStatus = "Connecting...";
    
    var psi = new ProcessStartInfo
    {
        FileName = "rasdial.exe",
        Arguments = $"\"{server.ConnectionName}\" \"{server.Username}\" \"{server.Password}\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    var vpnProcess = Process.Start(psi);
    if (vpnProcess != null)
    {
        vpnProcess.WaitForExit();
        string output = vpnProcess.StandardOutput.ReadToEnd();

        if (vpnProcess.ExitCode == 0 || output.Contains("successfully", StringComparison.OrdinalIgnoreCase))
        {
            IsConnected = true;
            ConnectionStatus = $"Connected to {server.Country}";
            LogMessage("Successfully connected to VPN!");
            
            // Update button states
            ConnectCommand?.RaiseCanExecuteChanged();
            DisconnectCommand?.RaiseCanExecuteChanged();
        }
        else
        {
            ConnectionStatus = "Connection failed";
            IsConnected = false;
            MessageBox.Show("Failed to connect. Run as Administrator.", 
                "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
```

### 9.4 DisconnectFromVPN Method

```csharp
private void DisconnectFromVPN(string connectionName)
{
    var psi = new ProcessStartInfo
    {
        FileName = "rasdial.exe",
        Arguments = $"\"{connectionName}\" /disconnect",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    var process = Process.Start(psi);
    if (process != null)
    {
        process.WaitForExit();
        
        IsConnected = false;
        ConnectionStatus = "Disconnected";
        LogMessage("Disconnected from VPN.");
        
        ConnectCommand?.RaiseCanExecuteChanged();
        DisconnectCommand?.RaiseCanExecuteChanged();
    }
}
```

---

## 10. Build, Run & Publish

### 10.1 Prerequisites

- .NET 10 SDK (or later)
- Windows 10/11
- Administrator rights (for VPN operations)

### 10.2 Build & Run (Development)

```powershell
cd C:\git\Projets\VPN\VPN\VPN
dotnet build
dotnet run
```

### 10.3 Publish Standalone Executable

```powershell
# Create self-contained single-file executable (x64)
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\publish

# Run the executable
.\publish\VPN.exe
```

**Publish Options:**

| Option | Purpose |
|--------|---------|
| `-c Release` | Release configuration (optimized) |
| `-r win-x64` | Target runtime (64-bit Windows) |
| `--self-contained true` | Include .NET runtime |
| `/p:PublishSingleFile=true` | Single .exe file |
| `-o .\publish` | Output directory |

### 10.4 Project File (VPN.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0-windows</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <UseWPF>true</UseWPF>
    </PropertyGroup>

    <ItemGroup>
      <Resource Include="Img\**\*" />  <!-- Include all images -->
    </ItemGroup>
</Project>
```

---

## 11. Troubleshooting Guide

### 11.1 Error: "No process is associated with this object"

**Cause:** Accessing `Process.ExitCode` or `HasExited` when `Process.Start()` returned `null`.

**Solution:** Always check for null:
```csharp
var process = Process.Start(psi);
if (process != null)
{
    process.WaitForExit();
    // Safe to use process properties
}
```

### 11.2 VPN Error 623 (Remote Access Failed)

**Causes:**
- Server offline or unreachable
- PPTP blocked by ISP/firewall
- Incorrect credentials
- Windows VPN service disabled

**Solutions:**
1. Test manually in PowerShell:
   ```powershell
   rasdial "VPNBook_US16" vpnbook m4mkacr
   ```
2. Check server availability: `ping us16.vpnbook.com`
3. Try different server
4. Run application as Administrator

### 11.3 File Lock Error on Build

**Error:** `The process cannot access the file 'VPN.exe' because it is being used`

**Solution:**
1. Close running application
2. Check Task Manager for orphan `VPN.exe` processes
3. Kill process and rebuild

### 11.4 VPN Creation Requires Administrator

**Issue:** `Add-VpnConnection` fails silently without admin rights.

**Solution:** Run application as Administrator:
1. Right-click `VPN.exe` → "Run as administrator"
2. Or create shortcut with "Run as administrator" enabled

---

## Summary

**Surfhub** is a complete WPF VPN client implementing:

| Feature | Implementation |
|---------|----------------|
| **Architecture** | MVVM with Singleton ProtectionManager |
| **Server Management** | Add/Remove with 31 country flags |
| **Persistence** | JSON in AppData (auto-save) |
| **VPN Connection** | Windows PPTP via PowerShell + rasdial |
| **UI** | Custom Nord theme, borderless window |
| **Navigation** | ContentPresenter + DataTemplates |

**Key Files:**
- `ProtectionManager.cs` — Central server repository
- `ProtectionViewModel.cs` — VPN connection logic
- `SettingsViewModel.cs` — Server management
- `ServerEntry.cs` — Data model
- `MainWindow.xaml` — Application shell

---

*Documentation generated on 2026-01-07*

