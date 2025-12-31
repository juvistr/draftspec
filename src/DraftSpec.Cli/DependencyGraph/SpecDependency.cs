namespace DraftSpec.Cli.DependencyGraph;

/// <summary>
/// Represents a spec file and its dependencies.
/// </summary>
/// <param name="SpecFile">Absolute path to the .spec.csx file.</param>
/// <param name="LoadDependencies">Files referenced via #load directives.</param>
/// <param name="Namespaces">Namespaces referenced via using directives.</param>
public record SpecDependency(
    string SpecFile,
    IReadOnlyList<string> LoadDependencies,
    IReadOnlyList<string> Namespaces);
