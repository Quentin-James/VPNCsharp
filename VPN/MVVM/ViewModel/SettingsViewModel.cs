// C#
using System;
using System.Collections.ObjectModel;
using VPN.Core;
using VPN.MVVM.Model;

namespace VPN.MVVM.ViewModel;

public class SettingsViewModel : ObservableObject
{
    private string _serverAddress = string.Empty;
    public string ServerAddress
    {
        get => _serverAddress;
        set
        {
            if (SetField(ref _serverAddress, value))
                AddServerCommand.RaiseCanExecuteChanged();
        }
    }

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set
        {
            if (SetField(ref _username, value))
                AddServerCommand.RaiseCanExecuteChanged();
        }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set
        {
            if (SetField(ref _password, value))
                AddServerCommand.RaiseCanExecuteChanged();
        }
    }

    // Liste des pays disponibles
    public ObservableCollection<CountryFlag> AvailableCountries { get; } = new()
    {
        new CountryFlag { Code = "US", Name = "United States" },
        new CountryFlag { Code = "CA", Name = "Canada" },
        new CountryFlag { Code = "GB", Name = "United Kingdom" },
        new CountryFlag { Code = "FR", Name = "France" },
        new CountryFlag { Code = "DE", Name = "Germany" },
        new CountryFlag { Code = "IT", Name = "Italy" },
        new CountryFlag { Code = "ES", Name = "Spain" },
        new CountryFlag { Code = "NL", Name = "Netherlands" },
        new CountryFlag { Code = "BE", Name = "Belgium" },
        new CountryFlag { Code = "CH", Name = "Switzerland" },
        new CountryFlag { Code = "AT", Name = "Austria" },
        new CountryFlag { Code = "SE", Name = "Sweden" },
        new CountryFlag { Code = "NO", Name = "Norway" },
        new CountryFlag { Code = "DK", Name = "Denmark" },
        new CountryFlag { Code = "FI", Name = "Finland" },
        new CountryFlag { Code = "PL", Name = "Poland" },
        new CountryFlag { Code = "RU", Name = "Russia" },
        new CountryFlag { Code = "JP", Name = "Japan" },
        new CountryFlag { Code = "KR", Name = "South Korea" },
        new CountryFlag { Code = "CN", Name = "China" },
        new CountryFlag { Code = "IN", Name = "India" },
        new CountryFlag { Code = "AU", Name = "Australia" },
        new CountryFlag { Code = "NZ", Name = "New Zealand" },
        new CountryFlag { Code = "BR", Name = "Brazil" },
        new CountryFlag { Code = "MX", Name = "Mexico" },
        new CountryFlag { Code = "AR", Name = "Argentina" },
        new CountryFlag { Code = "CL", Name = "Chile" },
        new CountryFlag { Code = "ZA", Name = "South Africa" },
        new CountryFlag { Code = "SG", Name = "Singapore" },
        new CountryFlag { Code = "HK", Name = "Hong Kong" },
        new CountryFlag { Code = "TR", Name = "Turkey" }
    };

    private CountryFlag? _selectedCountry;
    public CountryFlag? SelectedCountry
    {
        get => _selectedCountry;
        set => SetField(ref _selectedCountry, value);
    }

    // Utiliser la collection du ProtectionManager pour synchronisation avec ProtectionView
    public ReadOnlyObservableCollection<ServerEntry> Servers => ProtectionManager.Instance.Servers;

    public RelayCommand AddServerCommand { get; }
    public RelayCommand ClearFormCommand { get; }
    public RelayCommand RemoveServerCommand { get; }

    public SettingsViewModel()
    {
        // Sélectionner US par défaut
        SelectedCountry = AvailableCountries[0];
        
        AddServerCommand = new RelayCommand(_ => AddServer(), _ => CanAddServer());
        ClearFormCommand = new RelayCommand(_ => ClearForm());
        RemoveServerCommand = new RelayCommand(param => RemoveServer(param as ServerEntry), param => param is ServerEntry);
    }

    private bool CanAddServer() => !string.IsNullOrWhiteSpace(ServerAddress);

    private void AddServer()
    {
        // Générer un ID unique basé sur le timestamp
        var id = (int)(DateTime.Now.Ticks % int.MaxValue);
        
        // Générer un nom de connexion unique
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

        // Ajouter uniquement via ProtectionManager (Servers est readonly)
        ProtectionManager.Instance.AddServer(entry);

        ClearForm();
    }

    private void RemoveServer(ServerEntry? entry)
    {
        if (entry == null) return;
        // Supprimer uniquement via ProtectionManager (Servers est readonly)
        ProtectionManager.Instance.RemoveServer(entry);
    }

    private void ClearForm()
    {
        ServerAddress = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
    }
}