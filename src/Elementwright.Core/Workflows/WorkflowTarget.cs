namespace Elementwright.Core.Workflows;

/// <summary>Describes the application window a workflow runs against.</summary>
public sealed record WorkflowTarget
{
    /// <summary>Process name without extension, e.g. <c>notepad</c>. Compared case-insensitively.</summary>
    public string? ProcessName { get; init; }

    /// <summary>Text the window title must contain. Compared case-insensitively.</summary>
    public string? WindowTitle { get; init; }
}
