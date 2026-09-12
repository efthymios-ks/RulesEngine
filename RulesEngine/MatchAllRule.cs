namespace RulesEngine;

/// <summary>
/// Applies when every condition matched. Conditions sharing a target type are intersected, so an
/// item has to satisfy all of them to survive.
/// </summary>
public class MatchAllRule<TRuleContext> : RuleBase<TRuleContext>
    where TRuleContext : RuleContextBase
{
    public override RuleMatch? Match(TRuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var itemsByTargetType = MatchByTargetType(
            context,
            (existing, matched) => [.. existing.Intersect(matched)]
        );

        // A rule with no conditions has nothing to be true, so it never applies.
        var matchedEverything = Conditions.Any()
            && Conditions.All(condition => itemsByTargetType.ContainsKey(condition.TargetType));

        return matchedEverything ? new RuleMatch(itemsByTargetType) : null;
    }
}
