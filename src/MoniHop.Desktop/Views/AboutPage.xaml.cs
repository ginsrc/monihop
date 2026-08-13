using System.Windows.Controls;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Views;

public partial class AboutPage : UserControl
{
    public AboutPage(MoniHopPaths paths)
    {
        InitializeComponent();
        ArgumentNullException.ThrowIfNull(paths);
        OperatingSystem = Environment.OSVersion.VersionString;
        ExecutableDirectory = AppContext.BaseDirectory;
        DataDirectory = paths.DataDirectory;
        ConfigurationDirectory = paths.ConfigurationDirectory;
        DataContext = this;
    }

    public string OperatingSystem { get; }

    public string ExecutableDirectory { get; }

    public string DataDirectory { get; }

    public string ConfigurationDirectory { get; }
}
