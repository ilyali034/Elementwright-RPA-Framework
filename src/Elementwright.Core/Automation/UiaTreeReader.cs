using System.Globalization;
using System.Windows;
using System.Windows.Automation;

namespace Elementwright.Core.Automation;

public sealed class UiaTreeReader
{
    public const int DefaultMaxDepth = 12;
    public const int DefaultMaxNodes = 5000;

    public Task<ElementNode> ReadAsync(
        nint windowHandle,
        int maxDepth = DefaultMaxDepth,
        int maxNodes = DefaultMaxNodes,
        CancellationToken cancellationToken = default)
    {
        if (windowHandle == nint.Zero)
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));
        if (maxDepth < 0)
            throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if (maxNodes < 1)
            throw new ArgumentOutOfRangeException(nameof(maxNodes));

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = AutomationElement.FromHandle(windowHandle)
                ?? throw new InvalidOperationException("Windows UI Automation could not access this window.");

            var budget = maxNodes;
            return Build(root, null, string.Empty, 0, maxDepth, ref budget, cancellationToken);
        }, cancellationToken);
    }

    private static ElementNode Build(
        AutomationElement element,
        string? parentSelector,
        string path,
        int depth,
        int maxDepth,
        ref int budget,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (budget <= 0)
            throw new InvalidOperationException("The UI tree node limit was reached.");
        budget--;

        var info = element.Current;
        var type = Safe(() => info.ControlType.ProgrammaticName.Replace("ControlType.", string.Empty), "Custom");
        var name = Safe(() => info.Name, string.Empty);
        var automationId = Safe(() => info.AutomationId, string.Empty);
        var className = Safe(() => info.ClassName, string.Empty);
        var rect = Safe(() => info.BoundingRectangle, Rect.Empty);
        var safeRect = rect.IsEmpty || double.IsInfinity(rect.X) || double.IsInfinity(rect.Y)
            ? (X: 0d, Y: 0d, W: 0d, H: 0d)
            : (X: rect.X, Y: rect.Y, W: rect.Width, H: rect.Height);

        var selector = BuildSelector(type, name, automationId, className);
        var fullSelector = parentSelector is null ? selector : $"{parentSelector}/{selector}";

        var node = new ElementNode
        {
            Name = name,
            AutomationId = automationId,
            ClassName = className,
            ControlType = type,
            IsEnabled = Safe(() => info.IsEnabled, false),
            IsOffscreen = Safe(() => info.IsOffscreen, false),
            IsKeyboardFocusable = Safe(() => info.IsKeyboardFocusable, false),
            HasInvokePattern = HasPattern(element, InvokePattern.Pattern),
            HasValuePattern = HasPattern(element, ValuePattern.Pattern),
            HasTogglePattern = HasPattern(element, TogglePattern.Pattern),
            HasSelectionItemPattern = HasPattern(element, SelectionItemPattern.Pattern),
            HelpText = Safe(() => info.HelpText, string.Empty),
            Value = ReadValue(element),
            X = safeRect.X,
            Y = safeRect.Y,
            Width = safeRect.W,
            Height = safeRect.H,
            Selector = fullSelector,
            Path = path
        };

        if (depth >= maxDepth || budget <= 0)
            return node;

        AutomationElement? child;
        try { child = TreeWalker.ControlViewWalker.GetFirstChild(element); }
        catch (ElementNotAvailableException) { return node; }

        var childIndex = 0;
        while (child is not null && budget > 0)
        {
            ct.ThrowIfCancellationRequested();
            AutomationElement? next;
            try
            {
                var childPath = path.Length == 0
                    ? childIndex.ToString(CultureInfo.InvariantCulture)
                    : $"{path}/{childIndex.ToString(CultureInfo.InvariantCulture)}";
                node.Children.Add(Build(child, fullSelector, childPath, depth + 1, maxDepth, ref budget, ct));
                childIndex++;
                next = TreeWalker.ControlViewWalker.GetNextSibling(child);
            }
            catch (ElementNotAvailableException)
            {
                break;
            }
            child = next;
        }

        return node;
    }

    private static string ReadValue(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern) &&
                pattern is ValuePattern valuePattern)
                return valuePattern.Current.Value ?? string.Empty;
        }
        catch (ElementNotAvailableException) { }
        catch (InvalidOperationException) { }
        return string.Empty;
    }

    private static bool HasPattern(AutomationElement element, AutomationPattern pattern)
    {
        try { return element.TryGetCurrentPattern(pattern, out _); }
        catch (ElementNotAvailableException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private static T Safe<T>(Func<T> read, T fallback)
    {
        try { return read(); }
        catch (ElementNotAvailableException) { return fallback; }
        catch (InvalidOperationException) { return fallback; }
        catch (System.Runtime.InteropServices.COMException) { return fallback; }
    }

    private static string BuildSelector(string type, string name, string automationId, string className)
    {
        if (!string.IsNullOrWhiteSpace(automationId))
            return $"{type}[AutomationId='{Escape(automationId)}']";
        if (!string.IsNullOrWhiteSpace(name))
            return $"{type}[Name='{Escape(name)}']";
        if (!string.IsNullOrWhiteSpace(className))
            return $"{type}[ClassName='{Escape(className)}']";
        return type;
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'");
}
