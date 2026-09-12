using System.Text.Json;
using RulesEngine.ActionResults;

namespace RulesEngine.Tests;

public sealed class RuleActionResultJsonConverterTests
{
    [Fact]
    public void Write_WhenAResultIsSerialized_ShouldCarryItsNameAlongsideItsProperties()
    {
        // Arrange
        var options = CreateOptions();
        IRuleActionResult result = new DiscountResult
        {
            OrderReference = "big",
            Percent = 10
        };

        // Act
        var json = JsonSerializer.Serialize(result, options);

        // Assert
        Assert.Equal("""{"Rule":"discount","OrderReference":"big","Percent":10}""", json);
    }

    [Fact]
    public void Read_WhenTheNameIsKnown_ShouldReturnThatResult()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<IRuleActionResult>(
            """{"Rule":"discount","OrderReference":"big","Percent":10}""",
            options
        );

        // Assert
        var discount = Assert.IsType<DiscountResult>(result);

        Assert.Equal("big", discount.OrderReference);
        Assert.Equal(10, discount.Percent);
    }

    [Fact]
    public void Read_WhenTheNameDiffersInCase_ShouldStillResolveIt()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<IRuleActionResult>("""{"Rule":"DISCOUNT"}""", options);

        // Assert
        Assert.IsType<DiscountResult>(result);
    }

    [Fact]
    public void Read_WhenTheDiscriminatorIsMissing_ShouldThrow()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var error = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IRuleActionResult>("""{"OrderReference":"big"}""", options)
        );

        // Assert
        Assert.Contains("Rule", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_WhenTheNameIsUnknown_ShouldThrowNamingIt()
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var error = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IRuleActionResult>("""{"Rule":"refund"}""", options)
        );

        // Assert
        Assert.Contains("refund", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_WhenTheDiscriminatorIsNull_ShouldThrow()
    {
        // Arrange
        var options = CreateOptions();

        // Act & Assert
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<IRuleActionResult>("""{"Rule":null}""", options)
        );
    }

    [Fact]
    public void Write_WhenANamingPolicyIsSet_ShouldFollowItForTheDiscriminator()
    {
        // Arrange
        var options = CreateOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        IRuleActionResult result = new GreetingResult
        {
            CustomerName = "vip"
        };

        // Act
        var json = JsonSerializer.Serialize(result, options);

        // Assert
        Assert.Equal("""{"rule":"greeting","customerName":"vip"}""", json);
    }

    [Fact]
    public void ReadWrite_WhenAListIsRoundTripped_ShouldKeepEveryResultsType()
    {
        // Arrange
        var options = CreateOptions();
        IReadOnlyList<IRuleActionResult> results =
        [
            new DiscountResult { OrderReference = "big", Percent = 10 },
            new GreetingResult { CustomerName = "vip" }
        ];

        // Act
        var json = JsonSerializer.Serialize(results, options);
        var restored = JsonSerializer.Deserialize<IReadOnlyList<IRuleActionResult>>(json, options)!;

        // Assert
        Assert.Collection(
            restored,
            result => Assert.Equal("big", Assert.IsType<DiscountResult>(result).OrderReference),
            result => Assert.Equal("vip", Assert.IsType<GreetingResult>(result).CustomerName)
        );
    }

    [Fact]
    public void Write_WhenTheResultIsNotInTheMap_ShouldThrowNamingIt()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            Converters = { new RuleActionResultJsonConverter(new Dictionary<Type, string>()) }
        };

        IRuleActionResult result = new DiscountResult();

        // Act
        var error = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(result, options));

        // Assert
        Assert.Contains(nameof(DiscountResult), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenTwoResultsShareAName_ShouldThrowNamingBoth()
    {
        // Arrange
        var namesByType = new Dictionary<Type, string>
        {
            [typeof(DiscountResult)] = "same",
            [typeof(GreetingResult)] = "same"
        };

        // Act
        var error = Assert.Throws<ArgumentException>(() => new RuleActionResultJsonConverter(namesByType));

        // Assert
        Assert.Contains(nameof(DiscountResult), error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(GreetingResult), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenAnArgumentIsMissing_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RuleActionResultJsonConverter(null!));
        Assert.Throws<ArgumentException>(
            () => new RuleActionResultJsonConverter(new Dictionary<Type, string>(), discriminatorName: " ")
        );
    }

    [Fact]
    public void Write_WhenTheDiscriminatorIsRenamed_ShouldUseTheNewName()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new RuleActionResultJsonConverter(
                    new Dictionary<Type, string> { [typeof(GreetingResult)] = "greeting" },
                    discriminatorName: "kind"
                )
            }
        };

        IRuleActionResult result = new GreetingResult();

        // Act
        var json = JsonSerializer.Serialize(result, options);

        // Assert
        Assert.Equal("""{"kind":"greeting","CustomerName":""}""", json);
    }

    [Fact]
    public void ForAssemblies_WhenAResultHasNoName_ShouldThrowNamingIt()
    {
        // Arrange
        var assembly = typeof(UnnamedResult).Assembly;

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => RuleActionResultJsonConverter.ForAssemblies(assembly));

        // Assert
        Assert.Contains(nameof(UnnamedResult), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ForAssemblies_WhenTheAssemblyHasNoResults_ShouldReturnAConverterThatKnowsNone()
    {
        // Arrange
        var assembly = typeof(IRuleActionResult).Assembly;

        // Act
        var converter = RuleActionResultJsonConverter.ForAssemblies(assembly);

        // Assert
        Assert.True(converter.CanConvert(typeof(IRuleActionResult)));
    }

    [Fact]
    public void AddRuleActionResults_WhenCalled_ShouldAddTheConverterToTheOptions()
    {
        // Arrange
        var options = new JsonSerializerOptions();

        // Act
        options.AddRuleActionResults(typeof(IRuleActionResult).Assembly);

        // Assert
        Assert.Contains(options.Converters, converter => converter is RuleActionResultJsonConverter);
    }

    [Fact]
    public void AddRuleActionResults_WhenTheOptionsAreNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((JsonSerializerOptions)null!).AddRuleActionResults());
    }

    private static JsonSerializerOptions CreateOptions()
        => new()
        {
            Converters =
            {
                new RuleActionResultJsonConverter(new Dictionary<Type, string>
                {
                    [typeof(DiscountResult)] = "discount",
                    [typeof(GreetingResult)] = "greeting"
                })
            }
        };

    /// <summary>Left without a name on purpose, so scanning has something to complain about.</summary>
    public sealed class UnnamedResult : RuleActionResultBase<ShopContext>;
}
