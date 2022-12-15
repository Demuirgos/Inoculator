using System.Text.Json;
public class Printable<T> where T : class {
    public override string ToString()
    {
        return JsonSerializer.Serialize<T>(this as T, new JsonSerializerOptions {
            WriteIndented = true
        });
    }
} 