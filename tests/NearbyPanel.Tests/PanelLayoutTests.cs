using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class PanelLayoutTests
{
    private const float ScreenW = 1920f;
    private const float ScreenH = 1080f;

    // ----- sizing ---------------------------------------------------------

    [Theory]
    [InlineData(8, 14f)]     // floored: below this, rows are illegible
    [InlineData(10, 14f)]
    [InlineData(14, 18.9f)]
    [InlineData(32, 43.2f)]
    public void RowHeight_grows_with_the_font_and_never_goes_below_the_floor(int fontSize, float expected)
    {
        Assert.Equal(expected, PanelLayout.RowHeight(fontSize), 3);
    }

    [Theory]
    [InlineData(0, 12, 0)]
    [InlineData(5, 12, 5)]
    [InlineData(20, 12, 12)]
    [InlineData(12, 12, 12)]
    [InlineData(-1, 12, 0)]
    [InlineData(5, -1, 0)]
    public void VisibleRows_respects_the_cap(int rowCount, int max, int expected)
    {
        Assert.Equal(expected, PanelLayout.VisibleRows(rowCount, max));
    }

    [Fact]
    public void Height_counts_a_title_and_a_header_even_with_no_rows()
    {
        // 2 lines + padding, so an empty panel still has somewhere to say so.
        float expected = (PanelLayout.Padding * 2f) + (2f * PanelLayout.RowHeight(14));

        Assert.Equal(expected, PanelLayout.Height(0, 12, 14), 3);
    }

    [Fact]
    public void Height_adds_one_line_for_the_more_note_when_the_list_is_cut_short()
    {
        float twelve = PanelLayout.Height(12, 12, 14);
        float thirteen = PanelLayout.Height(13, 12, 14);

        Assert.Equal(PanelLayout.RowHeight(14), thirteen - twelve, 3);
    }

    // ----- placement ------------------------------------------------------

    [Fact]
    public void TopLeft_measures_margins_from_the_top_left()
    {
        PanelRect r = PanelLayout.Place(PanelAnchor.TopLeft, 12f, 40f, 400f, 200f, ScreenW, ScreenH);

        Assert.Equal(12f, r.X, 3);
        Assert.Equal(40f, r.Y, 3);
    }

    [Fact]
    public void TopRight_measures_margins_from_the_right_edge()
    {
        PanelRect r = PanelLayout.Place(PanelAnchor.TopRight, 12f, 40f, 400f, 200f, ScreenW, ScreenH);

        Assert.Equal(ScreenW - 400f - 12f, r.X, 3);
        Assert.Equal(40f, r.Y, 3);
    }

    [Fact]
    public void BottomLeft_measures_margins_from_the_bottom_edge()
    {
        PanelRect r = PanelLayout.Place(PanelAnchor.BottomLeft, 12f, 40f, 400f, 200f, ScreenW, ScreenH);

        Assert.Equal(12f, r.X, 3);
        Assert.Equal(ScreenH - 200f - 40f, r.Y, 3);
    }

    [Fact]
    public void BottomRight_measures_from_both_far_edges()
    {
        PanelRect r = PanelLayout.Place(PanelAnchor.BottomRight, 12f, 40f, 400f, 200f, ScreenW, ScreenH);

        Assert.Equal(ScreenW - 400f - 12f, r.X, 3);
        Assert.Equal(ScreenH - 200f - 40f, r.Y, 3);
    }

    [Fact]
    public void The_same_margins_inset_the_same_amount_from_whichever_corner()
    {
        // The point of anchoring: the numbers mean the same thing everywhere.
        PanelRect tl = PanelLayout.Place(PanelAnchor.TopLeft, 30f, 50f, 400f, 200f, ScreenW, ScreenH);
        PanelRect br = PanelLayout.Place(PanelAnchor.BottomRight, 30f, 50f, 400f, 200f, ScreenW, ScreenH);

        Assert.Equal(tl.X, ScreenW - (br.X + br.Width), 3);
        Assert.Equal(tl.Y, ScreenH - (br.Y + br.Height), 3);
    }

    [Fact]
    public void A_huge_margin_cannot_push_the_panel_off_screen()
    {
        PanelRect r = PanelLayout.Place(PanelAnchor.TopLeft, 9000f, 9000f, 400f, 200f, ScreenW, ScreenH);

        Assert.True(r.X + r.Width <= ScreenW);
        Assert.True(r.Y + r.Height <= ScreenH);
        Assert.True(r.X >= 0f);
        Assert.True(r.Y >= 0f);
    }

    [Fact]
    public void A_negative_margin_is_treated_as_zero()
    {
        PanelRect r = PanelLayout.Place(PanelAnchor.TopLeft, -50f, -50f, 400f, 200f, ScreenW, ScreenH);

        Assert.Equal(0f, r.X, 3);
        Assert.Equal(0f, r.Y, 3);
    }

    [Fact]
    public void A_panel_wider_than_the_screen_is_shrunk_and_still_starts_on_screen()
    {
        PanelRect r = PanelLayout.Place(PanelAnchor.TopRight, 12f, 12f, 4000f, 200f, ScreenW, ScreenH);

        Assert.Equal(ScreenW, r.Width, 3);
        Assert.Equal(0f, r.X, 3);
    }

    [Fact]
    public void A_panel_taller_than_the_screen_is_shrunk_to_fit()
    {
        // A long list at a large font on a small screen.
        PanelRect r = PanelLayout.Place(PanelAnchor.BottomLeft, 0f, 0f, 400f, 5000f, ScreenW, ScreenH);

        Assert.Equal(ScreenH, r.Height, 3);
        Assert.Equal(0f, r.Y, 3);
    }

    // ----- column widths --------------------------------------------------

    private static float[] Edges(float inner, params float[] widths)
    {
        float[] edges = new float[widths.Length + 2];
        PanelLayout.ColumnEdges(widths, inner, edges);
        return edges;
    }

    [Fact]
    public void Columns_sit_side_by_side_and_the_last_takes_the_rest()
    {
        Assert.Equal(new[] { 0f, 140f, 200f, 684f }, Edges(684f, 140f, 60f));
    }

    [Fact]
    public void Columns_wider_than_the_panel_are_cut_at_its_edge()
    {
        // STATUS ends up with nothing rather than drawing outside the panel.
        Assert.Equal(new[] { 0f, 300f, 400f, 400f }, Edges(400f, 300f, 200f));
    }

    [Fact]
    public void A_negative_width_counts_as_zero()
    {
        Assert.Equal(new[] { 0f, 0f, 60f, 684f }, Edges(684f, -50f, 60f));
    }

    [Fact]
    public void A_width_of_zero_hides_the_column()
    {
        float[] edges = Edges(684f, 140f, 0f, 60f);

        Assert.Equal(edges[1], edges[2]);
    }
}
