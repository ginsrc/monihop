namespace MoniHop.Windows.ApplicationProjection;

public interface IInstalledApplicationCatalog
{
    IReadOnlyList<InstalledApplication> ReadAll();
}
