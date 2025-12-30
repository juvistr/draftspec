using DraftSpec.Formatters;

namespace DraftSpec.Cli.DependencyInjection;

/// <summary>
/// Registry for CLI formatters with factory-based lookup.
/// </summary>
/// <remarks>
/// This interface differs from <see cref="DraftSpec.Plugins.IFormatterRegistry"/> in that it uses
/// a factory pattern to support CLI-specific options (like CSS URL for HTML output).
/// The core library's IFormatterRegistry takes formatter instances directly and is used
/// by the scripting API and plugins. This CLI version needs access to <see cref="CliOptions"/>
/// to configure formatters at resolution time.
/// </remarks>
public interface ICliFormatterRegistry
{
    /// <summary>
    /// Gets a formatter by name, optionally configured with CLI options.
    /// </summary>
    /// <param name="name">The formatter name (e.g., "json", "html", "markdown").</param>
    /// <param name="options">Optional CLI options to configure the formatter.</param>
    /// <returns>The configured formatter, or null if not found.</returns>
    IFormatter? GetFormatter(string name, CliOptions? options = null);

    /// <summary>
    /// Registers a formatter factory with the given name.
    /// </summary>
    /// <param name="name">The formatter name.</param>
    /// <param name="factory">A factory function that creates the formatter with optional CLI options.</param>
    void Register(string name, Func<CliOptions?, IFormatter> factory);

    /// <summary>
    /// Gets all registered formatter names.
    /// </summary>
    IEnumerable<string> Names { get; }
}