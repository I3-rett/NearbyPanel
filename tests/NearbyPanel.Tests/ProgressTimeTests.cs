using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// The STATUS column can say how long is left instead of, or as well as, how far
/// along. Taming time only runs while the animal is fed, so its wording says so;
/// growth time runs regardless.
/// </summary>
public class ProgressTimeTests
{
    private static readonly Vec3 Origin = new(0f, 0f, 0f);
    private static readonly Vec3 North = new(0f, 0f, 1f);

    private static NearbyEntity Taming(float progress, float? secondsLeft, string? status = null) =>
        new("Boar", EntityKind.Tameable, new Vec3(0f, 0f, 5f), 1, status, progress,
            TamingSecondsLeft: secondsLeft);

    private static NearbyEntity Growing(float progress, float? secondsLeft, string? status = null) =>
        new("Lox", EntityKind.Tameable, new Vec3(0f, 0f, 5f), 1, status, null,
            GrowthProgress: progress, GrowthSecondsLeft: secondsLeft);

    // ----- duration -------------------------------------------------------

    [Theory]
    [InlineData(0f, "0 s")]
    [InlineData(0.2f, "1 s")]     // rounded up: never "0" while something is left
    [InlineData(45f, "45 s")]
    [InlineData(59.5f, "1 min")]
    [InlineData(60f, "1 min")]
    [InlineData(61f, "2 min")]
    [InlineData(240f, "4 min")]
    [InlineData(3599.5f, "1 h 00")]
    [InlineData(3900f, "1 h 05")]
    [InlineData(7260f, "2 h 01")]
    public void Duration_is_short_and_rounded_up(float seconds, string expected)
    {
        Assert.Equal(expected, RowFormatter.Duration(seconds));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(-5f)]
    public void Duration_survives_nonsense(float seconds)
    {
        Assert.Equal("?", RowFormatter.Duration(seconds));
    }

    // ----- taming ---------------------------------------------------------

    [Fact]
    public void Percent_is_the_default_and_unchanged()
    {
        Assert.Equal("42% Hungry", RowFormatter.StatusText(Taming(0.42f, 240f, "Hungry")));
    }

    [Fact]
    public void Taming_time_says_it_only_counts_while_fed()
    {
        Assert.Equal("4 min fed Hungry",
            RowFormatter.StatusText(Taming(0.42f, 240f, "Hungry"), ProgressFormat.Time));
    }

    [Fact]
    public void Taming_both_shows_percent_then_time()
    {
        Assert.Equal("42% · 4 min fed",
            RowFormatter.StatusText(Taming(0.42f, 240f), ProgressFormat.Both));
    }

    [Fact]
    public void Taming_without_a_known_time_falls_back_to_percent()
    {
        Assert.Equal("42%", RowFormatter.StatusText(Taming(0.42f, null), ProgressFormat.Time));
        Assert.Equal("42%", RowFormatter.StatusText(Taming(0.42f, null), ProgressFormat.Both));
    }

    // ----- growth ---------------------------------------------------------

    [Fact]
    public void Growth_time_reads_as_time_to_adult()
    {
        Assert.Equal("grown in 12 min, Hungry",
            RowFormatter.StatusText(Growing(0.62f, 700f, "Hungry"), ProgressFormat.Time));
    }

    [Fact]
    public void Growth_both_shows_percent_then_time()
    {
        Assert.Equal("62% grown · 12 min",
            RowFormatter.StatusText(Growing(0.62f, 700f), ProgressFormat.Both));
    }

    [Fact]
    public void Growth_percent_is_unchanged()
    {
        Assert.Equal("62% grown", RowFormatter.StatusText(Growing(0.62f, 700f), ProgressFormat.Percent));
    }

    [Fact]
    public void Growth_without_a_known_time_falls_back_to_percent()
    {
        Assert.Equal("62% grown", RowFormatter.StatusText(Growing(0.62f, null), ProgressFormat.Time));
    }

    // ----- plumbing -------------------------------------------------------

    [Fact]
    public void Cells_and_row_pass_the_format_through()
    {
        NearbyEntity boar = Taming(0.42f, 240f);

        Assert.Equal("4 min fed",
            RowFormatter.Cells(boar, Origin, North, DirectionFormat.Degrees, ProgressFormat.Time).Status);
        Assert.EndsWith("4 min fed",
            RowFormatter.Row(boar, Origin, North, DirectionFormat.Degrees, ProgressFormat.Time));
    }

    [Fact]
    public void A_time_still_counts_as_progress_for_the_highlight()
    {
        Assert.True(RowFormatter.Cells(Taming(0.42f, 240f), Origin, North,
            DirectionFormat.Degrees, ProgressFormat.Time).Progress);
    }
}
