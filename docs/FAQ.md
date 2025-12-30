# Frequently Asked Questions

## General

### What is DraftSpec?

DraftSpec is an RSpec-inspired BDD testing framework for .NET 10. It brings the `describe`/`it`/`expect` syntax to .NET, filling the gap left by abandoned frameworks like NSpec.

### Why another testing framework?

The .NET ecosystem lacks a maintained BDD framework with the ergonomics of RSpec. NSpec was abandoned years ago, and xUnit/NUnit, while excellent, use a different paradigm. DraftSpec provides:

- Nested `describe` blocks for organizing tests
- Natural language specs with `it`
- Jest-style `expect()` assertions
- Focus (`fit`) and skip (`xit`) for development workflow
- CSX scripting for zero-compile iteration

### Is DraftSpec production-ready?

DraftSpec is in **alpha** (v0.4.x). The core API is stable with 2000+ tests and 80%+ code coverage. Minor breaking changes may occur before v1.0. It's suitable for new projects willing to adapt to API evolution.

### What .NET versions are supported?

DraftSpec requires **.NET 10** or later. It uses C# 14 features and modern .NET APIs.

## Comparison with Other Frameworks

### DraftSpec vs NSpec

| Feature | DraftSpec | NSpec |
|---------|-----------|-------|
| Maintained | Yes (active) | No (abandoned ~2018) |
| .NET Version | .NET 10+ | .NET Framework/.NET Core |
| Syntax | `describe`/`it`/`expect` | `describe`/`it`/`should` |
| File Format | CSX scripts | C# classes |
| IDE Integration | MTP + Test Explorer | Custom runner |
| Focus/Skip | `fit`/`xit` | Similar |

DraftSpec is spiritually similar to NSpec but rebuilt for modern .NET with CSX scripting support.

### DraftSpec vs xUnit/NUnit/MSTest

| Feature | DraftSpec | xUnit/NUnit/MSTest |
|---------|-----------|-------------------|
| Style | BDD (describe/it) | AAA (Arrange/Act/Assert) |
| Organization | Nested contexts | Test classes |
| Assertions | `expect().toBe()` | `Assert.Equal()` |
| Spec Files | CSX scripts | C# classes |
| Compilation | JIT on run | Pre-compiled |

Choose DraftSpec if you prefer RSpec-style BDD. Choose xUnit/NUnit if you prefer traditional unit testing or need maximum IDE integration.

### DraftSpec vs SpecFlow/Reqnroll

| Feature | DraftSpec | SpecFlow/Reqnroll |
|---------|-----------|-------------------|
| Style | Code-first BDD | Gherkin feature files |
| Audience | Developers | Developers + BA/QA |
| Syntax | `describe`/`it` | Given/When/Then |
| Files | `.spec.csx` | `.feature` + step defs |

Choose DraftSpec for developer-focused BDD. Choose SpecFlow/Reqnroll for collaboration with non-developers using Gherkin.

## Usage Questions

### Can I use DraftSpec with existing test projects?

Yes. DraftSpec can coexist with xUnit/NUnit/MSTest in the same solution. Each framework runs its own tests.

### How do I run specs in CI/CD?

```bash
# CLI approach
draftspec run ./specs --format junit -o results.xml

# Or via dotnet test (with MTP package)
dotnet test --logger "trx;LogFileName=results.trx"
```

### Can I use dependency injection?

DraftSpec doesn't have built-in DI, but you can use any DI container in your `spec_helper.csx`:

```csharp
// spec_helper.csx
#r "nuget: Microsoft.Extensions.DependencyInjection, 10.0.0"

var services = new ServiceCollection();
services.AddSingleton<IMyService, MyService>();
var provider = services.BuildServiceProvider();

public T GetService<T>() => provider.GetRequiredService<T>();
```

### How do I mock dependencies?

Use any mocking library. Moq works well:

```csharp
#r "nuget: Moq, 4.20.0"
using Moq;

describe("UserService", () =>
{
    var mockRepo = new Mock<IUserRepository>();

    before(() =>
    {
        mockRepo.Reset();
        mockRepo.Setup(r => r.GetById(1)).Returns(new User { Name = "Test" });
    });

    it("gets user by id", () =>
    {
        var service = new UserService(mockRepo.Object);
        var user = service.GetUser(1);
        expect(user.Name).toBe("Test");
    });
});
```

### Why CSX files instead of regular C#?

CSX (C# Script) files provide:

1. **Zero compilation**: Run specs immediately without build step
2. **Fast iteration**: Change and rerun instantly
3. **Self-contained**: NuGet references inline with `#r`
4. **Simpler structure**: No namespaces, classes, or boilerplate

You can also use DraftSpec with regular C# via the MTP integration if you prefer compiled tests.

### How do I share setup across spec files?

Use `spec_helper.csx` and `#load`:

```csharp
// spec_helper.csx
#r "nuget: DraftSpec, *"
using static DraftSpec.Dsl;

public static HttpClient CreateTestClient() => new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
```

```csharp
// api.spec.csx
#load "spec_helper.csx"

describe("API", () =>
{
    var client = CreateTestClient();
    // ...
});
```

## Performance

### Is DraftSpec slower than xUnit/NUnit?

Slightly, due to CSX JIT compilation on first run. Subsequent runs are faster due to caching. For most test suites, the difference is negligible.

### How do I speed up spec execution?

1. Use `--parallel` for parallel file execution
2. Use `beforeAll`/`afterAll` for expensive shared setup
3. Enable caching (default) to skip recompilation

## Contributing

### How can I contribute?

See [CONTRIBUTING.md](../CONTRIBUTING.md). We welcome:

- Bug reports and feature requests
- Documentation improvements
- Code contributions (with tests)

### Where do I report bugs?

[GitHub Issues](https://github.com/juvistr/draftspec/issues/new?template=bug_report.md)

### Where can I ask questions?

[GitHub Discussions](https://github.com/juvistr/draftspec/discussions)
