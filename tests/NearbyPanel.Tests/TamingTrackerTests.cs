using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class TamingTrackerTests
{
    private const float Total = 100f;
    private const int Key = 1;

    [Fact]
    public void First_sight_uses_the_value_as_written()
    {
        TamingTracker tracker = new();

        Assert.Equal(0.25f, tracker.Progress(Key, remainingSeconds: 75f, Total, now: 10f, paused: false), 3);
    }

    [Fact]
    public void Progress_is_zero_when_nothing_has_been_written_yet()
    {
        // The game defaults a missing record to the full duration.
        TamingTracker tracker = new();

        Assert.Equal(0f, tracker.Progress(Key, remainingSeconds: Total, Total, now: 0f, paused: false), 3);
    }

    [Fact]
    public void Progress_advances_between_writes_while_running()
    {
        TamingTracker tracker = new();
        tracker.Progress(Key, 75f, Total, now: 10f, paused: false);

        // Two seconds later the owner has not written again, but taming has run.
        float progress = tracker.Progress(Key, 75f, Total, now: 12f, paused: false);

        Assert.Equal(0.27f, progress, 3);
    }

    [Fact]
    public void Progress_is_held_while_paused()
    {
        TamingTracker tracker = new();
        tracker.Progress(Key, 75f, Total, now: 10f, paused: false);

        // Hungry or frightened: the owner's countdown has stopped, so must ours.
        float progress = tracker.Progress(Key, 75f, Total, now: 12f, paused: true);

        Assert.Equal(0.25f, progress, 3);
    }

    [Fact]
    public void Prediction_never_runs_past_the_next_expected_write()
    {
        TamingTracker tracker = new();
        tracker.Progress(Key, 75f, Total, now: 10f, paused: false);

        // The owner has gone quiet for a minute - out of range, or not simulating.
        float progress = tracker.Progress(Key, 75f, Total, now: 70f, paused: false);

        // Capped at one write interval, so 75 - 3 rather than 75 - 60.
        Assert.Equal(0.28f, progress, 3);
    }

    [Fact]
    public void A_new_written_value_resets_the_prediction()
    {
        TamingTracker tracker = new();
        tracker.Progress(Key, 75f, Total, now: 10f, paused: false);
        tracker.Progress(Key, 75f, Total, now: 12f, paused: false);

        // The owner writes 72 at t=13; the prediction restarts from there.
        Assert.Equal(0.28f, tracker.Progress(Key, 72f, Total, now: 13f, paused: false), 3);
        Assert.Equal(0.29f, tracker.Progress(Key, 72f, Total, now: 14f, paused: false), 3);
    }

    [Fact]
    public void Progress_is_clamped_to_one()
    {
        TamingTracker tracker = new();

        Assert.Equal(1f, tracker.Progress(Key, remainingSeconds: 0f, Total, now: 5f, paused: false), 3);
    }

    [Fact]
    public void Progress_never_goes_negative_when_prediction_overshoots()
    {
        TamingTracker tracker = new();
        tracker.Progress(Key, 1f, Total, now: 10f, paused: false);

        Assert.Equal(1f, tracker.Progress(Key, 1f, Total, now: 13f, paused: false), 3);
    }

    [Fact]
    public void A_total_of_zero_reports_no_progress_rather_than_dividing_by_it()
    {
        TamingTracker tracker = new();

        Assert.Equal(0f, tracker.Progress(Key, 10f, totalSeconds: 0f, now: 1f, paused: false), 3);
    }

    [Fact]
    public void Creatures_are_tracked_independently()
    {
        TamingTracker tracker = new();

        tracker.Progress(1, 75f, Total, now: 10f, paused: false);
        tracker.Progress(2, 50f, Total, now: 10f, paused: false);

        Assert.Equal(0.27f, tracker.Progress(1, 75f, Total, now: 12f, paused: false), 3);
        Assert.Equal(0.52f, tracker.Progress(2, 50f, Total, now: 12f, paused: false), 3);
        Assert.Equal(2, tracker.Count);
    }

    [Fact]
    public void Entries_are_forgotten_once_they_stop_being_asked_about()
    {
        TamingTracker tracker = new();
        tracker.Progress(1, 75f, Total, now: 0f, paused: false);
        tracker.Progress(2, 75f, Total, now: 0f, paused: false);

        // Only creature 2 is still in range two minutes later.
        tracker.Progress(2, 75f, Total, now: 61f, paused: false);
        tracker.Progress(2, 75f, Total, now: 122f, paused: false);

        Assert.Equal(1, tracker.Count);
    }

    [Fact]
    public void Clear_forgets_everything()
    {
        TamingTracker tracker = new();
        tracker.Progress(1, 75f, Total, now: 0f, paused: false);

        tracker.Clear();

        Assert.Equal(0, tracker.Count);
    }
}
