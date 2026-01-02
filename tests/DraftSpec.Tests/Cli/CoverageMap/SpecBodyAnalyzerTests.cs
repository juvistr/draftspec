using DraftSpec.Cli.CoverageMap;
using Microsoft.CodeAnalysis.CSharp;

namespace DraftSpec.Tests.Cli.CoverageMap;

public class SpecBodyAnalyzerTests
{
    [Test]
    public async Task ExtractsMethodCalls()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("UserService", () =>
            {
                it("creates a user", () =>
                {
                    var service = new UserService();
                    service.CreateAsync("test@example.com");
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].MethodCalls).Count().IsEqualTo(1);
        await Assert.That(specs[0].MethodCalls[0].MethodName).IsEqualTo("CreateAsync");
    }

    [Test]
    public async Task ExtractsTypeInstantiations()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("TodoService", () =>
            {
                it("creates a todo", () =>
                {
                    var service = new TodoService();
                    var todo = new Todo();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences).Count().IsEqualTo(2);
        await Assert.That(specs[0].TypeReferences[0].TypeName).IsEqualTo("TodoService");
        await Assert.That(specs[0].TypeReferences[0].Kind).IsEqualTo(ReferenceKind.New);
        await Assert.That(specs[0].TypeReferences[1].TypeName).IsEqualTo("Todo");
    }

    [Test]
    public async Task ExtractsUsingDirectives()
    {
        // Arrange
        var source = """
            using MyApp.Services;
            using MyApp.Models;
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("does something", () => { });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].UsingNamespaces).Contains("MyApp.Services");
        await Assert.That(specs[0].UsingNamespaces).Contains("MyApp.Models");
    }

    [Test]
    public async Task HandlesNestedContexts()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Parent", () =>
            {
                describe("Child", () =>
                {
                    it("nested spec", () =>
                    {
                        new Service();
                    });
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].ContextPath).Count().IsEqualTo(2);
        await Assert.That(specs[0].ContextPath[0]).IsEqualTo("Parent");
        await Assert.That(specs[0].ContextPath[1]).IsEqualTo("Child");
    }

    [Test]
    public async Task IgnoresHookBodies()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                before(() =>
                {
                    new SetupService();
                });

                it("test spec", () =>
                {
                    new TestService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        // Should only have TestService, not SetupService
        await Assert.That(specs[0].TypeReferences).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences[0].TypeName).IsEqualTo("TestService");
    }

    [Test]
    public async Task HandlesFocusedSpecs()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                fit("focused spec", () =>
                {
                    new FocusedService();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].SpecDescription).IsEqualTo("focused spec");
    }

    [Test]
    public async Task SkipsPendingSpecs()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("pending spec");
                it("implemented spec", () =>
                {
                    new Service();
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        // Pending spec should be skipped (no body to analyze)
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].SpecDescription).IsEqualTo("implemented spec");
    }

    [Test]
    public async Task ExtractsTypeOfReferences()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("Test", () =>
            {
                it("checks type", () =>
                {
                    var type = typeof(UserService);
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source);

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences).Count().IsEqualTo(1);
        await Assert.That(specs[0].TypeReferences[0].TypeName).IsEqualTo("UserService");
        await Assert.That(specs[0].TypeReferences[0].Kind).IsEqualTo(ReferenceKind.TypeOf);
    }

    [Test]
    public async Task GeneratesCorrectSpecId()
    {
        // Arrange
        var source = """
            using static DraftSpec.Dsl;

            describe("UserService", () =>
            {
                describe("CreateAsync", () =>
                {
                    it("creates a user", () => { });
                });
            });
            """;

        // Act
        var specs = ParseSpecs(source, "/project", "/project/specs/user.spec.csx");

        // Assert
        await Assert.That(specs).Count().IsEqualTo(1);
        await Assert.That(specs[0].SpecId).IsEqualTo("specs/user.spec.csx:UserService/CreateAsync/creates a user");
    }

    private static List<SpecReference> ParseSpecs(
        string source,
        string projectPath = "/project",
        string sourceFile = "/project/test.spec.csx")
    {
        // Strip #load and #r directives for parsing
        var lines = source.Split('\n').Where(line =>
        {
            var trimmed = line.TrimStart();
            return !trimmed.StartsWith("#load", StringComparison.Ordinal) &&
                   !trimmed.StartsWith("#r", StringComparison.Ordinal);
        });
        var cleanSource = string.Join('\n', lines);

        var parseOptions = CSharpParseOptions.Default
            .WithKind(Microsoft.CodeAnalysis.SourceCodeKind.Script);
        var syntaxTree = CSharpSyntaxTree.ParseText(cleanSource, parseOptions);
        var root = syntaxTree.GetCompilationUnitRoot();

        var analyzer = new SpecBodyAnalyzer(sourceFile, projectPath);
        analyzer.Visit(root);

        return analyzer.SpecReferences.ToList();
    }
}
