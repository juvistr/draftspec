namespace DraftSpec.Tests.Infrastructure.Mocks;

using DraftSpec.Cli;

/// <summary>
/// Mock project resolver for unit testing.
/// </summary>
public class MockProjectResolver : IProjectResolver
{
    /// <summary>
    /// Gets or sets the project path to return from FindProject.
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the project info to return from GetProjectInfo.
    /// </summary>
    public ProjectInfo? ProjectInfoResult { get; set; }

    public string? FindProject(string directory) => ProjectPath;

    public ProjectInfo? GetProjectInfo(string csprojPath) => ProjectInfoResult;
}
