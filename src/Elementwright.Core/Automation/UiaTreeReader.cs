using System.Windows.Automation;

namespace Elementwright.Core.Automation;

public sealed class UiaTreeReader
{
    public const int DefaultMaxDepth = 8;
    public const int DefaultMaxNodes = 2000;

    public Task<ElementNode> ReadAsync(
        nint windowHandle,
        int maxDepth = DefaultMaxDepth,
        int maxNodes = DefaultMaxNodes,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var root = AutomationElement.FromHandle(windowHandle);
            var budget = maxNodes;
            return Build(root, parentSelector: null, depth: 0, maxDepth, ref budget, cancellationToken);
        }, cancellationToken);
    }

    private static ElementNode Build(
        AutomationElement element,
        string? parentSelector,
        int depth,
        int maxDepth,
        ref int budget,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        budget--;

        var info = element.Current;
        var type = info.ControlType.ProgrammaticName.Replace("ControlType.", string.Empty);
        var rect = info.BoundingRectangle;
        var safeRect = rect.IsEmpty || double.IsInfinity(rect.X)
            ? (X: 0d, Y: 0d, W: 0d, H: 0d)
            : (X: rect.X, Y: rect.Y, W: rect.Width, H: rect.Height);

        var selector = BuildSelector(type, info.Name, info.AutomationId);
        var fullSelector = parentSelector is null ? selector : $"{parentSelector}/{selector}";

        var node = new ElementNode
        {
            Name = info.Name ?? string.Empty,
            AutomationId = info.AutomationId ?? string.Empty,
            ClassName = info.ClassName ?? string.Empty,
            ControlType = type,
            IsEnabled = info.IsEnabled,
            X = safeRect.X,
            Y = safeRect.Y,
            Width = safeRect.W,
            Height = safeRect.H,
            Selector = fullSelector,
        };

        if (depth >= maxDepth)
            return node;

        AutomationElement? child;
        try
        {
            child = TreeWalker.ControlViewWalker.GetFirstChild(element);
        }
        catch (ElementNotAvailableException)
        {
            return node;
        }

        while (child is not null && budget > 0)
        {
            try
            {
                node.Children.Add(Build(child, fullSelector, depth + 1, maxDepth, ref budget, ct));
                child = TreeWalker.ControlViewWalker.GetNextSibling(child);
            }
            catch (ElementNotAvailableException)
            {
                break; 
            }
        }

        return node;
    }

    private static string BuildSelector(string type, string? name, string? automationId)
    {
        if (!string.IsNullOrEmpty(automationId))
            return $"{type}[AutomationId='{Escape(automationId)}']";
        if (!string.IsNullOrEmpty(name))
            return $"{type}[Name='{Escape(name)}']";
        return type;
    }

    private static string Escape(string value) => value.Replace("'", "\\'");
}
