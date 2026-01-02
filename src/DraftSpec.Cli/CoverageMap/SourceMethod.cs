namespace DraftSpec.Cli.CoverageMap;

/// <summary>
/// Represents a public method extracted from a C# source file.
/// </summary>
public sealed class SourceMethod
{
    /// <summary>
    /// Fully qualified name including namespace and class.
    /// Example: "TodoApi.Services.TodoService.CreateAsync"
    /// </summary>
    public required string FullyQualifiedName { get; init; }

    /// <summary>
    /// The containing class or struct name.
    /// Example: "TodoService"
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// The method name without parameters.
    /// Example: "CreateAsync"
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Method signature with parameter types.
    /// Example: "CreateAsync(string, Priority)"
    /// </summary>
    public required string Signature { get; init; }

    /// <summary>
    /// The namespace containing the class.
    /// Example: "TodoApi.Services"
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Absolute path to the source file.
    /// </summary>
    public required string SourceFile { get; init; }

    /// <summary>
    /// 1-based line number where the method is declared.
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    /// Whether the method has the async modifier.
    /// </summary>
    public bool IsAsync { get; init; }

    /// <summary>
    /// Whether this is an extension method (first parameter has 'this' modifier).
    /// </summary>
    public bool IsExtensionMethod { get; init; }

    /// <summary>
    /// Parameter type names for overload disambiguation.
    /// </summary>
    public IReadOnlyList<string> ParameterTypes { get; init; } = [];
}
