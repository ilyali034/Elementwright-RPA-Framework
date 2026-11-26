namespace Elementwright.Core.Workflows;

/// <summary>Kinds of steps a <see cref="Workflow"/> can contain.</summary>
public enum WorkflowActionType
{
    /// <summary>UI Automation Invoke pattern (buttons, menu items). Requires a selector.</summary>
    Invoke,

    /// <summary>UI Automation Value pattern (editable fields). Requires a selector and a value.</summary>
    SetValue,

    /// <summary>UI Automation Toggle pattern (check boxes). Requires a selector.</summary>
    Toggle,

    /// <summary>UI Automation SelectionItem pattern (list items, tabs). Requires a selector.</summary>
    Select,

    /// <summary>Waits until the element appears. Requires a selector.</summary>
    WaitForElement,

    /// <summary>Pauses the workflow for <see cref="WorkflowStep.DelayMs"/> milliseconds.</summary>
    Delay
}
