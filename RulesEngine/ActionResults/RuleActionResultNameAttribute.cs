namespace RulesEngine.ActionResults;

/// <summary>
/// The name a result is written as when serialized. Stable across renames, which a type name is not.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RuleActionResultNameAttribute : Attribute
{
    public RuleActionResultNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }

    public string Name { get; }
}
