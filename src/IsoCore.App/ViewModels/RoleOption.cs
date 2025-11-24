namespace IsoCore.App.ViewModels;

public sealed class RoleOption
{
    public string Value { get; }
    public string DisplayName { get; }

    public RoleOption(string value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
}
