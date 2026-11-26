namespace Elementwright.Core.Automation;

/// <summary>A structured set of UI Automation properties used to identify an element.</summary>
public sealed record ElementSelector
{
    public string? AutomationId { get; init; }
    public string? Name { get; init; }
    public string? ClassName { get; init; }
    public string? ControlType { get; init; }

    public static ElementSelector FromNode(ElementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return !string.IsNullOrWhiteSpace(node.AutomationId)
            ? new ElementSelector
            {
                AutomationId = node.AutomationId,
                ControlType = NullIfEmpty(node.ControlType)
            }
            : new ElementSelector
            {
                Name = NullIfEmpty(node.Name),
                ClassName = NullIfEmpty(node.ClassName),
                ControlType = NullIfEmpty(node.ControlType)
            };
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
