using MoniHop.Core.ApplicationProjection;

namespace MoniHop.Desktop.Models;

public sealed record ProjectionLayoutViewModel(ProjectionLayout Layout, string Name)
{
    public override string ToString() => Name;
}
