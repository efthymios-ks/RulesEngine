# RulesEngine

Rules that read a context, decide what applies, and hand back decisions rather than performing them.
Read-only: nothing here creates or edits rules.
```
RuleContextBase.cs                what the rules are asked about
Conditions/RuleConditionBase.cs   picks the items of one type a rule applies to
Actions/RuleActionBase.cs         what the rule does to those items
ActionResults/                    what it decided: comparable, serializable, applied later
RuleBase.cs                       priority, conditions, actions, execution
MatchAllRule.cs / MatchAnyRule.cs when a rule counts as matched
IRuleProvider.cs                  where rules come from
IRuleEngine.cs                    runs them in priority order
ServiceCollectionExtensions.cs    AddRuleEngine<TRuleContext, TRuleProvider>()
```

## A context and its rules

```csharp
public sealed class ShopContext : RuleContextBase
{
    public IReadOnlyList<Order> Orders { get; init; } = [];
    public IReadOnlyList<Customer> Customers { get; init; } = [];
}

public sealed class OrdersOver(decimal threshold) : RuleConditionBase<ShopContext, Order>
{
    public override IEnumerable<Order> Match(ShopContext context)
        => context.Orders.Where(order => order.Total > threshold);
}

public sealed class DiscountOrders(decimal percent) : RuleActionBase<ShopContext, Order>
{
    public override IEnumerable<IRuleActionResult> Apply(ShopContext context, IEnumerable<Order> matchedItems)
        => [.. matchedItems.Select(order => new DiscountResult { OrderReference = order.Reference, Percent = percent })];
}
```

A condition and an action each name one target type, and the rule hands each action only the items of
its own type. Both are generic over the context, so a rule cannot be built from parts of another
domain.

```csharp
var rule = new MatchAllRule<ShopContext>
{
    Priority = 10,
    ShouldStopAfterMatch = true,
    Conditions = [new OrdersOver(100), new VipCustomers()],
    Actions = [new DiscountOrders(15)]
};
```

| Rule | Applies when | Conditions sharing a target type |
| --- | --- | --- |
| `MatchAllRule` | every condition matched something | intersected — an item must satisfy all of them |
| `MatchAnyRule` | at least one condition matched | unioned — an item satisfying any is kept |

A rule with no conditions never applies. `Match` returns null when the rule does not apply, so there
is no second question to ask, and the `RuleMatch` it returns holds only the types that really matched.

## Running them

```csharp
public sealed class ShopRuleProvider(ShopDbContext database) : IRuleProvider<ShopContext>
{
    public async Task<IEnumerable<IRule<ShopContext>>> GetRulesAsync(CancellationToken cancellationToken = default)
        => await database.Rules.ToRulesAsync(cancellationToken);
}

services.AddRuleEngine<ShopContext, ShopRuleProvider>();
```

```csharp
var results = await ruleEngine.ExecuteAsync(context, cancellationToken);

foreach (var result in results)
{
    result.Apply(context);
}
```

Rules run highest `Priority` first, and equal priorities keep the order the provider gave them. A
matched rule with `ShouldStopAfterMatch` ends the run; one that did not match never does. Both the
engine and its provider are registered scoped by default, since a provider usually reads from a
database — pass a `ServiceLifetime` to change both.

Deciding and doing are kept apart: a result carries its own inputs, so it can be stored, sent
somewhere else and applied later. When two rules decide the same thing, `HasSameInputs` lets the
engine keep the first — the default says nothing is a duplicate, so results are kept until a result
type says otherwise.

## Sending results elsewhere

```csharp
[RuleActionResultName("discount")]
public sealed class DiscountResult : RuleActionResultBase<ShopContext>
{
    public string OrderReference { get; set; } = string.Empty;
    public decimal Percent { get; set; }

    public override bool HasSameInputs(IRuleActionResult other)
        => other is DiscountResult discount && discount.OrderReference == OrderReference;

    public override bool Apply(ShopContext context)
        => context.Discount(OrderReference, Percent);
}
```

```csharp
var options = new JsonSerializerOptions().AddRuleActionResults(typeof(DiscountResult).Assembly);

var json = JsonSerializer.Serialize(results, options);
// [{"Rule":"discount","OrderReference":"A-1","Percent":15}]

var restored = JsonSerializer.Deserialize<IReadOnlyList<IRuleActionResult>>(json, options);
```

The name in the attribute is what goes on the wire, so a type can be renamed without breaking what
was already stored. A result type without one is refused while scanning rather than at the first
serialization. The discriminator follows the options' naming policy, and the converter carries its
own map, so two sets of results can be serialized side by side.

## License

MIT.
