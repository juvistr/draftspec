using DraftSpec.Cli;
using DraftSpec.Cli.Commands;
using DraftSpec.Cli.Options;
using DraftSpec.Cli.Options.Enums;
using DraftSpec.Cli.Pipeline;
using DraftSpec.Cli.Pipeline.Phases.Common;
using DraftSpec.Cli.Pipeline.Phases.CoverageMap;
using DraftSpec.Cli.Services;
using DraftSpec.Tests.Infrastructure.Mocks;

namespace DraftSpec.Tests.Cli.Commands;

/// <summary>
/// Tests for CoverageMapCommand.
/// These tests use the real file system for source and spec discovery.
/// </summary>
[NotInParallel]
public class CoverageMapCommandTests
{
    private string _tempDir = null!;
    private MockConsole _console = null!;
    private RealFileSystem _fileSystem = null!;

    [Before(Test)]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"draftspec_coverage_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        // Create a fake project file to stop FindProjectRoot from walking up to protected directories
        File.WriteAllText(Path.Combine(_tempDir, "Test.csproj"), "<Project/>");
        _console = new MockConsole();
        _fileSystem = new RealFileSystem();
    }

    [After(Test)]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private CoverageMapCommand CreateCommand()
    {
        // Build the pipeline with real phases
        var specFinder = new SpecFinder(_fileSystem);
        var coverageMapService = new CoverageMapService();

        var pipeline = new CommandPipelineBuilder()
            .Use(new PathResolutionPhase())
            .Use(new SourceDiscoveryPhase(_fileSystem))
            .Use(new CoverageMapPhase(coverageMapService, specFinder))
            .Use(new CoverageMapOutputPhase())
            .Build();

        return new CoverageMapCommand(pipeline, _console, _fileSystem);
    }

    #region Path Validation

    [Test]
    public async Task ExecuteAsync_NonexistentPath_ReturnsError()
    {
        // The command's FindProjectRoot is called before checking path existence.
        // Create the parent directory but not the child to properly test.
        var parentDir = Path.Combine(_tempDir, "parent");
        Directory.CreateDirectory(parentDir);
        File.WriteAllText(Path.Combine(parentDir, "Parent.csproj"), "<Project/>");

        var command = CreateCommand();
        var nonexistentPath = Path.Combine(parentDir, "nonexistent");
        var options = new CoverageMapOptions { SourcePath = nonexistentPath };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("Path not found");
    }

    [Test]
    public async Task ExecuteAsync_NoCsFiles_ReturnsError()
    {
        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("No C# source files found");
    }

    [Test]
    public async Task ExecuteAsync_NonCsFile_ReturnsError()
    {
        // Create a file that's not a .cs file
        var txtFile = Path.Combine(_tempDir, "readme.txt");
        File.WriteAllText(txtFile, "This is not a C# file");

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = txtFile };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("No C# source files found");
    }

    [Test]
    public async Task ExecuteAsync_NonSpecFile_ReturnsError()
    {
        // Create source file but point spec path to a non-.spec.csx file
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        var txtFile = Path.Combine(_tempDir, "readme.txt");
        File.WriteAllText(txtFile, "This is not a spec file");

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            SpecPath = txtFile
        };

        var result = await command.ExecuteAsync(options);

        // Returns error because file is not a spec file
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("must end with .spec.csx");
    }

    [Test]
    public async Task ExecuteAsync_EmptySpecDirectory_ReturnsError()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        // Create empty spec directory
        var specDir = Path.Combine(_tempDir, "specs");
        Directory.CreateDirectory(specDir);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            SpecPath = specDir
        };

        var result = await command.ExecuteAsync(options);

        // Returns error because no spec files found
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("No spec files found");
    }

    #endregion

    #region Source File Discovery

    [Test]
    public async Task ExecuteAsync_SingleSourceFile_ParsesMethods()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("test.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("creates user", () =>
                {
                    var service = new UserService();
                    service.CreateUser();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("CreateUser");
    }

    [Test]
    public async Task ExecuteAsync_NoPublicMethods_ReturnsZeroWithMessage()
    {
        CreateSourceFile("Internal.cs", """
            namespace MyApp;

            internal class InternalService
            {
                private void PrivateMethod() { }
            }
            """);
        CreateSpecFile("test.spec.csx", """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("test", () => { });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No public methods found");
    }

    [Test]
    public async Task ExecuteAsync_SkipsGeneratedFiles()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class RealService
            {
                public void RealMethod() { }
            }
            """);
        CreateSourceFile("Service.g.cs", """
            namespace MyApp;

            public class GeneratedService
            {
                public void GeneratedMethod() { }
            }
            """);
        CreateSpecFile("test.spec.csx", """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("test", () => { });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("RealMethod");
        await Assert.That(_console.Output).DoesNotContain("GeneratedMethod");
    }

    #endregion

    #region Spec File Discovery

    [Test]
    public async Task ExecuteAsync_NoSpecFiles_ReturnsError()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Errors).Contains("No spec files found");
    }

    [Test]
    public async Task ExecuteAsync_ExplicitSpecPath_UsesSpecPath()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);

        var specsDir = Path.Combine(_tempDir, "specs");
        Directory.CreateDirectory(specsDir);
        CreateSpecFile("specs/user.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("creates user", () =>
                {
                    new UserService().CreateUser();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            SpecPath = specsDir
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("CreateUser");
    }

    #endregion

    #region Namespace Filter

    [Test]
    public async Task ExecuteAsync_NamespaceFilter_FiltersMethods()
    {
        CreateSourceFile("Services.cs", """
            namespace MyApp.Services
            {
                public class UserService
                {
                    public void CreateUser() { }
                }
            }

            namespace MyApp.Controllers
            {
                public class UserController
                {
                    public void Get() { }
                }
            }
            """);
        CreateSpecFile("test.spec.csx", """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("test", () => { });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            NamespaceFilter = "MyApp.Services"
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("CreateUser");
        await Assert.That(_console.Output).DoesNotContain("UserController");
    }

    [Test]
    public async Task ExecuteAsync_NamespaceFilter_NoMatch_ReturnsZeroWithMessage()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp.Services;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("test.spec.csx", """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("test", () => { });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            NamespaceFilter = "Other.Namespace"
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No methods found matching namespace filter");
    }

    [Test]
    public async Task ExecuteAsync_NamespaceFilter_MultipleFilters()
    {
        CreateSourceFile("Services.cs", """
            namespace MyApp.Services
            {
                public class UserService { public void CreateUser() { } }
            }
            namespace MyApp.Controllers
            {
                public class UserController { public void Get() { } }
            }
            namespace MyApp.Models
            {
                public class User { public void Validate() { } }
            }
            """);
        CreateSpecFile("test.spec.csx", """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("test", () => { });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            NamespaceFilter = "MyApp.Services, MyApp.Controllers"
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("CreateUser");
        await Assert.That(_console.Output).Contains("Get");
        await Assert.That(_console.Output).DoesNotContain("Validate");
    }

    #endregion

    #region Coverage Confidence

    [Test]
    public async Task ExecuteAsync_DirectMethodCall_ShowsHighConfidence()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("user.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("creates user", () =>
                {
                    var service = new UserService();
                    service.CreateUser();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("[HIGH]");
    }

    [Test]
    public async Task ExecuteAsync_TypeInstantiation_ShowsMediumConfidence()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("user.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("has service", () =>
                {
                    var service = new UserService();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("[MEDIUM]");
    }

    [Test]
    public async Task ExecuteAsync_NamespaceMatch_ShowsLowConfidence()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp.Services;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("user.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp.Services;

            describe("UserService", () =>
            {
                it("does something", () =>
                {
                    var x = 1;
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("[LOW]");
    }

    [Test]
    public async Task ExecuteAsync_NoMatch_ShowsNoneConfidence()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("unrelated.spec.csx", """
            using static DraftSpec.Dsl;

            describe("OtherService", () =>
            {
                it("does other things", () =>
                {
                    var x = 42;
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("[NONE]");
    }

    #endregion

    #region GapsOnly Mode

    [Test]
    public async Task ExecuteAsync_GapsOnly_ShowsOnlyUncovered()
    {
        // Use completely different namespaces so namespace match doesn't give both LOW confidence
        CreateSourceFile("CoveredService.cs", """
            namespace Covered.Namespace;

            public class CoveredService
            {
                public void DoWork() { }
            }
            """);
        CreateSourceFile("UncoveredService.cs", """
            namespace Uncovered.Other.Namespace;

            public class UncoveredService
            {
                public void DoSomething() { }
            }
            """);
        CreateSpecFile("covered.spec.csx", """
            using static DraftSpec.Dsl;
            using Covered.Namespace;

            describe("CoveredService", () =>
            {
                it("does work", () =>
                {
                    new CoveredService().DoWork();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            GapsOnly = true
        };

        var result = await command.ExecuteAsync(options);

        // Returns 1 because uncovered methods exist in UncoveredService
        await Assert.That(result).IsEqualTo(1);
        await Assert.That(_console.Output).Contains("UncoveredService");
        await Assert.That(_console.Output).Contains("DoSomething");
        await Assert.That(_console.Output).DoesNotContain("CoveredService");
    }

    [Test]
    public async Task ExecuteAsync_GapsOnly_NoGaps_ReturnsZero()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("user.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("creates user", () =>
                {
                    new UserService().CreateUser();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            GapsOnly = true
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("No uncovered methods found");
    }

    #endregion

    #region JSON Format

    [Test]
    public async Task ExecuteAsync_JsonFormat_ProducesValidJson()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("user.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("creates user", () =>
                {
                    new UserService().CreateUser();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            Format = CoverageMapFormat.Json
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        // JSON output should contain JSON structure
        await Assert.That(_console.Output).Contains("\"summary\"");
        await Assert.That(_console.Output).Contains("\"methods\"");
        await Assert.That(_console.Output).Contains("\"totalMethods\"");
    }

    #endregion

    #region Summary Display

    [Test]
    public async Task ExecuteAsync_ShowsCoveragePercentage()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CoveredMethod() { }
                public void UncoveredMethod() { }
            }
            """);
        CreateSpecFile("user.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("calls covered method", () =>
                {
                    new UserService().CoveredMethod();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = _tempDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("Coverage Map:");
        await Assert.That(_console.Output).Contains("%");
    }

    #endregion

    #region Single File Paths

    [Test]
    public async Task ExecuteAsync_SingleSourceFile_ParsesFile()
    {
        var sourceFile = CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        CreateSpecFile("test.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("creates user", () =>
                {
                    new UserService().CreateUser();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = sourceFile };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("CreateUser");
    }

    [Test]
    public async Task ExecuteAsync_SingleSpecFile_ParsesFile()
    {
        CreateSourceFile("Service.cs", """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        var specFile = CreateSpecFile("test.spec.csx", """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("creates user", () =>
                {
                    new UserService().CreateUser();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions
        {
            SourcePath = _tempDir,
            SpecPath = specFile
        };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("CreateUser");
    }

    #endregion

    #region Project Root Discovery

    [Test]
    public async Task ExecuteAsync_NoProjectFile_UsesSourceDirectory()
    {
        // Create a subdirectory without any .csproj or .sln
        var subDir = Path.Combine(_tempDir, "nested", "deep");
        Directory.CreateDirectory(subDir);

        // Remove the project file we created in SetUp to test the fallback
        File.Delete(Path.Combine(_tempDir, "Test.csproj"));

        var sourceFile = Path.Combine(subDir, "Service.cs");
        File.WriteAllText(sourceFile, """
            namespace MyApp;

            public class UserService
            {
                public void CreateUser() { }
            }
            """);
        var specFile = Path.Combine(subDir, "test.spec.csx");
        File.WriteAllText(specFile, """
            using static DraftSpec.Dsl;
            using MyApp;

            describe("UserService", () =>
            {
                it("creates user", () =>
                {
                    new UserService().CreateUser();
                });
            });
            """);

        var command = CreateCommand();
        var options = new CoverageMapOptions { SourcePath = subDir };

        var result = await command.ExecuteAsync(options);

        await Assert.That(result).IsEqualTo(0);
        await Assert.That(_console.Output).Contains("CreateUser");
    }

    #endregion

    #region Helper Methods

    private string CreateSourceFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    private string CreateSpecFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    #endregion
}
