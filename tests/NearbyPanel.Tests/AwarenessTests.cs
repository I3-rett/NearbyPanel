using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class AwarenessTests
{
    private static readonly Vec3 Viewer = new(0f, 0f, 0f);
    private static readonly Vec3 North = new(0f, 0f, 1f);

    private static NearbyEntity Creature(
        Awareness awareness = Awareness.Calm,
        bool hostile = false,
        bool? targetsYou = null,
        string name = "Greydwarf") =>
        new(name, EntityKind.Creature, new Vec3(0f, 0f, 10f), 1, null, null, awareness, hostile, targetsYou);

    private static RowCells CellsFor(NearbyEntity entity) => RowFormatter.Cells(entity, Viewer, North);

    // ----- the AI column ---------------------------------------------------

    [Fact]
    public void A_calm_creature_shows_a_dash()
    {
        // The deer that has not noticed you.
        Assert.Equal("-", CellsFor(Creature()).Awareness);
    }

    [Fact]
    public void A_creature_with_a_target_but_no_alert_shows_a_question_mark()
    {
        Assert.Equal("?", CellsFor(Creature(Awareness.Tracking)).Awareness);
    }

    [Fact]
    public void An_alerted_creature_shows_an_exclamation_mark()
    {
        Assert.Equal("!", CellsFor(Creature(Awareness.Alerted)).Awareness);
    }

    [Fact]
    public void A_creature_known_to_be_targeting_you_says_so()
    {
        // The deathsquito that took aggro while you were not looking.
        NearbyEntity squito = Creature(Awareness.Alerted, hostile: true, targetsYou: true, name: "Deathsquito");

        Assert.Equal("!you", CellsFor(squito).Awareness);
    }

    [Fact]
    public void An_unknown_target_is_shown_as_alerted_not_as_safe()
    {
        // TargetsYou is null when the creature is simulated by someone else, so the
        // truth is unavailable. It must read as a plain alert, never as "not you":
        // claiming safety we cannot verify is the failure that gets someone killed.
        NearbyEntity unknown = Creature(Awareness.Alerted, hostile: true, targetsYou: null);
        NearbyEntity notYou = Creature(Awareness.Alerted, hostile: true, targetsYou: false);

        Assert.Equal("!", CellsFor(unknown).Awareness);
        Assert.Equal("!", CellsFor(notYou).Awareness);
    }

    // ----- the threat flag -------------------------------------------------

    [Fact]
    public void Threat_needs_both_hostility_and_an_alert()
    {
        Assert.True(CellsFor(Creature(Awareness.Alerted, hostile: true)).Threat);
        Assert.False(CellsFor(Creature(Awareness.Alerted)).Threat);
        Assert.False(CellsFor(Creature(Awareness.Tracking, hostile: true)).Threat);
        Assert.False(CellsFor(Creature(Awareness.Calm, hostile: true)).Threat);
    }

    [Fact]
    public void A_tamed_animal_is_never_a_threat_however_alert_it_is()
    {
        NearbyEntity wolf = new(
            "Fenrir", EntityKind.Tameable, new Vec3(0f, 0f, 5f), 2, "Happy", null, Awareness.Alerted, false);

        Assert.False(CellsFor(wolf).Threat);
        Assert.Equal("!", CellsFor(wolf).Awareness);
    }

    // ----- the status column -----------------------------------------------

    [Fact]
    public void A_hostile_creature_with_nothing_else_to_report_says_hostile()
    {
        // Not always obvious from the name: an aggravated dvergr reads the same as
        // a neutral one.
        Assert.Equal("hostile", CellsFor(Creature(hostile: true)).Status);
    }

    [Fact]
    public void A_harmless_creature_leaves_the_status_empty()
    {
        Assert.Equal(string.Empty, CellsFor(Creature()).Status);
    }

    [Fact]
    public void Taming_information_wins_over_the_hostile_note()
    {
        NearbyEntity boar = new(
            "Boar", EntityKind.Tameable, new Vec3(0f, 0f, 5f), 1, "Hungry", 0.42f, Awareness.Alerted, true);

        Assert.Equal("42% Hungry", CellsFor(boar).Status);
    }

    // ----- layout ----------------------------------------------------------

    [Fact]
    public void The_AI_column_is_wide_enough_for_its_longest_value()
    {
        NearbyEntity squito = Creature(Awareness.Alerted, hostile: true, targetsYou: true, name: "Deathsquito");

        string row = RowFormatter.Row(squito, Viewer, North);
        int[] offsets = RowFormatter.ColumnOffsets();
        int width = offsets[6] - offsets[5] - 1;

        Assert.Equal("!you", row.Substring(offsets[5], width).Trim());
    }

    [Fact]
    public void The_header_still_lines_up_now_that_a_column_was_added()
    {
        string header = RowFormatter.Header;
        int[] offsets = RowFormatter.ColumnOffsets();
        string[] names = RowFormatter.ColumnNames;

        Assert.Equal(7, names.Length);

        for (int i = 0; i < names.Length; i++)
        {
            int width = i + 1 < offsets.Length ? offsets[i + 1] - offsets[i] - 1 : 0;
            string span = width <= 0 ? header.Substring(offsets[i]) : header.Substring(offsets[i], width);

            Assert.Equal(names[i], span.Trim());
        }
    }
}
