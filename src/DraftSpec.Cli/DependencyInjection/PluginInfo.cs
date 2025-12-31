using System.Reflection;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Information about a discovered plugin.
/// </summary>
public record PluginInfo(
    string Name,
    Type Type,
    PluginKind Kind,
    Assembly Assembly);
