using RulesEngine.ActionResults;
using RulesEngine.Actions;
using RulesEngine.Conditions;

namespace RulesEngine;

/// <summary>
/// Everything a rule does apart from deciding what counts as a match, which is what
/// <see cref="MatchAllRule{TRuleContext}"/> and <see cref="MatchAnyRule{TRuleContext}"/> differ on.
/// </summary>
public abstract class RuleBase<TRuleContext> : IRule<TRuleContext>
    where TRuleContext : RuleContextBase
{
    public int Priority { get; set; }

    public bool ShouldStopAfterMatch { get; set; }

    public IEnumerable<IRuleCondition<TRuleContext>> Conditions { get; set; } = [];

    public IEnumerable<IRuleAction<TRuleContext>> Actions { get; set; } = [];

    public abstract RuleMatch? Match(TRuleContext context);

    /// <summary>Each action is given the items of its own target type, and is skipped without any.</summary>
    public IEnumerable<IRuleActionResult> Execute(TRuleContext context, RuleMatch match)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(match);

        var results = new List<IRuleActionResult>();

        foreach (var action in Actions)
        {
            var matchedItems = match.ItemsOf(action.TargetType);

            if (matchedItems.Count > 0)
            {
                results.AddRange(action.Apply(context, matchedItems));
            }
        }

        return results;
    }

    /// <summary>
    /// Runs every condition and folds the results of those sharing a target type together. A type
    /// left with nothing is dropped, so a present type always means a real match.
    /// </summary>
    protected IReadOnlyDictionary<Type, IReadOnlyList<object>> MatchByTargetType(
        TRuleContext context,
        Func<IReadOnlyList<object>, IReadOnlyList<object>, IReadOnlyList<object>> combine
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        var itemsByTargetType = new Dictionary<Type, IReadOnlyList<object>>();

        foreach (var condition in Conditions)
        {
            IReadOnlyList<object> matchedItems = [.. condition.Match(context)];

            itemsByTargetType[condition.TargetType] = itemsByTargetType.TryGetValue(condition.TargetType, out var existing)
                ? combine(existing, matchedItems)
                : matchedItems;
        }

        foreach (var targetType in itemsByTargetType.Keys.ToArray())
        {
            if (itemsByTargetType[targetType].Count == 0)
            {
                itemsByTargetType.Remove(targetType);
            }
        }

        return itemsByTargetType;
    }
}
