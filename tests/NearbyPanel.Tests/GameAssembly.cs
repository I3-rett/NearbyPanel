using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace NearbyPanel.Tests;

/// <summary>
/// Reads the installed <c>assembly_valheim.dll</c> so the API guard tests can
/// assert that every game member the plugin depends on still exists, with the
/// same shape and the same visibility.
///
/// Why this exists: the plugin binds to the game at runtime by name. When Iron
/// Gate renames or hides a member, nothing fails at build time — you find out as
/// a MissingMethodException mid-session. These tests turn that into a red
/// <c>dotnet test</c> before you ever launch the game.
///
/// The game assembly is Iron Gate's property and is never committed. Point
/// VALHEIM_MANAGED at your own install if it is not in a standard location; when
/// no assembly is found the guard tests report as passing rather than failing, so
/// the repository still builds on a machine without Valheim.
/// </summary>
internal static class GameAssembly
{
    private static readonly Lazy<AssemblyDefinition?> Loaded = new(Load);

    public static bool IsAvailable => Loaded.Value != null;

    public static TypeDefinition Type(string name)
    {
        AssemblyDefinition assembly = Loaded.Value
            ?? throw new InvalidOperationException("Game assembly not available; guard with IsAvailable.");

        return assembly.MainModule.Types.FirstOrDefault(t => t.Name == name)
            ?? throw new InvalidOperationException($"Type '{name}' no longer exists in assembly_valheim.");
    }

    /// <summary>The named method, matched on parameter type names when given.</summary>
    public static MethodDefinition Method(string typeName, string methodName, params string[] parameterTypes)
    {
        List<MethodDefinition> candidates = Type(typeName).Methods
            .Where(m => m.Name == methodName)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"'{typeName}.{methodName}' no longer exists.");
        }

        if (parameterTypes.Length == 0)
        {
            return candidates[0];
        }

        MethodDefinition? match = candidates.FirstOrDefault(m =>
            m.Parameters.Count == parameterTypes.Length &&
            m.Parameters.Select(p => p.ParameterType.Name).SequenceEqual(parameterTypes));

        return match ?? throw new InvalidOperationException(
            $"'{typeName}.{methodName}' exists but no overload takes ({string.Join(", ", parameterTypes)}). " +
            $"Found: {string.Join(" | ", candidates.Select(Describe))}");
    }

    public static FieldDefinition Field(string typeName, string fieldName) =>
        Type(typeName).Fields.FirstOrDefault(f => f.Name == fieldName)
        ?? throw new InvalidOperationException($"'{typeName}.{fieldName}' no longer exists.");

    public static TypeDefinition NestedType(string typeName, string nestedName) =>
        Type(typeName).NestedTypes.FirstOrDefault(t => t.Name == nestedName)
        ?? throw new InvalidOperationException($"'{typeName}.{nestedName}' no longer exists.");

    private static string Describe(MethodDefinition method) =>
        $"{method.Name}({string.Join(", ", method.Parameters.Select(p => p.ParameterType.Name))})";

    private static AssemblyDefinition? Load()
    {
        foreach (string directory in CandidateDirectories())
        {
            string path = Path.Combine(directory, "assembly_valheim.dll");
            if (File.Exists(path))
            {
                return AssemblyDefinition.ReadAssembly(path);
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateDirectories()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("VALHEIM_MANAGED");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            yield return fromEnvironment!;
        }

        yield return @"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed";
        yield return @"C:\Program Files\Steam\steamapps\common\Valheim\valheim_Data\Managed";
    }
}
