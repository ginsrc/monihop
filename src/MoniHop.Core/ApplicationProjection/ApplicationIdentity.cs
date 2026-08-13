namespace MoniHop.Core.ApplicationProjection;

public sealed record ApplicationIdentity
{
    public ApplicationIdentity(ApplicationIdentityKind kind, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Kind = kind;
        Value = value.Trim();
    }

    public ApplicationIdentityKind Kind { get; }

    public string Value { get; }

    public bool Matches(ApplicationIdentity other) =>
        other is not null &&
        Kind == other.Kind &&
        StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
}

public enum ApplicationIdentityKind
{
    ExecutablePath,
    ApplicationUserModelId,
}
