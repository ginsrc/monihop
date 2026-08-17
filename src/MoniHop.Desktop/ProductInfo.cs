using System.Reflection;

namespace MoniHop.Desktop;

public static class ProductInfo
{
    public static string Version { get; } = ResolveVersion();

    public const string RepositoryUrl = "https://github.com/ginsrc/monihop";
    public const string ReleasesUrl = RepositoryUrl + "/releases";
    public const string IssuesUrl = RepositoryUrl + "/issues";
    public const string ReleasesApiUrl =
        "https://api.github.com/repos/ginsrc/monihop/releases?per_page=20";

    private static string ResolveVersion()
    {
        var assembly = typeof(ProductInfo).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+', 2)[0];
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
