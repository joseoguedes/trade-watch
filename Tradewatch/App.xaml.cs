using System.Windows;

namespace Tradewatch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        base.OnStartup(e);
    }
}

