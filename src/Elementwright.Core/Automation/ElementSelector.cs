using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elementwright.Core.Automation;

/// <summary>A structured set of UI Automation properties used to identify an element.</summary>
public sealed record ElementSelector
{
    public string? AutomationId { get; init; }
    public string? Name { get; init; }
    public string? ClassName { get; init; }
    public string? ControlType { get; init; }

    /// <summary>
    /// Tree path used only as a tie-breaker when several elements match the other properties.
    /// </summary>
    public string? Path { get; init; }

    public static ElementSelector FromNode(ElementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return !string.IsNullOrWhiteSpace(node.AutomationId)
            ? new ElementSelector
            {
                AutomationId = node.AutomationId,
                ControlType = NullIfEmpty(node.ControlType),
                Path = node.Path
            }
            : new ElementSelector
            {
                Name = NullIfEmpty(node.Name),
                ClassName = NullIfEmpty(node.ClassName),
                ControlType = NullIfEmpty(node.ControlType),
                Path = node.Path
            };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>Serializes the selector to compact JSON, e.g. <c>{"automationId":"btnOk","controlType":"Button","path":"2/1"}</c>.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>Parses a selector produced by <see cref="ToJson"/>.</summary>
    /// <exception cref="FormatException">The text is not a valid selector.</exception>
    public static ElementSelector Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        try
        {
            var selector = JsonSerializer.Deserialize<ElementSelector>(json, JsonOptions);
            if (selector is null || (selector.AutomationId is null && selector.Name is null &&
                                     selector.ClassName is null && selector.ControlType is null))
                throw new FormatException("The selector must specify at least one of automationId, name, className or controlType.");
            return selector;
        }
        catch (JsonException ex)
        {
            throw new FormatException("The selector is not valid JSON.", ex);
        }
    }

    public static bool TryParse(string? json, out ElementSelector? selector)
    {
        selector = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            selector = Parse(json);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
