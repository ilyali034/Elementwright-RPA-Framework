using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elementwright.Core.Workflows;

/// <summary>Reads and writes workflows as indented, camelCase JSON.</summary>
public static class WorkflowSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string ToJson(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        return JsonSerializer.Serialize(workflow, Options);
    }

    /// <summary>Parses workflow JSON. Does not check semantics; call <see cref="WorkflowValidator.Validate"/> afterwards.</summary>
    /// <exception cref="FormatException">The text is not a valid workflow document.</exception>
    public static Workflow Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        try
        {
            return JsonSerializer.Deserialize<Workflow>(json, Options)
                ?? throw new FormatException("The workflow document is empty.");
        }
        catch (JsonException ex)
        {
            throw new FormatException($"The workflow file is not valid: {ex.Message}", ex);
        }
    }

    public static async Task SaveAsync(Workflow workflow, string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await File.WriteAllTextAsync(path, ToJson(workflow), cancellationToken).ConfigureAwait(false);
    }

    /// <exception cref="FormatException">The file is not a valid workflow document.</exception>
    public static async Task<Workflow> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return Parse(json);
    }
}
