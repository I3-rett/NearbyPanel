namespace NearbyPanel;

/// <summary>
/// The <c>nearby_filter</c> console command: sets the text filter the panel and
/// the dump both apply, or clears it when given nothing.
///
/// The console is the input surface because the panel itself is inert — it takes
/// no clicks and never takes keyboard focus (ADR 0005), so it has nowhere to type.
/// The filter is also an ordinary config entry, so an in-game configuration manager
/// can set it too.
/// </summary>
internal static class FilterCommand
{
    private const string Name = "nearby_filter";

    public static void Register()
    {
        _ = new Terminal.ConsoleCommand(
            Name,
            "filters the nearby list by name; no argument clears it",
            Run);
    }

    private static void Run(Terminal.ConsoleEventArgs args)
    {
        // Everything after the command name, so multi-word names work.
        string text = args.Length > 1 ? string.Join(" ", args.Args, 1, args.Length - 1) : string.Empty;

        FilterState.Set(text);

        string message = FilterState.Active
            ? "nearby filter set to \"" + FilterState.Text + "\""
            : "nearby filter cleared";

        if (args.Context != null)
        {
            args.Context.AddString(message);
        }

        Plugin.Log.LogInfo(message);
    }
}
