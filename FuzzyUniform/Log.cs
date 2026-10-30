namespace FuzzyUniform;

public static class Log
{
    public static event Action<string>? Message;
    public static void Info(string text) => Message?.Invoke($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}");

    public static void ToFile(string path)
    {
        var sync = new object();
        Message += s => { lock (sync) File.AppendAllText(path, s + Environment.NewLine); };
    }
}
