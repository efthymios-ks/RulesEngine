namespace RulesEngine;

/// <summary>
/// Applies when at least one condition matched. Conditions sharing a target type are unioned, so an
/// item satisfying any of them is kept.
/// </summary>
public class MatchAnyRule<TRuleContext> : RuleBase<TRuleContext>
    where TRuleContext : RuleContextBase
{
    public override RuleMatch? Match(TRuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var itemsByTargetType = MatchByTargetType(
            context,
            (existing, matched) => [.. existing.Union(matched)]
        );

        return itemsByTargetType.Count > 0 ? new RuleMatch(itemsByTargetType) : null;
    }
}
