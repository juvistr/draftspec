using DraftSpec.Cli;

namespace DraftSpec.Tests.Infrastructure.Mocks;

/// <summary>
/// Mock project builder for testing.
/// </summary>
class MockProjectBuilder : IProjectBuilder
{
    public event Action<string>? OnBuildStarted;
    public event Action<BuildResult>? OnBuildCompleted;
    public event Action<string>? OnBuildSkipped;

    public List<string> BuildProjectsCalls { get; } = [];
    public int ClearBuildCacheCalls { get; private set; }

    public void BuildProjects(string directory)
    {
        BuildProjectsCalls.Add(directory);
    }

    public string FindOutputDirectory(string specDirectory)
    {
        return Path.Combine(specDirectory, "bin", "Debug", "net10.0");
    }

    public void ClearBuildCache()
    {
        ClearBuildCacheCalls++;
    }

    public void TriggerBuildStarted(string project)
    {
        OnBuildStarted?.Invoke(project);
    }

    public void TriggerBuildCompleted(BuildResult result)
    {
        OnBuildCompleted?.Invoke(result);
    }

    public void TriggerBuildSkipped(string project)
    {
        OnBuildSkipped?.Invoke(project);
    }
}
