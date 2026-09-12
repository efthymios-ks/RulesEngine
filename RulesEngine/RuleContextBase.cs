namespace RulesEngine;

/// <summary>
/// What a set of rules is asked about — the order being priced, the claim being assessed. Derive
/// one per domain, so its rules, conditions and actions can only be used together.
/// </summary>
public abstract class RuleContextBase;
