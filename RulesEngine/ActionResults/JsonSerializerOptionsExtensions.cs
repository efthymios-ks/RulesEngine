using System.Reflection;
using System.Text.Json;

namespace RulesEngine.ActionResults;

public static class JsonSerializerOptionsExtensions
{
    /// <summary>Teaches these options to write and read the results declared in these assemblies.</summary>
    public static JsonSerializerOptions AddRuleActionResults(
        this JsonSerializerOptions options,
        params Assembly[] assemblies
    )
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Converters.Add(RuleActionResultJsonConverter.ForAssemblies(assemblies));

        return options;
    }
}
