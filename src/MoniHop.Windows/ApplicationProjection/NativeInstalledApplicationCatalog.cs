using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MoniHop.Windows.ApplicationProjection;

[SupportedOSPlatform("windows")]
public sealed class NativeInstalledApplicationCatalog : IInstalledApplicationCatalog
{
    public IReadOnlyList<InstalledApplication> ReadAll()
    {
        var shellType = Type.GetTypeFromProgID("Shell.Application") ??
            throw new InvalidOperationException("Windows Shell application catalog is unavailable.");
        dynamic? shell = null;
        dynamic? folder = null;
        dynamic? items = null;
        var applications = new List<InstalledApplication>();

        try
        {
            shell = Activator.CreateInstance(shellType) ??
                throw new InvalidOperationException("Windows Shell application catalog could not be opened.");
            folder = shell.NameSpace("shell:AppsFolder") ??
                throw new InvalidOperationException("Windows AppsFolder is unavailable.");
            items = folder.Items();
            foreach (dynamic item in items)
            {
                try
                {
                    var application = InstalledApplication.Create(
                        Convert.ToString(item.Name) ?? string.Empty,
                        Convert.ToString(item.ExtendedProperty("System.AppUserModel.ID")),
                        Convert.ToString(item.ExtendedProperty("System.Link.TargetParsingPath")));
                    if (application is not null)
                    {
                        applications.Add(application);
                    }
                }
                finally
                {
                    ReleaseComObject(item);
                }
            }
        }
        finally
        {
            ReleaseComObject(items);
            ReleaseComObject(folder);
            ReleaseComObject(shell);
        }

        return applications
            .GroupBy(
                application => $"{application.Identity.Kind}:{application.Identity.Value}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(application => application.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            _ = Marshal.FinalReleaseComObject(value);
        }
    }
}
