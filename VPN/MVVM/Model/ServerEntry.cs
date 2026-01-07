namespace VPN.MVVM.Model;

public class ServerEntry
{
    public int Id { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConnectionName { get; set; } = string.Empty;
    public string FlagCode { get; set; } = "US"; // Code du pays (US, FR, CA, etc.)
    
    // Propriété calculée pour le chemin du drapeau
    public string FlagPath => $"../../Img/Flags/{FlagCode}.png";
}
