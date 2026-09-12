using RulesEngine.Actions;
using RulesEngine.Conditions;

namespace RulesEngine.Tests;

public sealed class RuleTests
{
    [Fact]
    public void Match_WhenEveryConditionMatches_ShouldReturnAMatchPerTargetType()
    {
        // Arrange
        var context = CreateContext();

        var rule = new MatchAllRule<ShopContext>
        {
            Conditions = [new OrdersOver(50), new VipCustomers()]
        };

        // Act
        var match = rule.Match(context);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(2, match.Count);
        Assert.Equal(["big", "huge"], match.ItemsOf<Order>().Select(order => order.Reference));
        Assert.Equal(["vip"], match.ItemsOf<Customer>().Select(customer => customer.Name));
    }

    [Fact]
    public void Match_WhenOneConditionFindsNothing_ShouldNotMatchAtAll()
    {
        // Arrange
        var context = CreateContext(customers: []);

        var rule = new MatchAllRule<ShopContext>
        {
            Conditions = [new OrdersOver(50), new VipCustomers()]
        };

        // Act
        var match = rule.Match(context);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public void Match_WhenTwoConditionsShareATargetType_ShouldKeepOnlyWhatSatisfiesBoth()
    {
        // Arrange
        var context = CreateContext();

        var rule = new MatchAllRule<ShopContext>
        {
            Conditions = [new OrdersOver(50), new OrdersUnder(500)]
        };

        // Act
        var match = rule.Match(context);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(["big"], match.ItemsOf<Order>().Select(order => order.Reference));
    }

    [Fact]
    public void Match_WhenTheIntersectionIsEmpty_ShouldNotMatch()
    {
        // Arrange
        var context = CreateContext();

        var rule = new MatchAllRule<ShopContext>
        {
            Conditions = [new OrdersOver(500), new OrdersUnder(50)]
        };

        // Act
        var match = rule.Match(context);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public void Match_WhenAMatchAllRuleHasNoConditions_ShouldNotMatch()
    {
        // Arrange
        var rule = new MatchAllRule<ShopContext>();

        // Act
        var match = rule.Match(CreateContext());

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public void Match_WhenOnlyOneConditionMatches_ShouldMatchWithThatTypeAlone()
    {
        // Arrange
        var context = CreateContext(customers: []);

        var rule = new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(50), new VipCustomers()]
        };

        // Act
        var match = rule.Match(context);

        // Assert
        Assert.NotNull(match);
        Assert.Equal([typeof(Order)], match.TargetTypes);
    }

    [Fact]
    public void Match_WhenTwoConditionsShareATargetType_ShouldKeepWhatSatisfiesEither()
    {
        // Arrange
        var context = CreateContext();

        var rule = new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(500), new OrdersUnder(50)]
        };

        // Act
        var match = rule.Match(context);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(["huge", "small"], match.ItemsOf<Order>().Select(order => order.Reference));
    }

    [Fact]
    public void Match_WhenNoConditionMatches_ShouldReturnNull()
    {
        // Arrange
        var context = CreateContext(orders: [], customers: []);

        var rule = new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(50), new VipCustomers()]
        };

        // Act
        var match = rule.Match(context);

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public void Match_WhenAMatchAnyRuleHasNoConditions_ShouldNotMatch()
    {
        // Arrange
        var rule = new MatchAnyRule<ShopContext>();

        // Act
        var match = rule.Match(CreateContext());

        // Assert
        Assert.Null(match);
    }

    [Fact]
    public void Match_WhenContextIsNull_ShouldThrow()
    {
        // Arrange
        var matchAll = new MatchAllRule<ShopContext>();
        var matchAny = new MatchAnyRule<ShopContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => matchAll.Match(null!));
        Assert.Throws<ArgumentNullException>(() => matchAny.Match(null!));
    }

    [Fact]
    public void Execute_WhenAnActionTargetsAMatchedType_ShouldApplyItToThoseItems()
    {
        // Arrange
        var context = CreateContext();

        var rule = new MatchAllRule<ShopContext>
        {
            Conditions = [new OrdersOver(500)],
            Actions = [new DiscountOrders(10)]
        };

        var match = rule.Match(context)!;

        // Act
        var results = rule.Execute(context, match).ToArray();

        // Assert
        var discount = Assert.IsType<DiscountResult>(Assert.Single(results));

        Assert.Equal("huge", discount.OrderReference);
        Assert.Equal(10, discount.Percent);
    }

    [Fact]
    public void Execute_WhenAnActionTargetsAnUnmatchedType_ShouldSkipIt()
    {
        // Arrange
        var context = CreateContext(customers: []);

        var rule = new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(50), new VipCustomers()],
            Actions = [new DiscountOrders(10), new GreetCustomers()]
        };

        var match = rule.Match(context)!;

        // Act
        var results = rule.Execute(context, match).ToArray();

        // Assert
        Assert.All(results, result => Assert.IsType<DiscountResult>(result));
    }

    [Fact]
    public void Execute_WhenThereAreSeveralActions_ShouldCollectEveryResult()
    {
        // Arrange
        var context = CreateContext();

        var rule = new MatchAllRule<ShopContext>
        {
            Conditions = [new OrdersOver(500), new VipCustomers()],
            Actions = [new DiscountOrders(10), new GreetCustomers()]
        };

        var match = rule.Match(context)!;

        // Act
        var results = rule.Execute(context, match).ToArray();

        // Assert
        Assert.Equal(2, results.Length);
        Assert.Contains(results, result => result is DiscountResult);
        Assert.Contains(results, result => result is GreetingResult);
    }

    [Fact]
    public void Execute_WhenAnArgumentIsNull_ShouldThrow()
    {
        // Arrange
        var context = CreateContext();

        var rule = new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(50)]
        };

        var match = rule.Match(context)!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => rule.Execute(null!, match));
        Assert.Throws<ArgumentNullException>(() => rule.Execute(context, null!));
    }

    [Fact]
    public void Priority_WhenSet_ShouldBeReadBack()
    {
        // Arrange
        var rule = new MatchAnyRule<ShopContext>
        {
            Priority = 5,
            ShouldStopAfterMatch = true
        };

        // Act & Assert
        Assert.Equal(5, rule.Priority);
        Assert.True(rule.ShouldStopAfterMatch);
    }

    [Fact]
    public void TargetType_WhenAConditionOrActionIsBuilt_ShouldBeItsTypeArgument()
    {
        // Arrange
        IRuleCondition<ShopContext> condition = new OrdersOver(1);
        IRuleAction<ShopContext> action = new GreetCustomers();

        // Act & Assert
        Assert.Equal(typeof(Order), condition.TargetType);
        Assert.Equal(typeof(Customer), action.TargetType);
    }

    [Fact]
    public void ItemsOf_WhenTheTypeDidNotMatch_ShouldBeEmpty()
    {
        // Arrange
        var context = CreateContext(customers: []);

        var rule = new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(50), new VipCustomers()]
        };

        // Act
        var match = rule.Match(context)!;

        // Assert
        Assert.Empty(match.ItemsOf<Customer>());
        Assert.Empty(match.ItemsOf(typeof(Customer)));
    }

    [Fact]
    public void ItemsOf_WhenTheTypeIsNull_ShouldThrow()
    {
        // Arrange
        var rule = new MatchAnyRule<ShopContext>
        {
            Conditions = [new OrdersOver(50)]
        };

        var match = rule.Match(CreateContext())!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => match.ItemsOf(null!));
    }

    private static ShopContext CreateContext(IReadOnlyList<Order>? orders = null, IReadOnlyList<Customer>? customers = null)
        => new()
        {
            Orders = orders ?? [new Order("small", 10), new Order("big", 100), new Order("huge", 1_000)],
            Customers = customers ?? [new Customer("vip", IsVip: true), new Customer("regular", IsVip: false)]
        };
}
