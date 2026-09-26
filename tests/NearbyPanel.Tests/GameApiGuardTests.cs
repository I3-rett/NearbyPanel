using Mono.Cecil;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// One test per game member the plugin actually binds to — no more and no less.
/// A guard for a member the code does not use is a false alarm that sends a future
/// maintainer hunting a call site that does not exist, so this list is kept in step
/// with <c>src/NearbyPanel.Plugin</c> rather than with anything aspirational.
///
/// Each was verified against Valheim 1.0.16. If one goes red after a game update,
/// the plugin needs attention at that exact call site.
///
/// See <see cref="GameAssembly"/> for what happens when Valheim is not installed,
/// and <see cref="GuardsAreArmedTests"/> for why that cannot pass unnoticed.
/// </summary>
public class GameApiGuardTests
{
    // ----- enumeration: EntityScanner -------------------------------------

    [Fact]
    public void Character_GetCharactersInRange_is_public_static()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method(
            "Character", "GetCharactersInRange", "Vector3", "Single", "List`1");

        Assert.True(method.IsPublic);
        Assert.True(method.IsStatic);
    }

    // ----- per-creature data: EntityMapper --------------------------------

    [Theory]
    [InlineData("IsDead")]
    [InlineData("IsPlayer")]
    [InlineData("GetLevel")]
    public void Character_members_used_by_the_mapper_are_public(string methodName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Character", methodName).IsPublic);
    }

    [Fact]
    public void Character_m_name_is_a_public_field()
    {
        // Used instead of GetHoverName(), which for a tamed animal can write back a
        // legacy author id. This mod only reads.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Character", "m_name");

        Assert.True(field.IsPublic);
        Assert.Equal("String", field.FieldType.Name);
    }

    [Fact]
    public void Player_m_localPlayer_is_a_public_static_field()
    {
        // The most-used game member in the mod: six call sites.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Player", "m_localPlayer");

        Assert.True(field.IsPublic);
        Assert.True(field.IsStatic);
        Assert.Equal("Player", field.FieldType.Name);
    }

    // ----- taming ---------------------------------------------------------

    [Theory]
    [InlineData("IsTamed")]
    [InlineData("IsHungry")]
    [InlineData("GetStatusString")]
    public void Tameable_status_members_are_public(string methodName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Tameable", methodName).IsPublic);
    }

    [Fact]
    public void Tameable_m_tamingTime_is_a_public_float_field()
    {
        // The denominator of the taming percentage. Serialized per prefab, so it
        // must be read off the live component, never hardcoded.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Tameable", "m_tamingTime");

        Assert.True(field.IsPublic);
        Assert.Equal("Single", field.FieldType.Name);
    }

    [Theory]
    [InlineData("s_tameTimeLeft")]
    [InlineData("s_tamedName")]
    public void ZDOVars_keys_are_public_static_ints(string fieldName)
    {
        // Taming progress and the given name live in the network record, which is
        // why a client that does not own the creature can still read them. The type
        // matters too: if these became a wrapper struct, GetFloat(Int32, Single)
        // would still exist and the guard would stay green while the call broke.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("ZDOVars", fieldName);

        Assert.True(field.IsPublic);
        Assert.True(field.IsStatic);
        Assert.Equal("Int32", field.FieldType.Name);
    }

    [Fact]
    public void ZDO_GetFloat_takes_a_hash_and_a_default()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("ZDO", "GetFloat", "Int32", "Single").IsPublic);
    }

    [Fact]
    public void ZDO_GetString_takes_a_hash_and_a_default()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("ZDO", "GetString", "Int32", "String").IsPublic);
    }

    [Fact]
    public void ZNetView_exposes_IsValid_and_GetZDO()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("ZNetView", "IsValid").IsPublic);
        Assert.True(GameAssembly.Method("ZNetView", "GetZDO").IsPublic);
    }
}
