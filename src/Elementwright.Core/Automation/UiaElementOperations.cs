using System.Globalization;
using System.Windows.Automation;

namespace Elementwright.Core.Automation;

/// <summary>An element found by a selector together with its position in the Control View tree.</summary>
public sealed record UiaMatch(AutomationElement Element, string Path);

/// <summary>Thrown when a selector matches several elements and cannot be narrowed down safely.</summary>
public sealed class AmbiguousSelectorException : InvalidOperationException
{
    public AmbiguousSelectorException(int matchCount)
        : base($"The selector matches {matchCount} elements and cannot be resolved to a single one. Refresh the tree and re-capture the selector.")
    {
        MatchCount = matchCount;
    }

    public int MatchCount { get; }
}

public sealed class UiaElementFinder
{
    private const int MaxDepth = 32;
    private const int MaxVisited = 10000;
    private const int MaxChildren = 2000;

    /// <summary>Returns the first element (pre-order) that matches the selector properties. Ignores <see cref="ElementSelector.Path"/>.</summary>
    public Task<AutomationElement?> FindFirstAsync(
        nint windowHandle,
        ElementSelector selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return Task.Run(() => FindFirst(windowHandle, selector, cancellationToken), cancellationToken);
    }

    public AutomationElement? FindFirst(
        nint windowHandle,
        ElementSelector selector,
        CancellationToken cancellationToken = default)
    {
        var matches = Search(windowHandle, selector, stopAfterFirst: true, cancellationToken);
        return matches.Count == 0 ? null : matches[0].Element;
    }

    /// <summary>
    /// Resolves the selector to exactly one element. Several matches are narrowed down by
    /// <see cref="ElementSelector.Path"/>; if that is not possible an <see cref="AmbiguousSelectorException"/> is thrown
    /// instead of silently picking the first match. Returns <c>null</c> when nothing matches.
    /// </summary>
    public Task<UiaMatch?> ResolveAsync(
        nint windowHandle,
        ElementSelector selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return Task.Run(() => Resolve(windowHandle, selector, cancellationToken), cancellationToken);
    }

    public UiaMatch? Resolve(
        nint windowHandle,
        ElementSelector selector,
        CancellationToken cancellationToken = default)
    {
        var matches = Search(windowHandle, selector, stopAfterFirst: false, cancellationToken);
        if (matches.Count == 0)
            return null;
        if (matches.Count == 1)
            return matches[0];

        if (selector.Path is not null)
        {
            var exact = matches.FirstOrDefault(m => string.Equals(m.Path, selector.Path, StringComparison.Ordinal));
            if (exact is not null)
                return exact;
        }

        throw new AmbiguousSelectorException(matches.Count);
    }

    private static List<UiaMatch> Search(
        nint windowHandle,
        ElementSelector selector,
        bool stopAfterFirst,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);
        if (windowHandle == nint.Zero)
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));
        if (selector.AutomationId is null && selector.Name is null &&
            selector.ClassName is null && selector.ControlType is null)
            throw new ArgumentException("At least one selector property must be specified.", nameof(selector));

        var matches = new List<UiaMatch>();
        var root = AutomationElement.FromHandle(windowHandle);
        if (root is null)
            return matches;

        var walker = TreeWalker.ControlViewWalker;
        var stack = new Stack<(AutomationElement Element, int Depth, string Path)>();
        stack.Push((root, 0, string.Empty));
        var visited = 0;

        while (stack.Count > 0 && visited < MaxVisited)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (element, depth, path) = stack.Pop();
            visited++;

            try
            {
                if (Matches(element, selector))
                {
                    matches.Add(new UiaMatch(element, path));
                    if (stopAfterFirst)
                        return matches;
                }

                if (depth >= MaxDepth)
                    continue;

                var children = new List<AutomationElement>();
                var child = walker.GetFirstChild(element);
                while (child is not null && children.Count < MaxChildren)
                {
                    children.Add(child);
                    child = walker.GetNextSibling(child);
                }

                for (var i = children.Count - 1; i >= 0; i--)
                {
                    var index = i.ToString(CultureInfo.InvariantCulture);
                    stack.Push((children[i], depth + 1, path.Length == 0 ? index : $"{path}/{index}"));
                }
            }
            catch (ElementNotAvailableException) { }
            catch (InvalidOperationException) { }
        }

        return matches;
    }

    private static bool Matches(AutomationElement element, ElementSelector selector)
    {
        var current = element.Current;
        if (selector.AutomationId is not null &&
            !string.Equals(current.AutomationId, selector.AutomationId, StringComparison.Ordinal))
            return false;
        if (selector.Name is not null &&
            !string.Equals(current.Name, selector.Name, StringComparison.Ordinal))
            return false;
        if (selector.ClassName is not null &&
            !string.Equals(current.ClassName, selector.ClassName, StringComparison.Ordinal))
            return false;
        if (selector.ControlType is not null &&
            !string.Equals(current.ControlType.ProgrammaticName.Replace("ControlType.", string.Empty),
                selector.ControlType, StringComparison.Ordinal))
            return false;
        return true;
    }
}

public sealed class UiaElementActions
{
    public void Invoke(AutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Current.IsEnabled)
            throw new InvalidOperationException("The selected element is disabled.");

        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern) &&
            pattern is InvokePattern invoke)
        {
            invoke.Invoke();
            return;
        }

        throw new NotSupportedException("This element does not expose the UI Automation Invoke pattern.");
    }

    /// <summary>
    /// Invokes the element without blocking on modal dialogs. UI Automation's <c>Invoke</c> may not return until a
    /// dialog opened by the click is closed, so the call runs on its own thread and is awaited for at most
    /// <paramref name="timeout"/> (default 2 seconds).
    /// </summary>
    /// <returns><c>true</c> if Invoke completed; <c>false</c> if it was still running when the timeout elapsed
    /// (the target is most likely showing a modal dialog).</returns>
    public async Task<bool> InvokeAsync(
        AutomationElement element,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Current.IsEnabled)
            throw new InvalidOperationException("The selected element is disabled.");

        if (!element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern) ||
            pattern is not InvokePattern invoke)
            throw new NotSupportedException("This element does not expose the UI Automation Invoke pattern.");

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new Thread(() =>
        {
            try
            {
                invoke.Invoke();
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "Elementwright.UiaInvoke"
        };
        worker.Start();

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(2);
        var finished = await Task.WhenAny(completion.Task, Task.Delay(effectiveTimeout, cancellationToken))
            .ConfigureAwait(false);

        if (finished == completion.Task)
        {
            await completion.Task.ConfigureAwait(false); // rethrow Invoke failures
            return true;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return false;
    }

    public void SetValue(AutomationElement element, string value)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(value);
        if (!element.Current.IsEnabled)
            throw new InvalidOperationException("The selected element is disabled.");

        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern) &&
            pattern is ValuePattern valuePattern)
        {
            if (valuePattern.Current.IsReadOnly)
                throw new InvalidOperationException("The selected element is read-only.");
            valuePattern.SetValue(value);
            return;
        }

        throw new NotSupportedException("This element does not expose the UI Automation Value pattern.");
    }

    public void Toggle(AutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Current.IsEnabled)
            throw new InvalidOperationException("The selected element is disabled.");
        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var pattern) &&
            pattern is TogglePattern toggle)
        {
            toggle.Toggle();
            return;
        }
        throw new NotSupportedException("This element does not expose the UI Automation Toggle pattern.");
    }

    public void Select(AutomationElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!element.Current.IsEnabled)
            throw new InvalidOperationException("The selected element is disabled.");
        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern) &&
            pattern is SelectionItemPattern selection)
        {
            selection.Select();
            return;
        }
        throw new NotSupportedException("This element does not expose the UI Automation SelectionItem pattern.");
    }
}
