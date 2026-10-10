using System.Linq;
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
    [InlineData("s_spawnTime")]
    public void ZDOVars_keys_are_public_static_ints(string fieldName)
    {
        // Taming progress, the given name and the birth instant live in the network
        // record, which is why a client that does not own the creature can still read
        // them — growth is readable for a friend's animals too. The type
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

    // ----- growth: EntityMapper.GrowthProgress ----------------------------

    [Fact]
    public void ZDO_GetLong_takes_a_hash_and_a_default()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("ZDO", "GetLong", "Int32", "Int64");

        Assert.True(method.IsPublic);
        Assert.Equal("Int64", method.ReturnType.Name);
    }

    [Fact]
    public void Growup_m_growTime_is_a_public_float_field()
    {
        // The denominator of the growth percentage, and like m_tamingTime it is
        // serialized per prefab: the assembly only carries a 60 second default that
        // every real prefab overwrites, so it must be read off the live component.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Growup", "m_growTime");

        Assert.True(field.IsPublic);
        Assert.Equal("Single", field.FieldType.Name);
    }

    [Fact]
    public void Growup_is_a_public_component()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        TypeDefinition type = GameAssembly.Type("Growup");

        Assert.True(type.IsPublic);
        Assert.Equal("MonoBehaviour", type.BaseType.Name);
    }

    [Fact]
    public void ZNet_exposes_the_shared_clock()
    {
        // Growth is measured against network time, not the local machine's, so every
        // client agrees on how old a calf is.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition time = GameAssembly.Method("ZNet", "GetTime");

        Assert.True(time.IsPublic);
        Assert.False(time.IsStatic);
        Assert.Equal("System.DateTime", time.ReturnType.FullName);

        MethodDefinition instance = GameAssembly.Method("ZNet", "get_instance");

        Assert.True(instance.IsPublic);
        Assert.True(instance.IsStatic);
    }

    [Fact]
    public void BaseAI_GetTimeSinceSpawned_still_writes_to_the_record()
    {
        // This guard is inverted on purpose. The mapper computes growth from the raw
        // record specifically because this convenient-looking getter stamps the
        // current time into the ZDO when the key is unset. If a game update ever makes
        // it a pure read, this fails and the workaround can go.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("BaseAI", "GetTimeSinceSpawned");
        bool writes = method.Body.Instructions.Any(instruction =>
            instruction.Operand is MethodReference called
            && called.DeclaringType.Name == "ZDO"
            && called.Name == "Set");

        Assert.True(writes, "BaseAI.GetTimeSinceSpawned no longer writes; EntityMapper can call it directly.");
    }

    // ----- breeding: EntityMapper.BreedingOf ------------------------------

    [Fact]
    public void Procreation_is_a_public_component()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        TypeDefinition type = GameAssembly.Type("Procreation");

        Assert.True(type.IsPublic);
        Assert.Equal("MonoBehaviour", type.BaseType.Name);
    }

    [Theory]
    [InlineData("m_totalCheckRange", "Single")]
    [InlineData("m_maxCreatures", "Int32")]
    [InlineData("m_partnerCheckRange", "Single")]
    [InlineData("m_pregnancyDuration", "Single")]
    [InlineData("m_requiredLovePoints", "Int32")]
    [InlineData("m_offspring", "GameObject")]
    [InlineData("m_seperatePartner", "GameObject")]
    [InlineData("m_noPartnerOffspring", "GameObject")]
    public void Procreation_rules_are_public_fields(string fieldName, string typeName)
    {
        // Serialized per prefab, so read off the live component, never hardcoded.
        // "seperate" is the game's own spelling.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Procreation", fieldName);

        Assert.True(field.IsPublic);
        Assert.Equal(typeName, field.FieldType.Name);
    }

    [Theory]
    [InlineData("s_lovePoints")]
    [InlineData("s_pregnant")]
    public void ZDOVars_breeding_keys_are_public_static_ints(string fieldName)
    {
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
    public void ZDO_GetInt_takes_a_hash_and_a_default()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("ZDO", "GetInt", "Int32", "Int32");

        Assert.True(method.IsPublic);
        Assert.Equal("Int32", method.ReturnType.Name);
    }

    [Fact]
    public void Procreation_ReadyForProcreation_is_a_public_pure_read()
    {
        // Called on every breeding animal in range. In 1.0.16 it only reads: IsTamed
        // refreshes a local cache, IsPregnant and IsHungry read the record. If it ever
        // starts writing, this mod would author world state — stop calling it.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Procreation", "ReadyForProcreation");

        Assert.True(method.IsPublic);
        Assert.Equal("Boolean", method.ReturnType.Name);
        Assert.DoesNotContain(method.Body.Instructions, instruction =>
            instruction.Operand is MethodReference called
            && called.DeclaringType.Name == "ZDO"
            && called.Name.StartsWith("Set"));
    }

    // ----- taming boost: EntityMapper.BoostedPlayers ----------------------

    [Theory]
    [InlineData("m_tamingSpeedMultiplierRange")]
    [InlineData("m_tamingBoostMultiplier")]
    public void Tameable_boost_settings_are_public_float_fields(string fieldName)
    {
        // Serialized per prefab: read off the live component, never hardcoded.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Tameable", fieldName);

        Assert.True(field.IsPublic);
        Assert.Equal("Single", field.FieldType.Name);
    }

    [Fact]
    public void Player_GetPlayersInRange_is_public_static()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method(
            "Player", "GetPlayersInRange", "Vector3", "Single", "List`1");

        Assert.True(method.IsPublic);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void Character_GetSEMan_is_public()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Character", "GetSEMan").IsPublic);
    }

    [Fact]
    public void SEMan_HaveStatusAttribute_is_a_public_pure_read()
    {
        // For a player this client does not own it reads s_seAttrib from the record,
        // which is how a friend's brew is visible here. If it ever writes, stop calling it.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("SEMan", "HaveStatusAttribute", "StatusAttribute");

        Assert.True(method.IsPublic);
        Assert.Equal("Boolean", method.ReturnType.Name);
        Assert.DoesNotContain(method.Body.Instructions, instruction =>
            instruction.Operand is MethodReference called
            && called.DeclaringType.Name == "ZDO"
            && called.Name.StartsWith("Set"));
    }

    [Fact]
    public void StatusAttribute_TamingBoost_is_still_eight()
    {
        // The value is compiled into the plugin, so a renumbering would silently test
        // the wrong bit.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        TypeDefinition attributes = GameAssembly.NestedType("StatusEffect", "StatusAttribute");
        FieldDefinition boost = attributes.Fields.Single(f => f.Name == "TamingBoost");

        Assert.True(attributes.IsNestedPublic);
        Assert.Equal(8, (int)boost.Constant);
    }

    // ----- fish: EntityMapper / EntityScanner -----------------------------

    [Fact]
    public void Fish_m_name_is_a_public_string_field()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Fish", "m_name");

        Assert.True(field.IsPublic);
        Assert.Equal("String", field.FieldType.Name);
    }

    [Fact]
    public void Fish_m_baits_is_a_public_field()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Fish", "m_baits");

        Assert.True(field.IsPublic);
        Assert.Equal("List`1", field.FieldType.Name);
    }

    [Fact]
    public void Fish_Instances_getter_is_public_static()
    {
        // The game keeps this list itself in OnEnable/OnDisable; the scanner only reads it.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Fish", "get_Instances");

        Assert.True(method.IsPublic);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void Fish_BaitSetting_is_a_public_nested_type_with_bait_and_chance()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        TypeDefinition setting = GameAssembly.NestedType("Fish", "BaitSetting");
        FieldDefinition bait = setting.Fields.First(f => f.Name == "m_bait");
        FieldDefinition chance = setting.Fields.First(f => f.Name == "m_chance");

        Assert.True(setting.IsNestedPublic);
        Assert.True(bait.IsPublic);
        Assert.Equal("ItemDrop", bait.FieldType.Name);
        Assert.True(chance.IsPublic);
        Assert.Equal("Single", chance.FieldType.Name);
    }

    [Fact]
    public void ItemDrop_m_itemData_is_a_public_field()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Field("ItemDrop", "m_itemData").IsPublic);
    }

    [Fact]
    public void ItemDrop_ItemData_exposes_quality_and_shared()
    {
        // A fish's size (1-3) is stored as the item's quality.
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        TypeDefinition data = GameAssembly.NestedType("ItemDrop", "ItemData");
        FieldDefinition quality = data.Fields.First(f => f.Name == "m_quality");
        FieldDefinition shared = data.Fields.First(f => f.Name == "m_shared");

        Assert.True(data.IsNestedPublic);
        Assert.True(quality.IsPublic);
        Assert.Equal("Int32", quality.FieldType.Name);
        Assert.True(shared.IsPublic);
    }

    [Fact]
    public void ItemDrop_SharedData_m_name_is_a_public_string_field()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        // Nested one level deeper than the brief assumed: ItemDrop.ItemData.SharedData.
        TypeDefinition shared = GameAssembly.NestedType("ItemDrop", "ItemData")
            .NestedTypes.First(t => t.Name == "SharedData");
        FieldDefinition name = shared.Fields.First(f => f.Name == "m_name");

        Assert.True(shared.IsNestedPublic);
        Assert.True(name.IsPublic);
        Assert.Equal("String", name.FieldType.Name);
    }
}
