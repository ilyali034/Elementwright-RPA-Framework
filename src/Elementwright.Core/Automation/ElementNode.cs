namespace Elementwright.Core.Automation;

public sealed class ElementNode
{
    public required string Name { get; init; }
    public required string AutomationId { get; init; }
    public required string ClassName { get; init; }
    public required string ControlType { get; init; }
    public required bool IsEnabled { get; init; }
    public required bool IsOffscreen { get; init; }
    public required bool IsKeyboardFocusable { get; init; }
    public required bool HasInvokePattern { get; init; }
    public required bool HasValuePattern { get; init; }
    public required bool HasTogglePattern { get; init; }
    public required bool HasSelectionItemPattern { get; init; }
    public required string HelpText { get; init; }
    public required string Value { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required string Selector { get; init; }

    public List<ElementNode> Children { get; } = [];

    public string DisplayName
    {
        get
        {
            var name = string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name;
            var state = IsOffscreen ? " · offscreen" : string.Empty;
            return $"{ControlType}: {name}{state}";
        }
    }
}
