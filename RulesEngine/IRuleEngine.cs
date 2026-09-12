using RulesEngine.ActionResults;

namespace RulesEngine;

/// <summary>Runs the rules of one context and collects what they decided.</summary>
public interface IRuleEngine<TRuleContext>
    where TRuleContext : RuleContextBase
{
    Task<IReadOnlyList<IRuleActionResult>> ExecuteAsync(
        TRuleContext context,
        CancellationToken cancellationToken = default
    );
}
