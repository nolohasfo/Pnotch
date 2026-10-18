using System;

namespace NusaNotch;

public class TodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public bool Done { get; set; }
    public DateTime? Due { get; set; }
}