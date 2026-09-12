using RulesEngine.ActionResults;
using RulesEngine.Actions;
using RulesEngine.Conditions;

namespace RulesEngine;

/// <summary>Conditions that decide whether a rule applies, and actions that say what it does.</summary>
public interface IRule<TRuleContext>
    where TRuleContext : RuleContextBase
{
    /// <summary>Higher runs first. Rules of equal priority keep the order the provider gave them.</summary>
    int Priority { get; }

    /// <summary>Whether a match ends the run, leaving lower-priority rules alone.</summary>
    bool ShouldStopAfterMatch { get; }

    IEnumerable<IRuleCondition<TRuleContext>> Conditions { get; }

    IEnumerable<IRuleAction<TRuleContext>> Actions { get; }

    /// <summary>What the conditions found, or null when the rule does not apply.</summary>
    RuleMatch? Match(TRuleContext context);

    IEnumerable<IRuleActionResult> Execute(TRuleContext context, RuleMatch match);
}
