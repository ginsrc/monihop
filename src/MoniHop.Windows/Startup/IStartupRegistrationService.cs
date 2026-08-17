namespace MoniHop.Windows.Startup;

public interface IStartupRegistrationService
{
    void Apply(bool enabled, bool runElevated);
}
