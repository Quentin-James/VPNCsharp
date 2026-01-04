using System.Windows;
using System.Windows.Input;

namespace VPN;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Si la fenêtre est maximisée, la restaurer d'abord
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
            
            // Repositionner la fenêtre pour que le curseur reste au même endroit relatif
            var mousePos = e.GetPosition(this);
            var screenPoint = this.PointToScreen(mousePos);
            this.Left = screenPoint.X - (this.ActualWidth / 2);
            this.Top = screenPoint.Y - 10;
        }

        try
        {
            // Drag the window while the left mouse button is pressed
            this.DragMove();
        }
        catch
        {
            // DragMove peut échouer si appelé dans un mauvais contexte
        }
    }
}