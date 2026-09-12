namespace RulesEngine.Conditions;

/// <summary>
/// A condition over one target type. The untyped side is what the engine uses; derived conditions
/// only see <typeparamref name="TTarget"/>.
/// </summary>
public abstract class RuleConditionBase<TRuleContext, TTarget> : IRuleCondition<TRuleContext>
    where TRuleContext : RuleContextBase
{
    public Type TargetType { get; } = typeof(TTarget);

    public abstract IEnumerable<TTarget> Match(TRuleContext context);

    IEnumerable<object> IRuleCondition<TRuleContext>.Match(TRuleContext context)
        => Match(context).Cast<object>();
}
