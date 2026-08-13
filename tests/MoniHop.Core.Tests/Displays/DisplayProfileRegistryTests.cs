using MoniHop.Core.Displays;

namespace MoniHop.Core.Tests.Displays;

public sealed class DisplayProfileRegistryTests
{
    [Fact]
    public void Reconcile_PreservesCustomNameAndMarksMissingDisplayDisconnected()
    {
        var seen = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);
        var profiles = new[]
        {
            new DisplayProfile("stable-a", "Work screen", "Display 1", "1920 × 1080", false, seen),
            new DisplayProfile("stable-b", null, "Display 2", "2560 × 1440", false, seen),
        };
        var current = new[] { Display("DISPLAY9", "Display 9", "stable-a", 1920, 1200) };

        var result = DisplayProfileRegistry.Reconcile(profiles, current, seen.AddDays(1));

        Assert.Collection(
            result,
            connected =>
            {
                Assert.Equal("stable-a", connected.Profile.StableId);
                Assert.Equal("Work screen", connected.DisplayName);
                Assert.True(connected.IsConnected);
                Assert.Equal("1920 × 1200", connected.Profile.LastResolution);
            },
            disconnected =>
            {
                Assert.Equal("stable-b", disconnected.Profile.StableId);
                Assert.False(disconnected.IsConnected);
                Assert.Equal("未连接", disconnected.ConnectionStatus);
            });
    }

    [Fact]
    public void Reconcile_AddsPreviouslyUnknownDisplay()
    {
        var now = new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);

        var result = DisplayProfileRegistry.Reconcile(
            [],
            [Display("DISPLAY1", "Display 1", "stable-new", 1920, 1080)],
            now);

        var record = Assert.Single(result);
        Assert.Equal("stable-new", record.Profile.StableId);
        Assert.Equal(now, record.Profile.LastSeenUtc);
        Assert.True(record.IsConnected);
    }

    [Fact]
    public void Rename_TrimsAndPersistsCustomName()
    {
        var profile = new DisplayProfile(
            "stable-a", null, "Display 1", "1920 × 1080", false, DateTimeOffset.UtcNow);

        var renamed = DisplayProfileRegistry.Rename([profile], "STABLE-A", "  Work screen  ");

        Assert.Equal("Work screen", Assert.Single(renamed).CustomName);
    }

    private static DisplaySnapshot Display(
        string deviceName,
        string displayName,
        string stableId,
        int width,
        int height) =>
        new(
            deviceName,
            displayName,
            new PixelRect(0, 0, width, height),
            new PixelRect(0, 0, width, height),
            false,
            stableId);
}
