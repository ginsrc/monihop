namespace MoniHop.Core.Displays;

public interface IDisplayCatalog
{
    IReadOnlyList<DisplaySnapshot> ReadAll();
}
