namespace MoniHop.Desktop.Models;

public sealed record DisplayTargetViewModel(string? StableId, string Name, bool IsAvailable)
{
    public override string ToString() => Name;
}
