namespace Elementwright.Core.Workflows;

/// <summary>An ordered list of steps that automate an application.</summary>
public sealed record Workflow
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>Version of the file format; bumped on breaking changes.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>Application window to run against. When null, the caller supplies the window.</summary>
    public WorkflowTarget? Target { get; init; }

    public IReadOnlyList<WorkflowStep> Steps { get; init; } = [];
}
