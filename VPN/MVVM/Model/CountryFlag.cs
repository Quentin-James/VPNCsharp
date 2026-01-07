namespace VPN.MVVM.Model;

public class CountryFlag
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FlagPath => $"../../Img/Flags/{Code}.png";
}

