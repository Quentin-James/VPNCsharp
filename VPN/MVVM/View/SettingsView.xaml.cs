// C#
using System.Windows;
using System.Windows.Controls;
using VPN.MVVM.ViewModel;

namespace VPN.MVVM.View;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _vm;

    public SettingsView()
    {
        InitializeComponent();
        _vm = new SettingsViewModel();
        DataContext = _vm;

        // Associe la valeur du PasswordBox à la propriété Password du VM
        PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;

        // Lier les boutons si vous ne le faites pas en XAML
        AddServerButton.Command = _vm.AddServerCommand;
        ClearFormButton.Command = _vm.ClearFormCommand;

        // Liste
        ServersListBox.ItemsSource = _vm.Servers;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.Password = PasswordBox.Password;
    }
}