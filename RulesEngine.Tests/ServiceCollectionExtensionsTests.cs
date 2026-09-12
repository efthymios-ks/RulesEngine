using Microsoft.Extensions.DependencyInjection;

namespace RulesEngine.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRuleEngine_WhenCalled_ShouldResolveTheEngineAndItsProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRuleEngine<ShopContext, EmptyRuleProvider>();

        // Assert
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IRuleEngine<ShopContext>>());
        Assert.IsType<EmptyRuleProvider>(scope.ServiceProvider.GetService<IRuleProvider<ShopContext>>());
    }

    [Fact]
    public void AddRuleEngine_WhenNoLifetimeIsGiven_ShouldRegisterBothAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRuleEngine<ShopContext, EmptyRuleProvider>();

        // Assert
        Assert.All(services, service => Assert.Equal(ServiceLifetime.Scoped, service.Lifetime));
    }

    [Fact]
    public void AddRuleEngine_WhenALifetimeIsGiven_ShouldUseItForBoth()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRuleEngine<ShopContext, EmptyRuleProvider>(ServiceLifetime.Singleton);

        // Assert
        Assert.All(services, service => Assert.Equal(ServiceLifetime.Singleton, service.Lifetime));
    }

    [Fact]
    public void AddRuleEngine_WhenAProviderIsAlreadyRegistered_ShouldLeaveItAlone()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRuleProvider<ShopContext>, OtherRuleProvider>();

        // Act
        services.AddRuleEngine<ShopContext, EmptyRuleProvider>();

        // Assert
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<OtherRuleProvider>(scope.ServiceProvider.GetService<IRuleProvider<ShopContext>>());
    }

    [Fact]
    public void AddRuleEngine_WhenCalledTwice_ShouldRegisterOnePairOfServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services
            .AddRuleEngine<ShopContext, EmptyRuleProvider>()
            .AddRuleEngine<ShopContext, EmptyRuleProvider>();

        // Assert
        Assert.Equal(2, services.Count);
    }

    [Fact]
    public void AddRuleEngine_WhenTheServicesAreNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => ((IServiceCollection)null!).AddRuleEngine<ShopContext, EmptyRuleProvider>()
        );
    }

    public sealed class EmptyRuleProvider : IRuleProvider<ShopContext>
    {
        public Task<IEnumerable<IRule<ShopContext>>> GetRulesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<IRule<ShopContext>>>([]);
    }

    public sealed class OtherRuleProvider : IRuleProvider<ShopContext>
    {
        public Task<IEnumerable<IRule<ShopContext>>> GetRulesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<IRule<ShopContext>>>([]);
    }
}
