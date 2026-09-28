using System.Collections.Generic;
using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// The game stores love points and pregnancy, but not why an animal is not
/// breeding: crowding and the partner check are recomputed every 10 s by the owner
/// and thrown away. These tests pin the recomputation to the rule in
/// <c>Procreation.Procreate</c> and <c>SpawnSystem.GetNrOfInstances</c> (1.0.16): a
/// 3-D sphere, inclusive at the edge, the animal itself counted.
/// </summary>
public class BreedingTests
{
    private const string Boar = "Boar(Clone)";
    private const string Piglet = "Boar_piggy(Clone)";

    private static Breeding Rules(
        int love = 0,
        bool pregnant = false,
        float? pregnancyLeft = null,
        bool hungry = false,
        bool needsPartner = true,
        string partner = Boar,
        bool separatePartner = false,
        int? crowd = null,
        int? partners = null) =>
        new(
            LovePoints: love,
            RequiredLovePoints: 4,
            Pregnant: pregnant,
            PregnancySecondsLeft: pregnancyLeft,
            PregnancyDuration: 60f,
            OffspringPrefab: Piglet,
            PartnerPrefab: partner,
            SeparatePartner: separatePartner,
            NeedsPartner: needsPartner,
            CrowdRange: 10f,
            MaxCrowd: 4,
            PartnerRange: 3f,
            Hungry: hungry,
            Crowd: crowd,
            Partners: partners);

    private static NearbyEntity Animal(
        string prefab,
        float x,
        float y = 0f,
        Breeding? breeding = null,
        bool ready = true,
        string name = "Boar",
        string? status = "Happy") =>
        new(name, EntityKind.Tameable, new Vec3(x, y, 0f), 1, status, null,
            Prefab: prefab, ReadyToMate: ready, Breeding: breeding);

    private static Breeding Resolved(List<NearbyEntity> found, int index)
    {
        BreedingRules.Resolve(found);
        return found[index].Breeding!;
    }

    // ----- crowding --------------------------------------------------------

    [Fact]
    public void Crowd_counts_the_animal_itself_its_kind_and_its_offspring()
    {
        List<NearbyEntity> found = new()
        {
            Animal(Boar, 0f, breeding: Rules()),
            Animal(Boar, 2f),
            Animal(Piglet, 5f),
            Animal("Wolf(Clone)", 1f), // another species does not crowd a boar
        };

        Assert.Equal(3, Resolved(found, 0).Crowd);
    }

    [Fact]
    public void Crowd_is_a_3D_sphere_so_a_parent_in_a_tower_escapes_it()
    {
        // The breeding-pen trick: parents on a platform above the piglets.
        List<NearbyEntity> found = new()
        {
            Animal(Boar, 0f, y: 11f, breeding: Rules()),
            Animal(Piglet, 0f),
            Animal(Piglet, 1f),
            Animal(Piglet, 2f),
        };

        Assert.Equal(1, Resolved(found, 0).Crowd);
    }

    [Fact]
    public void Crowd_includes_an_animal_exactly_at_the_edge()
    {
        // The game skips only what is *further* than the range.
        List<NearbyEntity> found = new() { Animal(Boar, 0f, breeding: Rules()), Animal(Piglet, 10f) };

        Assert.Equal(2, Resolved(found, 0).Crowd);
    }

    [Fact]
    public void Crowd_counts_every_member_regardless_of_readiness()
    {
        List<NearbyEntity> found = new() { Animal(Boar, 0f, breeding: Rules()), Animal(Boar, 1f, ready: false) };

        Assert.Equal(2, Resolved(found, 0).Crowd);
    }

    // ----- partner ---------------------------------------------------------

    [Fact]
    public void Partners_count_only_ready_animals_of_the_partner_kind_within_reach()
    {
        List<NearbyEntity> found = new()
        {
            Animal(Boar, 0f, breeding: Rules()),
            Animal(Boar, 2f),               // ready, in reach
            Animal(Boar, 2.5f, ready: false), // hungry or pregnant: not a partner
            Animal(Boar, 4f),               // too far
            Animal(Piglet, 1f),             // wrong kind
        };

        // Itself included, as the game does.
        Assert.Equal(2, Resolved(found, 0).Partners);
    }

    [Fact]
    public void A_separate_partner_kind_is_the_one_counted()
    {
        List<NearbyEntity> found = new()
        {
            Animal("Hen(Clone)", 0f, breeding: Rules(partner: "Rooster(Clone)", separatePartner: true)),
            Animal("Rooster(Clone)", 1f),
        };

        Assert.Equal(1, Resolved(found, 0).Partners);
    }

    [Fact]
    public void Resolve_leaves_animals_without_breeding_untouched()
    {
        NearbyEntity wolf = Animal("Wolf(Clone)", 0f);
        List<NearbyEntity> found = new() { wolf };

        BreedingRules.Resolve(found);

        Assert.Same(wolf, found[0]);
    }

    // ----- status ----------------------------------------------------------

    private static string Status(Breeding breeding, ProgressFormat format = ProgressFormat.Percent, string? status = "Happy") =>
        RowFormatter.StatusText(Animal(Boar, 0f, breeding: breeding, status: status), format);

    [Fact]
    public void Love_points_show_when_nothing_blocks()
    {
        Assert.Equal("Love 2/4", Status(Rules(love: 2, crowd: 2, partners: 2)));
    }

    [Fact]
    public void Crowded_names_the_count_and_the_limit()
    {
        Assert.Equal("Crowded 5/4", Status(Rules(love: 2, crowd: 5, partners: 2)));
        Assert.Equal("Crowded 4/4", Status(Rules(love: 2, crowd: 4, partners: 2)));
    }

    [Fact]
    public void No_partner_when_fewer_than_two_of_its_kind_are_ready()
    {
        Assert.Equal("No partner", Status(Rules(crowd: 1, partners: 1)));
    }

    [Fact]
    public void A_separate_partner_needs_only_one()
    {
        Assert.Equal("Love 0/4", Status(Rules(separatePartner: true, crowd: 1, partners: 1)));
        Assert.Equal("No partner", Status(Rules(separatePartner: true, crowd: 1, partners: 0)));
    }

    [Fact]
    public void An_animal_that_breeds_alone_never_lacks_a_partner()
    {
        Assert.Equal("Love 1/4", Status(Rules(love: 1, needsPartner: false, crowd: 1, partners: 0)));
    }

    [Fact]
    public void Hungry_wins_over_crowding_because_the_game_checks_it_first()
    {
        // The game's own word, not a second one of ours.
        Assert.Equal("Hungry", Status(Rules(hungry: true, crowd: 9, partners: 0), status: "Hungry"));
    }

    [Fact]
    public void Crowding_wins_over_the_partner_check_because_the_game_checks_it_first()
    {
        Assert.Equal("Crowded 6/4", Status(Rules(crowd: 6, partners: 0)));
    }

    [Fact]
    public void Pregnancy_wins_over_everything()
    {
        Breeding pregnant = Rules(pregnant: true, pregnancyLeft: 45f, hungry: true, crowd: 9, partners: 0);

        Assert.Equal("Pregnant 25%", Status(pregnant, status: "Hungry"));
        Assert.Equal("Pregnant 45 s", Status(pregnant, ProgressFormat.Time));
        Assert.Equal("Pregnant 25% · 45 s", Status(pregnant, ProgressFormat.Both));
    }

    [Fact]
    public void A_pregnancy_past_its_term_is_due()
    {
        // The birth happens on the owner's next 10 s tick, not at the instant.
        Assert.Equal("Pregnant, due", Status(Rules(pregnant: true, pregnancyLeft: 0f)));
    }

    [Fact]
    public void A_pregnancy_of_unknown_length_still_says_pregnant()
    {
        Assert.Equal("Pregnant", Status(Rules(pregnant: true, pregnancyLeft: null)));
    }

    [Fact]
    public void Unresolved_crowding_falls_back_to_love_points()
    {
        Assert.Equal("Love 3/4", Status(Rules(love: 3)));
    }

    [Fact]
    public void Pregnancy_is_progress_for_the_highlight_and_love_is_not()
    {
        Assert.True(RowFormatter.HasProgress(Animal(Boar, 0f, breeding: Rules(pregnant: true, pregnancyLeft: 30f))));
        Assert.False(RowFormatter.HasProgress(Animal(Boar, 0f, breeding: Rules(love: 3))));
    }

    [Fact]
    public void Taming_and_growth_still_come_before_breeding()
    {
        NearbyEntity calf = Animal(Boar, 0f, breeding: Rules(love: 1)) with { GrowthProgress = 0.5f };

        Assert.Equal("50% grown, Happy", RowFormatter.StatusText(calf));
    }
}
