namespace RulesEngine;

/// <summary>
/// What a rule's conditions found, grouped by the type they targeted. A type is present only when
/// something of it matched.
/// </summary>
public sealed class RuleMatch
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<object>> _itemsByTargetType;

    internal RuleMatch(IReadOnlyDictionary<Type, IReadOnlyList<object>> itemsByTargetType)
        => _itemsByTargetType = itemsByTargetType;

    public IEnumerable<Type> TargetTypes
        => _itemsByTargetType.Keys;

    public int Count
        => _itemsByTargetType.Count;

    /// <summary>The items matched for a type, empty when none were.</summary>
    public IReadOnlyList<object> ItemsOf(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        return _itemsByTargetType.TryGetValue(targetType, out var items) ? items : [];
    }

    public IReadOnlyList<TTarget> ItemsOf<TTarget>()
        => [.. ItemsOf(typeof(TTarget)).Cast<TTarget>()];
}
