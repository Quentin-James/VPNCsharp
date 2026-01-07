namespace VPN.MVVM.Model;

public class ServerModel
{
    public int Id { get; set; } = 0; 
    public string Country { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string ConnectionName { get; set; } = string.Empty;
}