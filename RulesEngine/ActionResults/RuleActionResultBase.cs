namespace RulesEngine.ActionResults;

/// <summary>A result that is applied to one kind of context.</summary>
public abstract class RuleActionResultBase<TRuleContext> : IRuleActionResult
    where TRuleContext : class
{
    /// <summary>False unless a derived result knows how to compare itself, which keeps every result.</summary>
    public virtual bool HasSameInputs(IRuleActionResult other)
        => false;

    public virtual bool Apply(TRuleContext context)
        => true;

    bool IRuleActionResult.Apply(object context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not TRuleContext typedContext)
        {
            var error = new ArgumentException(
                message: $"'{GetType().Name}' applies to '{typeof(TRuleContext).Name}', not '{context.GetType().Name}'.",
                paramName: nameof(context)
            );

            throw error;
        }

        return Apply(typedContext);
    }
}
