using System.Windows;

namespace Tradewatch;

public partial class App : Application
{
    // Held for the app's lifetime so a second launch can tell one is already running.
    private static System.Threading.Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstance = new System.Threading.Mutex(true, "Tradewatch.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show("Tradewatch is already running — look for its icon in the system tray.",
                "Tradewatch", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        base.OnStartup(e);
    }
}

