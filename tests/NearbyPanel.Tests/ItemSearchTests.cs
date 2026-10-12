using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class ItemSearchTests
{
    private static readonly FoundItem Saddle = new("Asksvin saddle", "SaddleAsksvin", 1);

    [Theory]
    [InlineData("saddle")]
    [InlineData("SADDLE")]
    [InlineData("  asksvin  ")]
    [InlineData("SaddleAsk")]
    public void Matches_on_the_shown_name_or_the_prefab_ignoring_case_and_padding(string query)
    {
        Assert.True(ItemSearch.Matches(Saddle, query));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void An_empty_query_matches_nothing(string? query)
    {
        Assert.False(ItemSearch.Matches(Saddle, query));
    }

    [Fact]
    public void A_missing_prefab_still_matches_on_the_name()
    {
        Assert.True(ItemSearch.Matches(new FoundItem("Asksvin saddle", null, 1), "saddle"));
        Assert.False(ItemSearch.Matches(new FoundItem("Asksvin saddle", null, 1), "SaddleAsksvin"));
    }

    [Fact]
    public void Describe_joins_names_with_their_stack()
    {
        string text = ItemSearch.Describe(new[]
        {
            new FoundItem("Asksvin saddle", "SaddleAsksvin", 1),
            new FoundItem("Lox saddle", "SaddleLox", 2),
        });

        Assert.Equal("Asksvin saddle ×1, Lox saddle ×2", text);
    }

    [Fact]
    public void Describe_merges_stacks_of_the_same_item_keeping_first_seen_order()
    {
        string text = ItemSearch.Describe(new[]
        {
            new FoundItem("Lox saddle", "SaddleLox", 2),
            new FoundItem("Asksvin saddle", "SaddleAsksvin", 1),
            new FoundItem("Lox saddle", "SaddleLox", 1),
        });

        Assert.Equal("Lox saddle ×3, Asksvin saddle ×1", text);
    }

    [Fact]
    public void Describe_of_nothing_is_empty()
    {
        Assert.Equal(string.Empty, ItemSearch.Describe(new FoundItem[0]));
    }
}
