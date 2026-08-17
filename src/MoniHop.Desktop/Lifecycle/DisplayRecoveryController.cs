namespace MoniHop.Desktop.Lifecycle;

public static class DisplayRecoveryController
{
    public static void HandleDisplaysChanged(bool isEnabled, Action recall)
    {
        ArgumentNullException.ThrowIfNull(recall);
        if (isEnabled)
        {
            recall();
        }
    }
}
