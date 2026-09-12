using RulesEngine.ActionResults;

namespace RulesEngine.Tests;

public sealed class RuleActionResultTests
{
    [Fact]
    public void Apply_WhenTheContextIsOfTheRightType_ShouldRun()
    {
        // Arrange
        var context = new ShopContext();
        IRuleActionResult result = new DiscountResult
        {
            OrderReference = "big",
            Percent = 10
        };

        // Act
        var applied = result.Apply(context);

        // Assert
        Assert.True(applied);
        Assert.Equal(["discount:big:10"], context.Applied);
    }

    [Fact]
    public void Apply_WhenTheContextIsOfAnotherType_ShouldThrowNamingBoth()
    {
        // Arrange
        IRuleActionResult result = new DiscountResult();

        // Act
        var error = Assert.Throws<ArgumentException>(() => result.Apply(new OtherContext()));

        // Assert
        Assert.Contains(nameof(DiscountResult), error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ShopContext), error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(OtherContext), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_WhenTheContextIsNull_ShouldThrow()
    {
        // Arrange
        IRuleActionResult result = new DiscountResult();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => result.Apply(null!));
    }

    [Fact]
    public void Apply_WhenTheResultDoesNotOverrideIt_ShouldSucceedAndDoNothing()
    {
        // Arrange
        var context = new ShopContext();
        IRuleActionResult result = new GreetingResult
        {
            CustomerName = "vip"
        };

        // Act
        var applied = result.Apply(context);

        // Assert
        Assert.True(applied);
        Assert.Empty(context.Applied);
    }

    [Fact]
    public void HasSameInputs_WhenTheResultDoesNotOverrideIt_ShouldBeFalse()
    {
        // Arrange
        var first = new GreetingResult { CustomerName = "vip" };
        var second = new GreetingResult { CustomerName = "vip" };

        // Act & Assert
        Assert.False(first.HasSameInputs(second));
    }

    [Fact]
    public void HasSameInputs_WhenTheResultComparesItsInputs_ShouldSayWhenTheyAgree()
    {
        // Arrange
        var first = new DiscountResult { OrderReference = "big", Percent = 10 };
        var same = new DiscountResult { OrderReference = "big", Percent = 50 };
        var other = new DiscountResult { OrderReference = "small", Percent = 10 };

        // Act & Assert
        Assert.True(first.HasSameInputs(same));
        Assert.False(first.HasSameInputs(other));
    }

    [Fact]
    public void RuleActionResultName_WhenANameIsGiven_ShouldKeepIt()
    {
        // Arrange
        var attribute = new RuleActionResultNameAttribute("discount");

        // Act & Assert
        Assert.Equal("discount", attribute.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RuleActionResultName_WhenTheNameIsMissing_ShouldThrow(string? name)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new RuleActionResultNameAttribute(name!));
    }

    private sealed class OtherContext : RuleContextBase;
}
