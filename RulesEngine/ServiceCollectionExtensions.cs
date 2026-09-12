using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RulesEngine;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the engine of one context and the provider its rules come from. Both share a
    /// lifetime, since the engine holds the provider; scoped by default, which a provider reading
    /// from a database needs.
    /// </summary>
    public static IServiceCollection AddRuleEngine<TRuleContext, TRuleProvider>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
        where TRuleContext : RuleContextBase
        where TRuleProvider : class, IRuleProvider<TRuleContext>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAdd(new ServiceDescriptor(
            serviceType: typeof(IRuleProvider<TRuleContext>),
            implementationType: typeof(TRuleProvider),
            lifetime: lifetime
        ));

        services.TryAdd(new ServiceDescriptor(
            serviceType: typeof(IRuleEngine<TRuleContext>),
            implementationType: typeof(RuleEngine<TRuleContext>),
            lifetime: lifetime
        ));

        return services;
    }
}
