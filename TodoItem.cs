using System;

namespace Pnotch;

public class TodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public bool Done { get; set; }
    public DateTime? Due { get; set; }
    public bool AlarmFired { get; set; }

    // Helper method untuk memperbarui status/hitung mundur
    public void UpdateCountdown()
    {
        // Dapat dikosongkan atau diisi logika format waktu sisa
    }
}