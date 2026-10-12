namespace NearbyPanel;

/// <summary>Writes a line to the in-game console and to <c>LogOutput.log</c>.</summary>
internal static class ConsoleOutput
{
    public static void Write(Terminal.ConsoleEventArgs args, string line)
    {
        // Terminal is a MonoBehaviour, so this needs Unity's null check rather than
        // ?., which would happily call into a destroyed object.
        if (args.Context != null)
        {
            args.Context.AddString(line);
        }

        Plugin.Log.LogInfo(line);
    }
}
