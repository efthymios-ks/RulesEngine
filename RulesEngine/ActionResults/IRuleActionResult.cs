namespace RulesEngine.ActionResults;

/// <summary>
/// What an action decided, kept apart from doing it. A result can be stored, sent over the wire and
/// applied later, which is why it carries its own inputs rather than a closure.
/// </summary>
public interface IRuleActionResult
{
    /// <summary>
    /// Whether this result says the same thing as another. The engine keeps the first of two results
    /// that agree, so a rule cannot restate what an earlier one already decided.
    /// </summary>
    bool HasSameInputs(IRuleActionResult other);

    /// <summary>Applies the decision. False when it could not be applied.</summary>
    bool Apply(object context);
}
