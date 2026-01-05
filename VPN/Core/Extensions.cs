using System.Windows;

namespace VPN.Core;

internal class Extensions
{
    public static readonly DependencyProperty 
        Icon= DependencyProperty.RegisterAttached
            ("Icon", typeof(String), typeof(Extensions), 
                new PropertyMetadata(default(string)));   
    
    public static void SetIcon(UIElement element, String value)
        => element.SetValue(Icon, value);
    
    public static String GetIcon(UIElement element)
        => (String)element.GetValue(Icon);
}