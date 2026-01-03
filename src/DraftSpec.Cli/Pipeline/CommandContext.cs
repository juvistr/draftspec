namespace DraftSpec.Cli.Pipeline;

/// <summary>
/// Context passed through the command pipeline, providing access to
/// input path, I/O abstractions, and phase-to-phase communication.
/// </summary>
/// <remarks>
/// Uses composition over inheritance: a single flat context with an Items
/// dictionary rather than a type hierarchy. This avoids fragile base class
/// problems and keeps all phases using the same non-generic interface.
/// </remarks>
public class CommandContext
{
    /// <summary>
    /// The input path provided to the command (file or directory).
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Console abstraction for output.
    /// </summary>
    public required IConsole Console { get; init; }

    /// <summary>
    /// File system abstraction for I/O operations.
    /// </summary>
    public required IFileSystem FileSystem { get; init; }

    /// <summary>
    /// Dictionary for phase-to-phase communication.
    /// Use <see cref="Get{T}"/> and <see cref="Set{T}"/> for type-safe access,
    /// or <see cref="ContextKeys"/> for well-known keys.
    /// </summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <summary>
    /// Get a typed value from the Items dictionary.
    /// </summary>
    /// <typeparam name="T">Expected type of the value.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value cast to T, or default if not found.</returns>
    public T? Get<T>(string key) => Items.TryGetValue(key, out var v) ? (T?)v : default;

    /// <summary>
    /// Set a typed value in the Items dictionary.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    /// <param name="key">The key to store under.</param>
    /// <param name="value">The value to store.</param>
    public void Set<T>(string key, T value) => Items[key] = value;
}
