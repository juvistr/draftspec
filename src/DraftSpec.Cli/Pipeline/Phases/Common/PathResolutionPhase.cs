namespace DraftSpec.Cli.Pipeline.Phases.Common;

/// <summary>
/// Resolves and validates the input path, setting <see cref="ContextKeys.ProjectPath"/>.
/// </summary>
/// <remarks>
/// <para><b>Requires:</b> <c>context.Path</c> is set</para>
/// <para><b>Produces:</b> <c>Items[ProjectPath]</c> - absolute path to project/spec directory</para>
/// <para><b>Short-circuits:</b> If path doesn't exist (returns 1)</para>
/// </remarks>
public class PathResolutionPhase : ICommandPhase
{
    /// <inheritdoc />
    public Task<int> ExecuteAsync(
        CommandContext context,
        Func<CommandContext, CancellationToken, Task<int>> pipeline,
        CancellationToken ct)
    {
        var path = context.Path;

        // Resolve to absolute path
        var absolutePath = Path.GetFullPath(path);

        // Validate path exists
        var isFile = context.FileSystem.FileExists(absolutePath);
        var isDirectory = context.FileSystem.DirectoryExists(absolutePath);

        if (!isFile && !isDirectory)
        {
            context.Console.WriteError($"Path not found: {absolutePath}");
            return Task.FromResult(1);
        }

        // For files, use the containing directory as project path
        var projectPath = isFile
            ? Path.GetDirectoryName(absolutePath)!
            : absolutePath;

        context.Set<string>(ContextKeys.ProjectPath, projectPath);

        return pipeline(context, ct);
    }
}
