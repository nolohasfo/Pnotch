using System.Windows;

namespace Pnotch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.DispatcherUnhandledException += (sender, args) =>
        {
            // Ambil InnerException jika ada untuk melihat detail penyebab crash
            var ex = args.Exception.InnerException ?? args.Exception;

            MessageBox.Show(
                $"Aplikasi mengalami crash pada startup:\n\n{ex.Message}\n\nDetail:\n{ex.StackTrace}",
                "Pnotch - Error Diagnostic",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            args.Handled = true;
        };
    }
}