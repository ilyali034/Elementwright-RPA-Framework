using System.Windows.Automation;

namespace Elementwright.Core.Automation;

public sealed class UiaElementFinder
{
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
        ArgumentNullException.ThrowIfNull(selector);
        if (windowHandle == nint.Zero)
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));
        if (selector.AutomationId is null && selector.Name is null &&
            selector.ClassName is null && selector.ControlType is null)
            throw new ArgumentException("At least one selector property must be specified.", nameof(selector));

        var root = AutomationElement.FromHandle(windowHandle);
        if (root is null)
            return null;

        var walker = TreeWalker.ControlViewWalker;
        var stack = new Stack<(AutomationElement Element, int Depth)>();
        stack.Push((root, 0));
        var visited = 0;

        while (stack.Count > 0 && visited < 10000)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (element, depth) = stack.Pop();
            visited++;

            try
            {
                if (Matches(element, selector))
                    return element;

                if (depth >= 32)
                    continue;

                var children = new List<AutomationElement>();
                var child = walker.GetFirstChild(element);
                while (child is not null && children.Count < 2000)
                {
                    children.Add(child);
                    child = walker.GetNextSibling(child);
                }

                for (var i = children.Count - 1; i >= 0; i--)
                    stack.Push((children[i], depth + 1));
            }
            catch (ElementNotAvailableException) { }
            catch (InvalidOperationException) { }
        }

        return null;
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
}
