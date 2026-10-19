using System.Windows;

namespace NusaNotch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Menangkap seluruh Unhandled Exception agar muncul pesan error
        this.DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show(
                $"Aplikasi mengalami crash pada startup:\n\n{args.Exception.Message}\n\nDetail:\n{args.Exception.StackTrace}",
                "Pnotch - Error Diagnostic",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            args.Handled = true; // Mencegah aplikasi langsung keluar secara tiba-tiba
        };
    }
}