using System.Windows;

namespace NusaNotch;

public interface INotchModule
{
    string Id { get; }
    UIElement View { get; }   // UserControl modul
    void OnOpened();          // Mulai timer/event saat panel tampil
    void OnClosed();          // Hentikan timer saat panel menutup
}