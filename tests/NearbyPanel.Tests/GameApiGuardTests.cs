using Mono.Cecil;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// One test per game member the plugin binds to. Each was verified against
/// Valheim 1.0.16. If one of these goes red after a game update, the plugin needs
/// attention at that exact call site — which is the whole point.
///
/// See <see cref="GameAssembly"/> for why these pass when Valheim is not installed.
/// </summary>
public class GameApiGuardTests
{
    // ----- enumeration -----------------------------------------------------

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

    [Fact]
    public void Character_GetAllCharacters_is_public_static()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Character", "GetAllCharacters");

        Assert.True(method.IsPublic);
        Assert.True(method.IsStatic);
    }

    // ----- per-creature display data --------------------------------------

    [Theory]
    [InlineData("GetHoverName")]
    [InlineData("GetCenterPoint")]
    [InlineData("GetLevel")]
    [InlineData("GetFaction")]
    [InlineData("GetHealthPercentage")]
    [InlineData("IsPlayer")]
    [InlineData("IsBoss")]
    [InlineData("IsDead")]
    public void Character_display_members_are_public(string methodName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Character", methodName).IsPublic);
    }

    [Fact]
    public void Character_Faction_is_a_public_nested_enum()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        TypeDefinition faction = GameAssembly.NestedType("Character", "Faction");

        Assert.True(faction.IsNestedPublic);
        Assert.True(faction.IsEnum);
    }

    // ----- taming ---------------------------------------------------------

    [Theory]
    [InlineData("IsTamed")]
    [InlineData("IsHungry")]
    [InlineData("GetStatusString")]
    [InlineData("GetHoverName")]
    [InlineData("GetName")]
    public void Tameable_status_members_are_public(string methodName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Tameable", methodName).IsPublic);
    }

    [Fact]
    public void Tameable_m_tamingTime_is_a_public_field()
    {
        // The denominator of the taming percentage. Serialized per prefab, so it
        // must be read off the live component, never hardcoded.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Field("Tameable", "m_tamingTime").IsPublic);
    }

    [Fact]
    public void ZDOVars_s_tameTimeLeft_is_a_public_static_field()
    {
        // Taming progress lives in the ZDO, which is why a client that does not
        // own the creature can still read it.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("ZDOVars", "s_tameTimeLeft");

        Assert.True(field.IsPublic);
        Assert.True(field.IsStatic);
    }

    [Fact]
    public void ZDO_GetFloat_takes_a_hash_and_a_default()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("ZDO", "GetFloat", "Int32", "Single");

        Assert.True(method.IsPublic);
    }

    [Fact]
    public void ZDO_GetPosition_is_public()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("ZDO", "GetPosition").IsPublic);
    }

    // ----- AI state -------------------------------------------------------

    [Fact]
    public void Character_GetBaseAI_is_public()
    {
        // How the mapper reaches IsAlerted to decide whether taming is paused.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Character", "GetBaseAI");

        Assert.True(method.IsPublic);
        Assert.Equal("BaseAI", method.ReturnType.Name);
    }

    [Theory]
    [InlineData("IsAlerted")]
    [InlineData("HaveTarget")]
    [InlineData("GetTimeSinceSpawned")]
    public void BaseAI_state_members_are_public(string methodName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("BaseAI", methodName).IsPublic);
    }
}
