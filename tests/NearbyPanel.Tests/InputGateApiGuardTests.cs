using Mono.Cecil;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// Guards for the members <see cref="NearbyPanel.InputGate"/> uses to decide
/// whether a keypress belongs to the mod or to something the player is typing in.
/// Verified against Valheim 1.0.16.
///
/// These matter more than most: if one of them disappears, the failure mode is not
/// a crash but a hotkey that fires while the player types in chat.
/// </summary>
public class InputGateApiGuardTests
{
    [Theory]
    [InlineData("Console")]
    [InlineData("TextInput")]
    [InlineData("Menu")]
    [InlineData("InventoryGui")]
    public void IsVisible_is_public_and_static(string typeName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method(typeName, "IsVisible");

        Assert.True(method.IsPublic);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void Chat_instance_is_a_public_static_property()
    {
        // Chat.m_instance is private, so the public property is the only way in.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition getter = GameAssembly.Method("Chat", "get_instance");

        Assert.True(getter.IsPublic);
        Assert.True(getter.IsStatic);
        Assert.Equal("Chat", getter.ReturnType.Name);
    }

    [Fact]
    public void Chat_HasFocus_is_public()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Chat", "HasFocus").IsPublic);
    }

    [Fact]
    public void Player_m_localPlayerExists_is_a_public_static_field()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Player", "m_localPlayerExists");

        Assert.True(field.IsPublic);
        Assert.True(field.IsStatic);
    }

    [Fact]
    public void Player_InPlaceMode_is_public()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Player", "InPlaceMode").IsPublic);
    }
}
