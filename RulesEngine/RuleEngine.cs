using RulesEngine.ActionResults;

namespace RulesEngine;

internal sealed class RuleEngine<TRuleContext>(IRuleProvider<TRuleContext> ruleProvider) : IRuleEngine<TRuleContext>
    where TRuleContext : RuleContextBase
{
    private readonly IRuleProvider<TRuleContext> _ruleProvider = ruleProvider;

    public async Task<IReadOnlyList<IRuleActionResult>> ExecuteAsync(
        TRuleContext context,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        var rules = await _ruleProvider.GetRulesAsync(cancellationToken);
        var results = new List<IRuleActionResult>();

        foreach (var rule in rules.OrderByDescending(rule => rule.Priority))
        {
            if (rule.Match(context) is not { } match)
            {
                continue;
            }

            foreach (var result in rule.Execute(context, match))
            {
                // A lower-priority rule can decide what a higher one already did; the first wins.
                if (!results.Any(existing => existing.HasSameInputs(result)))
                {
                    results.Add(result);
                }
            }

            if (rule.ShouldStopAfterMatch)
            {
                break;
            }
        }

        return results;
    }
}
