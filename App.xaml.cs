using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

namespace NusaNotch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        EnableAutoStart();
    }

    private static void EnableAutoStart()
    {
        try
        {
            string appName = "NusaNotch";
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

            if (!string.IsNullOrEmpty(exePath))
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.SetValue(appName, $"\"{exePath}\"");
            }
        }
        catch
        {
            // Abaikan jika gagal mengakses registry
        }
    }
}