namespace Elementwright.Core.Automation;

public sealed class ElementNode
{
    public required string Name { get; init; }
    public required string AutomationId { get; init; }
    public required string ClassName { get; init; }
    public required string ControlType { get; init; }
    public required bool IsEnabled { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }

    public required string Selector { get; init; }

    public List<ElementNode> Children { get; } = [];

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name) ? $"<{ControlType}>" : $"{ControlType}: {Name}";
}
