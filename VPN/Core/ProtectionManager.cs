using System.Collections.ObjectModel;
using System.Linq;
using VPN.MVVM.Model;

namespace VPN.Core;

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
        SaveServers();
    }

    public void RemoveServer(ServerEntry entry)
    {
        if (entry == null) return;
        _servers.Remove(entry);
        SaveServers();
    }

    private void SaveServers()
    {
        ServerPersistenceService.SaveServers(_servers.ToList());
    }
}

