using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// <see cref="RowFormatter.Cells"/> is what the panel draws and what
/// <see cref="RowFormatter.Row"/> lays out, so the two can never disagree about a
/// value. These tests pin the cell contents; RowFormatterTests pins the layout.
/// </summary>
public class RowCellsTests
{
    private static readonly Vec3 Viewer = new(0f, 0f, 0f);
    private static readonly Vec3 North = new(0f, 0f, 1f);
    private static readonly Vec3 East = new(1f, 0f, 0f);

    private static RowCells CellsFor(NearbyEntity entity, Vec3? forward = null) =>
        RowFormatter.Cells(entity, Viewer, forward ?? North);

    [Fact]
    public void Distance_is_the_ground_distance_to_one_decimal()
    {
        // Ground distance, not 3-D: this is the number you walk.
        NearbyEntity entity = new("Boar", EntityKind.Tameable, new Vec3(3f, 99f, 4f), 1, null, null);

        Assert.Equal("5.0", CellsFor(entity).Distance);
    }

    [Fact]
    public void Altitude_is_signed_and_whole()
    {
        NearbyEntity above = new("A", EntityKind.Creature, new Vec3(0f, 7.4f, 10f), 1, null, null);
        NearbyEntity below = new("B", EntityKind.Creature, new Vec3(0f, -7.4f, 10f), 1, null, null);
        NearbyEntity level = new("C", EntityKind.Creature, new Vec3(0f, 0f, 10f), 1, null, null);

        Assert.Equal("+7", CellsFor(above).Altitude);
        Assert.Equal("-7", CellsFor(below).Altitude);
        Assert.Equal("0", CellsFor(level).Altitude);
    }

    [Fact]
    public void Direction_defaults_to_degrees_from_where_the_viewer_faces()
    {
        // The default format; the three modes are covered in DirectionFormatTests.
        NearbyEntity due_north = new("N", EntityKind.Creature, new Vec3(0f, 0f, 10f), 1, null, null);

        Assert.Equal("0°", CellsFor(due_north).Direction);
        Assert.Equal("-90°", CellsFor(due_north, East).Direction);
    }

    [Fact]
    public void Level_is_rendered_as_stars()
    {
        NearbyEntity ordinary = new("A", EntityKind.Creature, new Vec3(0f, 0f, 5f), 1, null, null);
        NearbyEntity oneStar = new("B", EntityKind.Creature, new Vec3(0f, 0f, 5f), 2, null, null);

        // Blank, not "-": beside the calm awareness "-" it read as "- -", one
        // mark nobody could place under either heading.
        Assert.Equal(string.Empty, CellsFor(ordinary).Level);
        Assert.Equal("1", CellsFor(oneStar).Level);
    }

    [Fact]
    public void Status_carries_the_percentage_first_then_the_game_wording()
    {
        NearbyEntity boar = new("Boar", EntityKind.Tameable, new Vec3(0f, 0f, 5f), 2, "Hungry", 0.42f);

        Assert.Equal("42% Hungry", CellsFor(boar).Status);
    }

    [Fact]
    public void Status_is_just_the_percentage_when_there_is_no_wording()
    {
        NearbyEntity boar = new("Boar", EntityKind.Tameable, new Vec3(0f, 0f, 5f), 2, null, 0.42f);

        Assert.Equal("42%", CellsFor(boar).Status);
    }

    [Fact]
    public void Status_is_empty_rather_than_null_when_there_is_nothing_to_say()
    {
        NearbyEntity wolf = new("Wolf", EntityKind.Creature, new Vec3(0f, 0f, 5f), 1, null, null);

        Assert.Equal(string.Empty, CellsFor(wolf).Status);
    }

    [Fact]
    public void Name_is_not_truncated_in_the_cells()
    {
        // Truncation belongs to the fixed-width layout, not to the data. The panel
        // clips with the GUIStyle instead, so it must receive the full name.
        NearbyEntity entity = new(
            "AnImplausiblyLongCreatureName", EntityKind.Creature, new Vec3(0f, 0f, 5f), 1, null, null);

        Assert.Equal("AnImplausiblyLongCreatureName", CellsFor(entity).Name);
    }

    [Fact]
    public void A_missing_name_becomes_empty_rather_than_null()
    {
        NearbyEntity entity = new(null!, EntityKind.Creature, new Vec3(0f, 0f, 5f), 1, null, null);

        Assert.Equal(string.Empty, CellsFor(entity).Name);
    }

    [Fact]
    public void ColumnNames_and_alignment_line_up_with_the_cells()
    {
        // PanelView builds its values array in this order; if the two drift the
        // panel silently draws the level under ALT.
        int columns = RowFormatter.ColumnNames.Length;

        Assert.Equal(columns, RowFormatter.ColumnRightAligned.Length);
        Assert.Equal(columns, RowFormatter.ColumnOffsets().Length);

        // PanelView fills a fixed array in this order; if a column is added there
        // and not here, the panel draws a value under the wrong heading.
        Assert.Equal(
            new[] { "NAME", "DIST", "DIR", "ALT", "★", "ALERT", "STATUS" },
            RowFormatter.ColumnNames);
    }
}
