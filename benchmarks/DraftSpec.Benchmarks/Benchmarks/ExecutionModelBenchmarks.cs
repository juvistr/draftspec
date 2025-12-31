using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using DraftSpec;

namespace DraftSpec.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing execution overhead of different spec execution models.
/// Measures cold-start scenarios for:
/// - dotnet-script (.csx files) - current DraftSpec approach
/// - dotnet run with file-based apps (.NET 10 feature)
/// - In-process execution (baseline for comparison)
/// 
/// These benchmarks measure end-to-end execution time including process startup,
/// which is relevant for MCP tool integration where specs are generated and run dynamically.
/// 
/// Note: File-based apps require .NET 10 Preview 4+ and use the #:package directive.
/// This is experimental and the syntax may change before .NET 10 GA.
/// </summary>
[MemoryDiagnoser]
public class ExecutionModelBenchmarks
{
    private string _tempDir = null!;
    private string _scriptSpecPath = null!;
    private string _fileBasedSpecPath = null!;
    private bool _dotnetScriptAvailable;
    private bool _dotnet10Available;

    // Minimal spec that just runs one trivial test
    private const string MinimalSpecContent = """
                                              #r "nuget: DraftSpec, *"
                                              using static DraftSpec.Dsl;

                                              describe("Minimal", () =>
                                              {
                                                  it("passes", () => expect(1 + 1).toBe(2));
                                              });

                                              run(json: true);
                                              """;

    // File-based app version using .NET 10 directives
    // See: https://devblogs.microsoft.com/dotnet/announcing-dotnet-10-preview-4/
    private const string FileBasedSpecContent = """
                                                #:package DraftSpec@*
                                                using static DraftSpec.Dsl;

                                                describe("Minimal", () =>
                                                {
                                                    it("passes", () => expect(1 + 1).toBe(2));
                                                });

                                                run(json: true);
                                                """;

    [GlobalSetup]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec-bench-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Create script-based spec (.csx for dotnet-script)
        _scriptSpecPath = Path.Combine(_tempDir, "script.spec.csx");
        File.WriteAllText(_scriptSpecPath, MinimalSpecContent);

        // Create file-based app spec (.cs for dotnet run)
        _fileBasedSpecPath = Path.Combine(_tempDir, "filebased.cs");
        File.WriteAllText(_fileBasedSpecPath, FileBasedSpecContent);

        // Check tool availability
        _dotnetScriptAvailable = CheckToolAvailable("dotnet", "script --version");
        _dotnet10Available = CheckDotnet10Available();

        Console.WriteLine($"dotnet-script available: {_dotnetScriptAvailable}");
        Console.WriteLine($".NET 10 SDK available: {_dotnet10Available}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    /// <summary>
    /// Baseline: Execute spec via dotnet-script (current DraftSpec approach).
    /// This measures the full cold-start overhead of the script host.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int DotnetScript()
    {
        if (!_dotnetScriptAvailable)
            return -1; // Skip if not available

        return RunProcess("dotnet", $"script \"{_scriptSpecPath}\"");
    }

    /// <summary>
    /// Execute spec via dotnet run with file-based app (.NET 10 feature).
    /// Uses #:package directive instead of #r for NuGet references.
    /// </summary>
    [Benchmark]
    public int DotnetRunFileBased()
    {
        if (!_dotnet10Available)
            return -1; // Skip if not available

        return RunProcess("dotnet", $"run \"{_fileBasedSpecPath}\"");
    }

    /// <summary>
    /// For comparison: direct in-process execution (no process overhead).
    /// This shows the theoretical minimum - useful to understand how much
    /// overhead is process startup vs actual spec execution.
    /// </summary>
    [Benchmark]
    public int InProcess()
    {
        var context = new SpecContext("Minimal");
        context.AddSpec(new SpecDefinition("passes", () =>
        {
            if (1 + 1 != 2) throw new InvalidOperationException("Failed");
        }));

        var runner = new SpecRunner();
        var results = runner.Run(context);

        return results.Count(r => r.Status == SpecStatus.Passed);
    }

    private static int RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            return -1;

        process.WaitForExit();
        return process.ExitCode;
    }

    private static bool CheckToolAvailable(string fileName, string arguments)
    {
        try
        {
            var exitCode = RunProcess(fileName, arguments);
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckDotnet10Available()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-sdks",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return false;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Check for 10.x SDK (preview or GA)
            return output.Contains("10.");
        }
        catch
        {
            return false;
        }
    }
}