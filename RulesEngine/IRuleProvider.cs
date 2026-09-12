namespace RulesEngine;

/// <summary>Where rules come from — a database, a file, a list built in code.</summary>
public interface IRuleProvider<TRuleContext>
    where TRuleContext : RuleContextBase
{
    Task<IEnumerable<IRule<TRuleContext>>> GetRulesAsync(CancellationToken cancellationToken = default);
}
