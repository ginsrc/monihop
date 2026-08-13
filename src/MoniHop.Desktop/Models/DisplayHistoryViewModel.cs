using System.Collections.ObjectModel;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;

namespace MoniHop.Desktop.Models;

public sealed class DisplayHistoryViewModel
{
    public ObservableCollection<DisplayViewModel> ConnectedDisplays { get; } = [];

    public ObservableCollection<DisplayViewModel> AllDisplays { get; } = [];

    public void Refresh(IReadOnlyList<DisplayProfileState> states)
        => Refresh(states, []);

    public void Refresh(
        IReadOnlyList<DisplayProfileState> states,
        IReadOnlyList<ApplicationProjectionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(rules);
        ConnectedDisplays.Clear();
        AllDisplays.Clear();

        var connectedIndex = 0;
        foreach (var state in states)
        {
            if (state.IsConnected)
            {
                connectedIndex++;
            }

            var item = DisplayViewModel.Create(state, connectedIndex) with
            {
                ApplicationRules = FormatRuleCount(rules.Count(rule =>
                    StringComparer.OrdinalIgnoreCase.Equals(
                        rule.TargetDisplayId,
                        state.Profile.StableId))),
            };
            AllDisplays.Add(item);
            if (state.IsConnected)
            {
                ConnectedDisplays.Add(item);
            }
        }
    }

    private static string FormatRuleCount(int count) => count == 0 ? "未配置" : $"{count} 条";
}
