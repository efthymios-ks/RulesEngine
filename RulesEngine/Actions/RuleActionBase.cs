using RulesEngine.ActionResults;

namespace RulesEngine.Actions;

/// <summary>
/// An action over one target type. It is handed the items of that type the rule matched, already
/// cast.
/// </summary>
public abstract class RuleActionBase<TRuleContext, TTarget> : IRuleAction<TRuleContext>
    where TRuleContext : RuleContextBase
{
    public Type TargetType { get; } = typeof(TTarget);

    public abstract IEnumerable<IRuleActionResult> Apply(TRuleContext context, IEnumerable<TTarget> matchedItems);

    IEnumerable<IRuleActionResult> IRuleAction<TRuleContext>.Apply(
        TRuleContext context,
        IEnumerable<object> matchedItems
    ) => Apply(context, matchedItems.Cast<TTarget>());
}
