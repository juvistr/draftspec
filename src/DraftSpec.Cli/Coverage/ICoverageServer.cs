namespace DraftSpec.Cli.Coverage;

/// <summary>
/// Manages the dotnet-coverage server lifecycle for efficient coverage collection.
/// </summary>
public interface ICoverageServer : IDisposable
{
    /// <summary>
    /// Session ID for the coverage server.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Path to the coverage output file.
    /// </summary>
    string CoverageFile { get; }

    /// <summary>
    /// Whether the server is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Start the coverage server.
    /// </summary>
    /// <returns>True if the server started successfully.</returns>
    bool Start();

    /// <summary>
    /// Shutdown the server and finalize the coverage file.
    /// </summary>
    /// <returns>True if shutdown succeeded and coverage file exists.</returns>
    bool Shutdown();
}
