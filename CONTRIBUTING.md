# Contributing to DraftSpec

## Development Setup

```bash
# Clone and build
git clone https://github.com/juvistr/draftspec.git
cd draftspec
dotnet build

# Run tests
dotnet run --project tests/DraftSpec.Tests

# Run CLI integration tests
dotnet run --project tests/DraftSpec.Cli.IntegrationTests

# Run example specs
dotnet run --project examples/TodoApi.Specs
```

## Branch Naming

Use semantic prefixes:
- `feat/<description>` - New features
- `fix/<description>` - Bug fixes
- `test/<description>` - Test additions
- `docs/<description>` - Documentation
- `refactor/<description>` - Code refactoring

## Pull Request Workflow

1. Create a feature branch from `main`
2. Make changes and add tests
3. Open PR with issue reference in body (e.g., `Closes #123`)
4. Squash merge via PR (merge commits disabled)

## Release Process

Releases are triggered manually via GitHub Releases, which then triggers the NuGet publish workflow.

### Creating a Release

1. **Ensure CI is passing** on `main`

2. **Go to GitHub Releases**: https://github.com/juvistr/draftspec/releases

3. **Click "Draft a new release"**

4. **Create a new tag** in the format `v{major}.{minor}.{patch}[-prerelease]`:
   - Stable: `v0.5.0`
   - Alpha: `v0.5.0-alpha.1`
   - Beta: `v0.5.0-beta.1`

5. **Set the target** to `main`

6. **Write release notes** summarizing changes since the last release

7. **Check "Set as a pre-release"** for alpha/beta releases

8. **Click "Publish release"**

### What Happens Next

The `publish.yml` workflow automatically:
1. Builds the solution with the release version
2. Runs all tests
3. Packs NuGet packages
4. Publishes to NuGet.org (requires `nuget` environment approval)
5. Attaches `.nupkg` files to the GitHub release

### Version Format

DraftSpec uses [MinVer](https://github.com/adamralph/minver) for versioning:
- Version is derived from git tags
- Pre-release versions: `v0.5.0-alpha.1` → NuGet version `0.5.0-alpha.1`
- Stable versions: `v0.5.0` → NuGet version `0.5.0`

### Published Packages

Each release publishes:
- `DraftSpec` - Core library with DSL and assertions
- `DraftSpec.Cli` - Command-line tool (`draftspec` global tool)
- `DraftSpec.TestingPlatform` - Microsoft.Testing.Platform adapter
- `DraftSpec.Formatters.*` - Output formatters (Console, Html, Json, Markdown)
- `DraftSpec.Mcp` - MCP server for AI-assisted testing

## Code Style

- Follow existing patterns in the codebase
- Use `dotnet format` for formatting
- Prefer explicit types over `var` for public APIs
- Add XML documentation for public members
