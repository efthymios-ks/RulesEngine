namespace RulesEngine.Conditions;

/// <summary>Picks the items of one type a rule applies to.</summary>
public interface IRuleCondition<TRuleContext>
    where TRuleContext : RuleContextBase
{
    Type TargetType { get; }

    IEnumerable<object> Match(TRuleContext context);
}
