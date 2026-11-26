namespace Elementwright.Core.Workflows;

/// <summary>A problem found in a workflow. <see cref="StepIndex"/> is null for workflow-level problems.</summary>
public sealed record WorkflowIssue(int? StepIndex, string? StepId, string Message)
{
    public override string ToString() =>
        StepIndex is null ? Message : $"Step {StepIndex + 1}: {Message}";
}

/// <summary>Checks that a workflow can be executed.</summary>
public static class WorkflowValidator
{
    public const int MaxTimeoutMs = 600_000;
    public const int MaxDelayMs = 3_600_000;

    public static IReadOnlyList<WorkflowIssue> Validate(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var issues = new List<WorkflowIssue>();

        if (string.IsNullOrWhiteSpace(workflow.Name))
            issues.Add(new WorkflowIssue(null, null, "The workflow must have a name."));

        if (workflow.SchemaVersion != Workflow.CurrentSchemaVersion)
            issues.Add(new WorkflowIssue(null, null,
                $"Unsupported schema version {workflow.SchemaVersion} (supported: {Workflow.CurrentSchemaVersion})."));

        if (workflow.Steps.Count == 0)
            issues.Add(new WorkflowIssue(null, null, "The workflow has no steps."));

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < workflow.Steps.Count; i++)
        {
            var step = workflow.Steps[i];

            if (string.IsNullOrWhiteSpace(step.Id))
                issues.Add(new WorkflowIssue(i, step.Id, "The step has no id."));
            else if (!seenIds.Add(step.Id))
                issues.Add(new WorkflowIssue(i, step.Id, $"Duplicate step id '{step.Id}'."));

            ValidateStep(step, i, issues);
        }

        return issues;
    }

    private static void ValidateStep(WorkflowStep step, int index, List<WorkflowIssue> issues)
    {
        void Add(string message) => issues.Add(new WorkflowIssue(index, step.Id, message));

        if (step.Action == WorkflowActionType.Delay)
        {
            if (step.DelayMs <= 0 || step.DelayMs > MaxDelayMs)
                Add($"DelayMs must be between 1 and {MaxDelayMs}.");
            return;
        }

        var selector = step.Selector;
        if (selector is null ||
            (selector.AutomationId is null && selector.Name is null &&
             selector.ClassName is null && selector.ControlType is null))
        {
            Add($"{step.Action} requires a selector with at least one of automationId, name, className or controlType.");
        }

        if (step.Action == WorkflowActionType.SetValue && step.Value is null)
            Add("SetValue requires a value.");

        if (step.TimeoutMs <= 0 || step.TimeoutMs > MaxTimeoutMs)
            Add($"TimeoutMs must be between 1 and {MaxTimeoutMs}.");
    }
}
