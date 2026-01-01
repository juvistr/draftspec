namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Register a lazy fixture that is memoized per spec.
    /// The factory is invoked on first access via get&lt;T&gt;() and cached.
    /// </summary>
    /// <typeparam name="T">The type of the fixture</typeparam>
    /// <param name="name">Unique name for this fixture within the context</param>
    /// <param name="factory">Factory function that creates the value</param>
    /// <example>
    /// <code>
    /// describe("UserService", () => {
    ///     let("db", () => new InMemoryDatabase());
    ///     let("service", () => new UserService(get&lt;IDatabase&gt;("db")));
    ///
    ///     it("creates users", () => {
    ///         var service = get&lt;UserService&gt;("service");
    ///         expect(service).toNotBeNull();
    ///     });
    /// });
    /// </code>
    /// </example>
    public static void let<T>(string name, Func<T> factory)
    {
        CurrentContext?.AddLetDefinition(name, () => factory()!);
    }

    /// <summary>
    /// Get the value of a lazy fixture by name.
    /// Creates the value on first access and caches it for the spec duration.
    /// </summary>
    /// <typeparam name="T">The expected type of the fixture</typeparam>
    /// <param name="name">Name of the fixture registered with let()</param>
    /// <returns>The fixture value</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called outside spec execution or when no definition exists
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// Thrown when the value cannot be cast to the requested type
    /// </exception>
    /// <example>
    /// <code>
    /// it("uses fixtures", () => {
    ///     var db = get&lt;IDatabase&gt;("db");
    ///     var service = get&lt;UserService&gt;("service");
    ///     // Both values are memoized - calling again returns same instance
    /// });
    /// </code>
    /// </example>
    public static T get<T>(string name)
    {
        var scope = LetScope.Current
            ?? throw new InvalidOperationException(
                "get<T>() can only be called during spec execution. " +
                "Ensure you are calling it from within an it() block.");

        // Check if already memoized
        if (scope.Values.TryGetValue(name, out var cached))
            return (T)cached;

        // Find the factory in this context or ancestors
        var factory = scope.Context.GetLetFactory(name)
            ?? throw new InvalidOperationException(
                $"No let definition found for '{name}'. " +
                $"Ensure let(\"{name}\", ...) is called in this context or a parent.");

        // Create and memoize the value
        var value = factory();
        scope.Values[name] = value;
        return (T)value;
    }
}
