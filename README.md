# Elementwright-RPA-Framework 

A framework for optimizing and automating routine tasks, developing algorithms, and integrating automated systems into application workflows.


## Structure

| Project | Role |
|---|---|
| `src/Elementwright.Core` | Framework library: window discovery, UI Automation tree reader, selectors. No UI. |

The UI lives in a separate repository: **Elementwright-RPA-Studio**, which consumes this library.

## Use from Studio

Side by side clone (Studio picks it up automatically), or publish a package:

```
dotnet pack src/Elementwright.Core -c Release
```
