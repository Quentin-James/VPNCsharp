using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VPN.MVVM.Model;

namespace VPN.Core;

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
            // Créer le dossier s'il n'existe pas
            Directory.CreateDirectory(AppDataFolder);
            
            // Sérialiser et sauvegarder
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

