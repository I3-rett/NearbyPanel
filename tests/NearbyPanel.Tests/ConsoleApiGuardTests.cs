using System.Linq;
using Mono.Cecil;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// Guards for the console and localization API the <c>nearby_dump</c> command
/// binds to. Verified against Valheim 1.0.16.
/// </summary>
public class ConsoleApiGuardTests
{
    [Fact]
    public void Terminal_ConsoleCommand_is_a_public_nested_type()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.NestedType("Terminal", "ConsoleCommand").IsNestedPublic);
    }

    [Fact]
    public void Terminal_ConsoleCommand_can_be_constructed_from_a_name_description_and_handler()
    {
        // Registration happens in the constructor, so this shape is the whole
        // public contract the plugin depends on.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        TypeDefinition command = GameAssembly.NestedType("Terminal", "ConsoleCommand");

        bool hasConstructor = command.Methods.Any(m =>
            m.IsConstructor &&
            m.IsPublic &&
            m.Parameters.Count >= 3 &&
            m.Parameters[0].ParameterType.Name == "String" &&
            m.Parameters[1].ParameterType.Name == "String" &&
            m.Parameters[2].ParameterType.Name == "ConsoleEvent" &&
            m.Parameters.Skip(3).All(p => p.HasDefault));

        Assert.True(hasConstructor, "No public ConsoleCommand(string, string, ConsoleEvent, ...) with the rest optional.");
    }

    [Theory]
    [InlineData("ConsoleEvent")]
    [InlineData("ConsoleEventArgs")]
    public void Terminal_console_types_are_public(string nestedName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.NestedType("Terminal", nestedName).IsNestedPublic);
    }

    [Fact]
    public void ConsoleEventArgs_exposes_Context_so_output_can_go_back_to_the_console()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition context = GameAssembly
            .NestedType("Terminal", "ConsoleEventArgs")
            .Fields
            .FirstOrDefault(f => f.Name == "Context")!;

        Assert.NotNull(context);
        Assert.True(context.IsPublic);
        Assert.Equal("Terminal", context.FieldType.Name);
    }

    [Fact]
    public void Terminal_AddString_takes_a_single_string()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Terminal", "AddString", "String").IsPublic);
    }

    [Fact]
    public void Localization_Localize_is_public_and_takes_a_token()
    {
        // Lives in assembly_guiutils, not assembly_valheim.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Localization", "Localize", "String").IsPublic);
    }

    [Fact]
    public void Localization_instance_is_a_public_static_property()
    {
        // Guarding Localize without the singleton that reaches it covers the half
        // less likely to change.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition getter = GameAssembly.Method("Localization", "get_instance");

        Assert.True(getter.IsPublic);
        Assert.True(getter.IsStatic);
        Assert.Equal("Localization", getter.ReturnType.Name);
    }
}
