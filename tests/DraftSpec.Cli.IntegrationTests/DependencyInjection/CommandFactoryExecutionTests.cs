using DraftSpec.Cli.DependencyInjection;
using DraftSpec.Cli.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DraftSpec.Cli.IntegrationTests.DependencyInjection;

/// <summary>
/// Tests that verify CommandFactory executors can be invoked directly.
/// These tests execute the returned lambdas to ensure code coverage
/// of the executor logic including config loading and option conversion.
/// </summary>
[NotInParallel("CommandFactoryExecution")]
public class CommandFactoryExecutionTests : IntegrationTestBase
{
    private ServiceProvider _provider = null!;
    private ICommandFactory _factory = null!;

    [Before(Test)]
    public void SetUpFactory()
    {
        var services = new ServiceCollection();
        services.AddDraftSpec();
        _provider = services.BuildServiceProvider();
        _factory = _provider.GetRequiredService<ICommandFactory>();
    }

    [After(Test)]
    public void TearDownFactory()
    {
        _provider.Dispose();
    }

    #region List Command Executor

    [Test]
    public async Task ListExecutor_WithSpecs_ReturnsZero()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();
        var cliOptions = new CliOptions { Path = specDir };

        var executor = _factory.Create("list");
        var result = await executor!(cliOptions, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0)
            .Because("List command should succeed with valid specs");
    }

    [Test]
    public async Task ListExecutor_CaseInsensitive_Works()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();
        var cliOptions = new CliOptions { Path = specDir };

        var executor = _factory.Create("LIST");
        var result = await executor!(cliOptions, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0)
            .Because("Command names should be case-insensitive");
    }

    #endregion

    #region Validate Command Executor

    [Test]
    public async Task ValidateExecutor_WithValidSpecs_ReturnsZero()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();
        var cliOptions = new CliOptions { Path = specDir };

        var executor = _factory.Create("validate");
        var result = await executor!(cliOptions, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0)
            .Because("Validate command should succeed with valid specs");
    }

    [Test]
    public async Task ValidateExecutor_ConvertOptionsCorrectly()
    {
        var specDir = CreateFixture().WithPassingSpec().Build();
        var cliOptions = new CliOptions
        {
            Path = specDir,
            Quiet = true,
            Strict = false
        };

        var executor = _factory.Create("validate");
        var result = await executor!(cliOptions, CancellationToken.None);

        // If it executes without error, options were converted correctly
        await Assert.That(result).IsEqualTo(0)
            .Because("Options should be converted and passed to command");
    }

    #endregion

    #region Run Command Executor

    // Note: Run command executor tests that need spec files are covered by
    // CliExitCodeTests which run the CLI as a subprocess. This works around
    // the SpecFinder security check that requires paths within the working directory.

    [Test]
    public async Task RunExecutor_CanBeCreated()
    {
        var executor = _factory.Create("run");

        await Assert.That(executor).IsNotNull()
            .Because("Run executor should be creatable");
    }

    #endregion

    #region Legacy Command Executors

    [Test]
    public async Task InitExecutor_InEmptyDirectory_ReturnsZero()
    {
        var cliOptions = new CliOptions { Path = _tempDir };

        var executor = _factory.Create("init");
        var result = await executor!(cliOptions, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0)
            .Because("Init command should succeed in empty directory");
        await Assert.That(File.Exists(Path.Combine(_tempDir, "spec_helper.csx"))).IsTrue()
            .Because("Init should create spec_helper.csx");
    }

    [Test]
    public async Task NewExecutor_CreatesSpecFile()
    {
        var cliOptions = new CliOptions
        {
            Path = _tempDir,
            SpecName = "TestFeature"
        };

        var executor = _factory.Create("new");
        var result = await executor!(cliOptions, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0)
            .Because("New command should succeed");
        await Assert.That(File.Exists(Path.Combine(_tempDir, "TestFeature.spec.csx"))).IsTrue()
            .Because("New should create the spec file");
    }

    [Test]
    public async Task SchemaExecutor_ReturnsZero()
    {
        var cliOptions = new CliOptions();

        var executor = _factory.Create("schema");
        var result = await executor!(cliOptions, CancellationToken.None);

        await Assert.That(result).IsEqualTo(0)
            .Because("Schema command should succeed");
    }

    #endregion
}
