using System.Collections.ObjectModel;
using MoniHop.Core.Displays;

namespace MoniHop.Desktop.Models;

public sealed class DisplayHistoryViewModel
{
    public ObservableCollection<DisplayViewModel> ConnectedDisplays { get; } = [];

    public ObservableCollection<DisplayViewModel> AllDisplays { get; } = [];

    public void Refresh(IReadOnlyList<DisplayProfileState> states)
    {
        ArgumentNullException.ThrowIfNull(states);
        ConnectedDisplays.Clear();
        AllDisplays.Clear();

        var connectedIndex = 0;
        foreach (var state in states)
        {
            if (state.IsConnected)
            {
                connectedIndex++;
            }

            var item = DisplayViewModel.Create(state, connectedIndex);
            AllDisplays.Add(item);
            if (state.IsConnected)
            {
                ConnectedDisplays.Add(item);
            }
        }
    }
}
