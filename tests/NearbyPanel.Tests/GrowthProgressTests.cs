using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// The status column carries one percentage at a time: taming while it is under way,
/// growth once the animal is tamed. These tests pin the precedence and the wording,
/// because both are decisions rather than consequences — the adapter hands over two
/// independent measurements and this is where one of them is chosen.
/// </summary>
public class GrowthProgressTests
{
    private static readonly Vec3 Origin = new(0f, 0f, 0f);
    private static readonly Vec3 North = new(0f, 0f, 1f);
    private static readonly Vec3 Nearby = new(0f, 0f, 5f);

    private static NearbyEntity Entity(
        string? status,
        float? taming = null,
        float? growth = null,
        bool hostile = false) =>
        new(
            Name: "Lox",
            Kind: EntityKind.Tameable,
            Position: Nearby,
            Level: 1,
            Status: status,
            TamingProgress: taming,
            Hostile: hostile,
            GrowthProgress: growth);

    [Fact]
    public void Growth_reads_as_a_percentage_of_the_way_grown()
    {
        Assert.Equal("62% grown", RowFormatter.StatusText(Entity(null, growth: 0.62f)));
    }

    [Fact]
    public void Growth_keeps_the_games_own_word_after_it()
    {
        // "Hungry" is the reason a calf stops growing, so losing it to make room for
        // the percentage would trade the useful half for the pretty half.
        Assert.Equal("62% grown, Hungry", RowFormatter.StatusText(Entity("Hungry", growth: 0.62f)));
    }

    [Fact]
    public void Taming_wins_when_both_are_known()
    {
        string text = RowFormatter.StatusText(Entity("Hungry", taming: 0.25f, growth: 0.62f));

        Assert.Equal("25% Hungry", text);
        Assert.DoesNotContain("grown", text);
    }

    [Fact]
    public void Growth_surfaces_once_taming_is_no_longer_reported()
    {
        // Which is the whole point of the precedence: the adapter reports taming only
        // for an animal that is not yet tamed, so this is a tamed calf.
        Assert.Equal("8% grown, Happy", RowFormatter.StatusText(Entity("Happy", growth: 0.08f)));
    }

    [Fact]
    public void Growth_is_truncated_like_taming_is()
    {
        // The game truncates its own taming percentage, and one column showing two
        // measurements must not round one and truncate the other.
        Assert.Equal("99% grown", RowFormatter.StatusText(Entity(null, growth: 0.999f)));
    }

    [Theory]
    [InlineData(-0.5f, "0% grown")]
    [InlineData(1.5f, "100% grown")]
    public void Growth_outside_the_range_is_clamped(float progress, string expected)
    {
        Assert.Equal(expected, RowFormatter.StatusText(Entity(null, growth: progress)));
    }

    [Fact]
    public void Growth_that_cannot_be_computed_leaves_the_column_as_it_was()
    {
        // Null is "no answer", and must not render as 0% — a calf reported as freshly
        // born when nothing is known is worse than a calf reported as nothing at all.
        Assert.Equal("Happy", RowFormatter.StatusText(Entity("Happy")));
    }

    [Fact]
    public void An_adult_with_nothing_to_report_still_falls_through_to_hostile()
    {
        Assert.Equal("hostile", RowFormatter.StatusText(Entity(null, hostile: true)));
    }

    [Fact]
    public void Hostile_never_displaces_a_growth_reading()
    {
        Assert.Equal("40% grown", RowFormatter.StatusText(Entity(null, growth: 0.4f, hostile: true)));
    }

    [Fact]
    public void Growth_flags_the_row_as_carrying_progress()
    {
        Assert.True(RowFormatter.HasProgress(Entity(null, growth: 0.5f)));
        Assert.True(RowFormatter.HasProgress(Entity(null, taming: 0.5f)));
        Assert.False(RowFormatter.HasProgress(Entity("Happy")));
    }

    [Fact]
    public void The_flag_and_the_text_agree_about_every_combination()
    {
        // The panel highlights on the flag and the player reads the text. If those two
        // ever disagree, a highlighted row shows no percentage or the reverse.
        float?[] values = { null, 0f, 0.5f, 1f };

        foreach (float? taming in values)
        {
            foreach (float? growth in values)
            {
                NearbyEntity entity = Entity("Happy", taming, growth);
                RowCells cells = RowFormatter.Cells(entity, Origin, North);

                Assert.Equal(cells.Status.Contains("%"), cells.Progress);
            }
        }
    }

    [Fact]
    public void The_status_cell_is_what_the_row_and_the_panel_both_show()
    {
        // Cells is the shared source of truth; a growth reading must reach the
        // fixed-width dump as well as the panel.
        NearbyEntity entity = Entity("Happy", growth: 0.62f);

        Assert.Equal(RowFormatter.StatusText(entity), RowFormatter.Cells(entity, Origin, North).Status);
        Assert.Contains("62% grown", RowFormatter.Row(entity, Origin, North));
    }
}
