# Elementwright-RPA-Framework

Windows UI Automation and RPA primitives used by [Elementwright-RPA-Studio](https://github.com/ilyali034/Elementwright-RPA-Studio).

## Projects

| Project | Responsibility |
|---|---|
| `src/Elementwright.Core` | Window discovery, UI Automation tree inspection, structured selectors and element actions |

The framework contains no desktop UI. The WPF application is maintained in the separate Studio repository.

## Inspector capabilities

- Discover top-level windows belonging to running processes.
- Read the Windows UI Automation **Control View** tree.
- Capture element name, AutomationId, class, control type, enabled/offscreen/focusable state, bounds, help text, current Value and supported UIA patterns. The Value of password fields is never read (`ElementNode.IsPassword`).
- Generate a readable selector for each inspected element.
- Resolve structured selectors against a target window.
- Execute supported UI Automation actions: Invoke, SetValue, Toggle and Select.
- `UiaElementActions.InvokeAsync` does not hang when the click opens a modal dialog: it returns `false` after a timeout (default 2 s) instead of waiting for the dialog to close.
- Bound tree traversal by depth and node count to avoid unbounded reads.

## Selector example

```csharp
var selector = ElementSelector.FromNode(elementNode);
var finder = new UiaElementFinder();
var match = await finder.ResolveAsync(windowHandle, selector);

if (match is not null)
{
    new UiaElementActions().Invoke(match.Element);
}
```

`ResolveAsync` returns exactly one element. If several elements match the selector properties, they are narrowed down by the selector's `Path` (the element's position in the Control View tree). If that is not possible, `AmbiguousSelectorException` is thrown instead of picking the first match. `FindFirstAsync` keeps the old "first match" behaviour and ignores `Path`.

Selectors serialize to JSON, which is what Studio copies and what workflows should store:

```csharp
string json = selector.ToJson();
// {"automationId":"btnOk","controlType":"Button","path":"2/1"}

var parsed = ElementSelector.Parse(json); // or ElementSelector.TryParse(json, out var s)
```

Selectors prefer `AutomationId` when available and also constrain by control type. When no AutomationId is present, the selector falls back to Name, ClassName and ControlType. These properties are provider-dependent; dynamic applications may expose incomplete or changing UI Automation metadata.

## Workflows

A `Workflow` is an ordered list of steps stored as JSON. Studio's Workflow Designer edits it; the Runner executes it.

| Action | Needs | What it does |
|---|---|---|
| `invoke` | selector | UIA Invoke (buttons, menu items) |
| `setValue` | selector, value | UIA Value (editable fields) |
| `toggle` | selector | UIA Toggle (check boxes) |
| `select` | selector | UIA SelectionItem (list items, tabs) |
| `waitForElement` | selector, `timeoutMs` | waits until the element appears |
| `delay` | `delayMs` | pauses the workflow |

```json
{
  "schemaVersion": 1,
  "name": "Type into Notepad",
  "target": { "processName": "notepad" },
  "steps": [
    {
      "id": "3f1c0a9e",
      "name": "Type text",
      "action": "setValue",
      "selector": { "controlType": "Edit", "className": "RichEditD2DPT", "path": "1/0" },
      "value": "Hello from Elementwright",
      "timeoutMs": 10000
    },
    { "id": "a77b21d4", "action": "delay", "delayMs": 500 }
  ]
}
```

```csharp
var workflow = await WorkflowSerializer.LoadAsync("notepad.workflow.json");

var issues = WorkflowValidator.Validate(workflow);
if (issues.Count > 0)
    throw new InvalidOperationException(string.Join(Environment.NewLine, issues));
```

`WorkflowSerializer.Parse` checks the JSON structure (missing `action`, unknown action names). `WorkflowValidator.Validate` checks that the workflow can run (selectors present, `setValue` has a value, sane timeouts, unique step ids).

## Limitations

- Requires Windows and a desktop session; this is not a cross-platform UI automation library.
- Actions work only when the target exposes the corresponding UI Automation pattern.
- Some applications (including some Java, legacy, elevated, custom-rendered or remote-desktop apps) may expose incomplete UI Automation trees. Run Studio at an appropriate integrity level when inspecting elevated targets.
- The reader uses the Control View, so raw layout-only elements from the Raw View are intentionally omitted.
- `Path` is a positional tie-breaker and breaks when siblings are added or removed; ancestor-relative selectors are planned.

## Development

Clone the repositories side by side:

```text
projects/
├── Elementwright-RPA-Framework/
└── Elementwright-RPA-Studio/
```

Build the core library:

```powershell
dotnet build .\src\Elementwright.Core\Elementwright.Core.csproj
```

Package it for standalone Studio use:

```powershell
dotnet pack .\src\Elementwright.Core\Elementwright.Core.csproj -c Release
```
