using System.Text.Json;
using MarkdownGdi;

namespace MarkdownPad;

internal sealed class MarkdownPadApplicationState
{
    public WindowPlacementState Window { get; set; } = new();
    public EditorThemeMode ThemeMode { get; set; } = EditorThemeMode.System;
    public string? LastDirectory { get; set; }
    public int SelectedTabIndex { get; set; }
    public int NextUntitledCounter { get; set; } = 1;
    public List<string> RecentFiles { get; set; } = [];
    public List<MarkdownPadSessionDocument> OpenDocuments { get; set; } = [];
}

internal sealed class WindowPlacementState
{
    public int Left { get; set; } = 120;
    public int Top { get; set; } = 120;
    public int Width { get; set; } = 1063;
    public int Height { get; set; } = 572;
    public FormWindowState WindowState { get; set; } = FormWindowState.Normal;
}

internal sealed class MarkdownPadSessionDocument
{
    public string? FilePath { get; set; }
    public string? DefaultName { get; set; }
    public string Markdown { get; set; } = string.Empty;
    public bool Modified { get; set; }
    public float ViewScale { get; set; } = 1f;
}

internal static class ApplicationStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static string StateFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MarkdownPad",
        "session.json");

    public static MarkdownPadApplicationState Load()
    {
        try
        {
            if (!File.Exists(StateFilePath))
                return new MarkdownPadApplicationState();

            string json = File.ReadAllText(StateFilePath);
            return JsonSerializer.Deserialize<MarkdownPadApplicationState>(json, SerializerOptions)
                ?? new MarkdownPadApplicationState();
        }
        catch
        {
            return new MarkdownPadApplicationState();
        }
    }

    public static void Save(MarkdownPadApplicationState state)
    {
        string directory = Path.GetDirectoryName(StateFilePath)
            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(state, SerializerOptions);
        File.WriteAllText(StateFilePath, json);
    }
}
