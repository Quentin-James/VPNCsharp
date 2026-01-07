using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using VPN.Core;
using VPN.MVVM.Model;

namespace VPN.MVVM.ViewModel
{
    public class ProtectionViewModel : ObservableObject
    {
        // Utiliser la collection du ProtectionManager directement
        public ReadOnlyObservableCollection<ServerEntry> Servers => ProtectionManager.Instance.Servers;
        
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
        
        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand DisconnectCommand { get; set; }
        
        
        private string _connectionStatus = "Disconnected";
        private bool _isConnected = false;
        private string _logMessages = "Application started. Ready to connect.";
        
        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public string LogMessages
        {
            get => _logMessages;
            set
            {
                _logMessages = value;
                OnPropertyChanged();
            }
        }
        
        public ProtectionViewModel()
        {
            // Ajouter un serveur par défaut si la liste est vide
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
            
            // Sélectionner le premier serveur par défaut
            SelectedServer = Servers.FirstOrDefault();

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
                    
                    // Check if VPN connection exists, if not create it
                    CreateVpnConnection(SelectedServer);
                    
                    // Connect to VPN
                    ConnectToVPN(SelectedServer);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error: {ex.Message}");
                    MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, _ => SelectedServer != null && !IsConnected);

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
                    MessageBox.Show($"Disconnection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, _ => IsConnected);
        }

        private void CreateVpnConnection(ServerEntry server)
        {
            try
            {
                LogMessage("Checking VPN connection configuration...");
                
                // Create VPN connection using PowerShell
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-VpnConnection -Name '{server.ConnectionName}' -ServerAddress '{server.Address}' -TunnelType Pptp -EncryptionLevel Optional -AuthenticationMethod MSChapv2 -RememberCredential -Force\"",
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
                        LogMessage($"Configuration warning (connection might already exist): {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Configuration note: {ex.Message} (connection might already exist)");
            }
        }

        private void ConnectToVPN(ServerEntry server)
        {
            try
            {
                LogMessage("Initiating VPN connection...");
                ConnectionStatus = "Connecting...";
                
                // Use rasdial to connect
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
                    string error = vpnProcess.StandardError.ReadToEnd();

                    if (vpnProcess.ExitCode == 0 || output.Contains("successfully", StringComparison.OrdinalIgnoreCase))
                    {
                        IsConnected = true;
                        ConnectionStatus = $"Connected to {server.Country}";
                        LogMessage("Successfully connected to VPN!");
                        LogMessage(output);
                        ConnectCommand?.RaiseCanExecuteChanged();
                        DisconnectCommand?.RaiseCanExecuteChanged();
                    }
                    else
                    {
                        LogMessage($"Connection failed: {output}");
                        if (!string.IsNullOrEmpty(error))
                            LogMessage($"Error details: {error}");
                        
                        ConnectionStatus = "Connection failed";
                        IsConnected = false;
                        
                        MessageBox.Show(
                            "Failed to connect to VPN. Please check your credentials and try again.\n\n" +
                            "Note: You may need to run this application as Administrator.", 
                            "Connection Failed", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Connection error: {ex.Message}");
                ConnectionStatus = "Error";
                IsConnected = false;
                throw;
            }
        }

        private void DisconnectFromVPN(string connectionName)
        {
            try
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
                    string output = process.StandardOutput.ReadToEnd();
                    
                    IsConnected = false;
                    ConnectionStatus = "Disconnected";
                    LogMessage("Disconnected from VPN.");
                    LogMessage(output);
                    ConnectCommand?.RaiseCanExecuteChanged();
                    DisconnectCommand?.RaiseCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Disconnection error: {ex.Message}");
                throw;
            }
        }

        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogMessages += $"\n[{timestamp}] {message}";
            OnPropertyChanged(nameof(LogMessages));
        }
    }
}