using System.Windows.Controls;

namespace MoniHop.Desktop.Views;

public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
        OperatingSystem = Environment.OSVersion.VersionString;
        ExecutableDirectory = AppContext.BaseDirectory;
        DataContext = this;
    }

    public string OperatingSystem { get; }

    public string ExecutableDirectory { get; }
}
