using RulesEngine.ActionResults;

namespace RulesEngine.Actions;

/// <summary>What a rule does to the items its conditions matched.</summary>
public interface IRuleAction<TRuleContext>
    where TRuleContext : RuleContextBase
{
    Type TargetType { get; }

    IEnumerable<IRuleActionResult> Apply(TRuleContext context, IEnumerable<object> matchedItems);
}
