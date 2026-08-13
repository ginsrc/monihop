using MoniHop.Core.ApplicationProjection;

namespace MoniHop.Windows.ApplicationProjection;

public sealed record InstalledApplication(string DisplayName, ApplicationIdentity Identity)
{
    public static InstalledApplication? Create(
        string displayName,
        string? applicationUserModelId,
        string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(displayName) ||
            displayName.Contains("卸载", StringComparison.CurrentCultureIgnoreCase))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(targetPath) &&
            string.Equals(Path.GetExtension(targetPath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return new InstalledApplication(
                displayName.Trim(),
                new ApplicationIdentity(
                    ApplicationIdentityKind.ExecutablePath,
                    Path.GetFullPath(targetPath)));
        }

        if (string.IsNullOrWhiteSpace(applicationUserModelId) ||
            applicationUserModelId.Contains("://", StringComparison.Ordinal) ||
            Path.IsPathFullyQualified(applicationUserModelId))
        {
            return null;
        }

        return new InstalledApplication(
            displayName.Trim(),
            new ApplicationIdentity(
                ApplicationIdentityKind.ApplicationUserModelId,
                applicationUserModelId));
    }
}
