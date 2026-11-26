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
- Capture element name, AutomationId, class, control type, enabled/offscreen/focusable state, bounds, help text, current Value and supported UIA patterns.
- Generate a readable selector for each inspected element.
- Resolve structured selectors against a target window.
- Execute supported UI Automation actions: Invoke, SetValue, Toggle and Select.
- Bound tree traversal by depth and node count to avoid unbounded reads.

## Selector example

```csharp
var selector = ElementSelector.FromNode(elementNode);
var finder = new UiaElementFinder();
var element = await finder.FindFirstAsync(windowHandle, selector);

if (element is not null)
{
    new UiaElementActions().Invoke(element);
}
```

Selectors prefer `AutomationId` when available and also constrain by control type. When no AutomationId is present, the selector falls back to Name, ClassName and ControlType. These properties are provider-dependent; dynamic applications may expose incomplete or changing UI Automation metadata.

## Limitations

- Requires Windows and a desktop session; this is not a cross-platform UI automation library.
- Actions work only when the target exposes the corresponding UI Automation pattern.
- Some applications (including some Java, legacy, elevated, custom-rendered or remote-desktop apps) may expose incomplete UI Automation trees. Run Studio at an appropriate integrity level when inspecting elevated targets.
- The reader uses the Control View, so raw layout-only elements from the Raw View are intentionally omitted.
- Selector resolution currently returns the first matching element; uniqueness scoring and ancestor-relative selectors are planned.

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
