using RulesEngine.ActionResults;
using RulesEngine.Actions;
using RulesEngine.Conditions;

namespace RulesEngine.Tests;

public sealed class ShopContext : RuleContextBase
{
    public IReadOnlyList<Order> Orders { get; init; } = [];

    public IReadOnlyList<Customer> Customers { get; init; } = [];

    public List<string> Applied { get; } = [];
}

public sealed record Order(string Reference, decimal Total);

public sealed record Customer(string Name, bool IsVip);

public sealed class OrdersOver(decimal threshold) : RuleConditionBase<ShopContext, Order>
{
    public override IEnumerable<Order> Match(ShopContext context)
        => context.Orders.Where(order => order.Total > threshold);
}

public sealed class OrdersUnder(decimal threshold) : RuleConditionBase<ShopContext, Order>
{
    public override IEnumerable<Order> Match(ShopContext context)
        => context.Orders.Where(order => order.Total < threshold);
}

public sealed class VipCustomers : RuleConditionBase<ShopContext, Customer>
{
    public override IEnumerable<Customer> Match(ShopContext context)
        => context.Customers.Where(customer => customer.IsVip);
}

public sealed class DiscountOrders(decimal percent) : RuleActionBase<ShopContext, Order>
{
    public override IEnumerable<IRuleActionResult> Apply(ShopContext context, IEnumerable<Order> matchedItems)
        => [.. matchedItems.Select(order => new DiscountResult
        {
            OrderReference = order.Reference,
            Percent = percent
        })];
}

public sealed class GreetCustomers : RuleActionBase<ShopContext, Customer>
{
    public override IEnumerable<IRuleActionResult> Apply(ShopContext context, IEnumerable<Customer> matchedItems)
        => [.. matchedItems.Select(customer => new GreetingResult
        {
            CustomerName = customer.Name
        })];
}

[RuleActionResultName("discount")]
public sealed class DiscountResult : RuleActionResultBase<ShopContext>
{
    public string OrderReference { get; set; } = string.Empty;

    public decimal Percent { get; set; }

    /// <summary>One discount per order, whichever rule decided it first.</summary>
    public override bool HasSameInputs(IRuleActionResult other)
        => other is DiscountResult discount && discount.OrderReference == OrderReference;

    public override bool Apply(ShopContext context)
    {
        context.Applied.Add($"discount:{OrderReference}:{Percent}");

        return true;
    }
}

[RuleActionResultName("greeting")]
public sealed class GreetingResult : RuleActionResultBase<ShopContext>
{
    public string CustomerName { get; set; } = string.Empty;
}

public sealed class ListRuleProvider(params IRule<ShopContext>[] rules) : IRuleProvider<ShopContext>
{
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<IEnumerable<IRule<ShopContext>>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;

        return Task.FromResult<IEnumerable<IRule<ShopContext>>>(rules);
    }
}
