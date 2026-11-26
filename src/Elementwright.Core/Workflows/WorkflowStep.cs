using Elementwright.Core.Automation;

namespace Elementwright.Core.Workflows;

/// <summary>A single action of a <see cref="Workflow"/>.</summary>
public sealed record WorkflowStep
{
    public const int DefaultTimeoutMs = 10_000;

    /// <summary>Stable identifier, used to refer to the step in logs and validation results.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Optional human-readable label.</summary>
    public string? Name { get; init; }

    public required WorkflowActionType Action { get; init; }

    /// <summary>Element the action is applied to. Not used by <see cref="WorkflowActionType.Delay"/>.</summary>
    public ElementSelector? Selector { get; init; }

    /// <summary>Text for <see cref="WorkflowActionType.SetValue"/>.</summary>
    public string? Value { get; init; }

    /// <summary>How long to wait for the element to appear before the step fails.</summary>
    public int TimeoutMs { get; init; } = DefaultTimeoutMs;

    /// <summary>Pause length for <see cref="WorkflowActionType.Delay"/>.</summary>
    public int DelayMs { get; init; }
}
