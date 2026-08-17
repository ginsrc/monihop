using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Lifecycle;

public enum CloseAction
{
    Cancel,
    Hide,
    Exit,
}

public sealed record ClosePromptResult(CloseAction Action, bool Remember);

public sealed record CloseResolution(CloseAction Action, CloseBehavior? RememberedBehavior);

public static class CloseBehaviorController
{
    public static CloseResolution Resolve(
        CloseBehavior configuredBehavior,
        Func<ClosePromptResult?> prompt) => configuredBehavior switch
        {
            CloseBehavior.MinimizeToTray => new CloseResolution(CloseAction.Hide, null),
            CloseBehavior.Exit => new CloseResolution(CloseAction.Exit, null),
            CloseBehavior.Ask => ResolvePrompt(prompt()),
            _ => new CloseResolution(CloseAction.Cancel, null),
        };

    private static CloseResolution ResolvePrompt(ClosePromptResult? result)
    {
        if (result is null)
        {
            return new CloseResolution(CloseAction.Cancel, null);
        }

        CloseBehavior? remembered = result.Remember
            ? result.Action switch
            {
                CloseAction.Hide => CloseBehavior.MinimizeToTray,
                CloseAction.Exit => CloseBehavior.Exit,
                _ => null,
            }
            : null;
        return new CloseResolution(result.Action, remembered);
    }
}
