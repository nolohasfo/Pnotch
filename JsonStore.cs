using System;
using System.IO;
using System.Text.Json;

namespace Pnotch;

static class JsonStore
{
    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pnotch");

    public static T Load<T>(string name) where T : new()
    {
        try { 
            var p = Path.Combine(Dir, name + ".json");
            return File.Exists(p) ? JsonSerializer.Deserialize<T>(File.ReadAllText(p)) ?? new T() : new T(); 
        }
        catch { return new T(); }
    }

    public static void Save<T>(string name, T v)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Path.Combine(Dir, name + ".json"), JsonSerializer.Serialize(v));
    }
}