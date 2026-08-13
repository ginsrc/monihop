namespace MoniHop.Core.Displays;

public static class DisplayProfileRegistry
{
    public static IReadOnlyList<DisplayProfileState> Reconcile(
        IReadOnlyList<DisplayProfile> profiles,
        IReadOnlyList<DisplaySnapshot> currentDisplays,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(currentDisplays);

        var known = profiles
            .Select(profile => profile.Validate())
            .ToDictionary(profile => profile.StableId, StringComparer.OrdinalIgnoreCase);
        var states = new List<DisplayProfileState>();

        foreach (var display in currentDisplays)
        {
            known.Remove(display.StableId, out var existing);
            var updated = new DisplayProfile(
                display.StableId,
                existing?.CustomName,
                display.DisplayName,
                $"{display.ResolutionWidth} × {display.ResolutionHeight}",
                existing?.IsBuiltIn ?? false,
                now,
                display.RefreshRateHz,
                display.ScalePercent,
                display.Orientation,
                display.PhysicalWidthMillimeters,
                display.PhysicalHeightMillimeters);
            states.Add(new DisplayProfileState(updated, display));
        }

        states.AddRange(
            known.Values
                .OrderByDescending(profile => profile.LastSeenUtc)
                .ThenBy(profile => profile.LastSystemName, StringComparer.CurrentCultureIgnoreCase)
                .Select(profile => new DisplayProfileState(profile, null)));

        return states;
    }

    public static IReadOnlyList<DisplayProfile> Rename(
        IReadOnlyList<DisplayProfile> profiles,
        string stableId,
        string customName)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(stableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(customName);
        var trimmedName = customName.Trim();

        var found = false;
        var result = profiles.Select(profile =>
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(profile.StableId, stableId))
            {
                return profile;
            }

            found = true;
            return profile with { CustomName = trimmedName };
        }).ToArray();

        if (!found)
        {
            throw new KeyNotFoundException($"Unknown display profile: {stableId}");
        }

        return result;
    }

    public static IReadOnlyList<DisplayProfile> Forget(
        IReadOnlyList<DisplayProfile> profiles,
        string stableId)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(stableId);
        return profiles
            .Where(profile => !StringComparer.OrdinalIgnoreCase.Equals(profile.StableId, stableId))
            .ToArray();
    }
}
