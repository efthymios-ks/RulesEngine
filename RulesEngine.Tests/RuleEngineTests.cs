using RulesEngine.ActionResults;

namespace RulesEngine.Tests;

public sealed class RuleEngineTests
{
    [Fact]
    public async Task ExecuteAsync_WhenARuleMatches_ShouldReturnWhatItsActionsDecided()
    {
        // Arrange
        var context = CreateContext();

        var engine = CreateEngine(new MatchAllRule<ShopContext>
        {
            Conditions = [new OrdersOver(50)],
            Actions = [new DiscountOrders(10)]
        });

        // Act
        var results = await engine.ExecuteAsync(context);

        // Assert
        var discount = Assert.IsType<DiscountResult>(Assert.Single(results));

        Assert.Equal("big", discount.OrderReference);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRulesHaveDifferentPriorities_ShouldRunTheHighestFirst()
    {
        // Arrange
        var context = CreateContext();

        var engine = CreateEngine(
            new MatchAnyRule<ShopContext>
            {
                Priority = 1,
                Conditions = [new VipCustomers()],
                Actions = [new GreetCustomers()]
            },
            new MatchAnyRule<ShopContext>
            {
                Priority = 10,
                Conditions = [new OrdersOver(50)],
                Actions = [new DiscountOrders(10)]
            }
        );

        // Act
        var results = await engine.ExecuteAsync(context);

        // Assert
        Assert.IsType<DiscountResult>(results[0]);
        Assert.IsType<GreetingResult>(results[^1]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPrioritiesAreEqual_ShouldKeepTheOrderTheProviderGave()
    {
        // Arrange
        var context = CreateContext();

        var engine = CreateEngine(
            new MatchAnyRule<ShopContext>
            {
                Conditions = [new VipCustomers()],
                Actions = [new GreetCustomers()]
            },
            new MatchAnyRule<ShopContext>
            {
                Conditions = [new OrdersOver(50)],
                Actions = [new DiscountOrders(10)]
            }
        );

        // Act
        var results = await engine.ExecuteAsync(context);

        // Assert
        Assert.IsType<GreetingResult>(results[0]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAMatchedRuleStopsTheRun_ShouldLeaveLowerRulesAlone()
    {
        // Arrange
        var context = CreateContext();

        var engine = CreateEngine(
            new MatchAnyRule<ShopContext>
            {
                Priority = 10,
                ShouldStopAfterMatch = true,
                Conditions = [new OrdersOver(50)],
                Actions = [new DiscountOrders(10)]
            },
            new MatchAnyRule<ShopContext>
            {
                Priority = 1,
                Conditions = [new VipCustomers()],
                Actions = [new GreetCustomers()]
            }
        );

        // Act
        var results = await engine.ExecuteAsync(context);

        // Assert
        Assert.All(results, result => Assert.IsType<DiscountResult>(result));
    }

    [Fact]
    public async Task ExecuteAsync_WhenARuleThatStopsTheRunDoesNotMatch_ShouldCarryOn()
    {
        // Arrange
        var context = CreateContext(customers: []);

        var engine = CreateEngine(
            new MatchAllRule<ShopContext>
            {
                Priority = 10,
                ShouldStopAfterMatch = true,
                Conditions = [new VipCustomers()],
                Actions = [new GreetCustomers()]
            },
            new MatchAnyRule<ShopContext>
            {
                Priority = 1,
                Conditions = [new OrdersOver(50)],
                Actions = [new DiscountOrders(10)]
            }
        );

        // Act
        var results = await engine.ExecuteAsync(context);

        // Assert
        Assert.All(results, result => Assert.IsType<DiscountResult>(result));
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task ExecuteAsync_WhenALaterRuleDecidesTheSameThing_ShouldKeepTheFirstResult()
    {
        // Arrange
        var context = CreateContext();

        var engine = CreateEngine(
            new MatchAnyRule<ShopContext>
            {
                Priority = 10,
                Conditions = [new OrdersOver(50)],
                Actions = [new DiscountOrders(10)]
            },
            new MatchAnyRule<ShopContext>
            {
                Priority = 1,
                Conditions = [new OrdersOver(50)],
                Actions = [new DiscountOrders(50)]
            }
        );

        // Act
        var results = await engine.ExecuteAsync(context);

        // Assert
        var discount = Assert.IsType<DiscountResult>(Assert.Single(results));

        Assert.Equal(10, discount.Percent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAMatchedRuleHasNoActions_ShouldStillStopTheRun()
    {
        // Arrange
        var context = CreateContext();

        var engine = CreateEngine(
            new MatchAnyRule<ShopContext>
            {
                Priority = 10,
                ShouldStopAfterMatch = true,
                Conditions = [new OrdersOver(50)]
            },
            new MatchAnyRule<ShopContext>
            {
                Priority = 1,
                Conditions = [new VipCustomers()],
                Actions = [new GreetCustomers()]
            }
        );

        // Act
        var results = await engine.ExecuteAsync(context);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoRuleMatches_ShouldReturnNothing()
    {
        // Arrange
        var context = CreateContext(orders: []);

        var engine = CreateEngine(new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(50)],
            Actions = [new DiscountOrders(10)]
        });

        // Act
        var results = await engine.ExecuteAsync(context);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteAsync_WhenThereAreNoRules_ShouldReturnNothing()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        var results = await engine.ExecuteAsync(CreateContext());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteAsync_WhenContextIsNull_ShouldThrow()
    {
        // Arrange
        var engine = CreateEngine();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => engine.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenGivenACancellationToken_ShouldHandItToTheProvider()
    {
        // Arrange
        var provider = new ListRuleProvider();
        var engine = new RuleEngine<ShopContext>(provider);

        using var cancellation = new CancellationTokenSource();

        // Act
        await engine.ExecuteAsync(CreateContext(), cancellation.Token);

        // Assert
        Assert.Equal(cancellation.Token, provider.LastCancellationToken);
    }

    [Fact]
    public async Task Apply_WhenTheResultsAreApplied_ShouldChangeTheContext()
    {
        // Arrange
        var context = CreateContext();

        var engine = CreateEngine(new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(50)],
            Actions = [new DiscountOrders(10)]
        });

        var results = await engine.ExecuteAsync(context);

        // Act
        var applied = results.All(result => result.Apply(context));

        // Assert
        Assert.True(applied);
        Assert.Equal(["discount:big:10"], context.Applied);
    }

    private static IRuleEngine<ShopContext> CreateEngine(params IRule<ShopContext>[] rules)
        => new RuleEngine<ShopContext>(new ListRuleProvider(rules));

    private static ShopContext CreateContext(IReadOnlyList<Order>? orders = null, IReadOnlyList<Customer>? customers = null)
        => new()
        {
            Orders = orders ?? [new Order("small", 10), new Order("big", 100)],
            Customers = customers ?? [new Customer("vip", IsVip: true)]
        };
}
