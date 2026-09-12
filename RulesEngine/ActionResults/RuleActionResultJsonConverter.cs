using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RulesEngine.ActionResults;

/// <summary>
/// Writes and reads results polymorphically, by the name each one declares with a
/// <see cref="RuleActionResultNameAttribute"/>. The map is per converter, so two sets of results can
/// be serialized side by side.
/// </summary>
public sealed class RuleActionResultJsonConverter : JsonConverter<IRuleActionResult>
{
    private const string DefaultDiscriminatorName = "Rule";

    private readonly IReadOnlyDictionary<Type, string> _namesByType;
    private readonly IReadOnlyDictionary<string, Type> _typesByName;
    private readonly string _discriminatorName;

    public RuleActionResultJsonConverter(
        IReadOnlyDictionary<Type, string> namesByType,
        string discriminatorName = DefaultDiscriminatorName
    )
    {
        ArgumentNullException.ThrowIfNull(namesByType);
        ArgumentException.ThrowIfNullOrWhiteSpace(discriminatorName);

        _namesByType = namesByType;
        _discriminatorName = discriminatorName;
        _typesByName = BuildReverseMap(namesByType);
    }

    /// <summary>Every result type in these assemblies, by the name it declares.</summary>
    public static RuleActionResultJsonConverter ForAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var namesByType = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IRuleActionResult).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false })
            .Distinct()
            .ToDictionary(type => type, NameOf);

        return new RuleActionResultJsonConverter(namesByType);
    }

    public override IRuleActionResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);

        var discriminatorName = DiscriminatorNameIn(options);

        if (!document.RootElement.TryGetProperty(discriminatorName, out var discriminator))
        {
            throw new JsonException($"Missing '{discriminatorName}' discriminator.");
        }

        var name = discriminator.GetString();

        if (name is null || !_typesByName.TryGetValue(name, out var resultType))
        {
            throw new JsonException($"Unknown '{discriminatorName}' value '{name}'.");
        }

        return (IRuleActionResult?)JsonSerializer.Deserialize(document.RootElement.GetRawText(), resultType, options);
    }

    public override void Write(Utf8JsonWriter writer, IRuleActionResult value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        var resultType = value.GetType();

        if (!_namesByType.TryGetValue(resultType, out var name))
        {
            throw new JsonException($"'{resultType.Name}' is not one of the results this converter knows.");
        }

        // Serializing the concrete type first, then reopening the object, is what lets the
        // discriminator be written alongside the result's own properties.
        var element = JsonSerializer.SerializeToElement(value, resultType, options);

        writer.WriteStartObject();
        writer.WriteString(DiscriminatorNameIn(options), name);

        foreach (var property in element.EnumerateObject())
        {
            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    private static string NameOf(Type resultType)
        => resultType.GetCustomAttribute<RuleActionResultNameAttribute>()?.Name
            ?? throw new InvalidOperationException(
                $"'{resultType.FullName}' has no '{nameof(RuleActionResultNameAttribute)}'. Every result needs one to be written and read back."
            );

    private static IReadOnlyDictionary<string, Type> BuildReverseMap(IReadOnlyDictionary<Type, string> namesByType)
    {
        var typesByName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var (resultType, name) in namesByType)
        {
            if (!typesByName.TryAdd(name, resultType))
            {
                var error = new ArgumentException(
                    message: $"'{name}' names both '{typesByName[name].Name}' and '{resultType.Name}'.",
                    paramName: nameof(namesByType)
                );

                throw error;
            }
        }

        return typesByName;
    }

    /// <summary>The discriminator follows the naming policy, so it does not stand out in the payload.</summary>
    private string DiscriminatorNameIn(JsonSerializerOptions options)
        => options.PropertyNamingPolicy?.ConvertName(_discriminatorName) ?? _discriminatorName;
}
