# Contributing to DraftSpec

Please read our [Code of Conduct](CODE_OF_CONDUCT.md) before contributing.

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
3. Open PR with conventional commit title (see below)
4. Add issue reference in body (e.g., `Closes #123`)
5. Squash merge via PR (merge commits disabled)

## PR Title Format (Conventional Commits)

Use conventional commit format for PR titles. This enables automatic labeling and categorized release notes.

```
<type>(<scope>): <description>
```

### Types and Labels

| Type | Label | Release Category | When to Use |
|------|-------|------------------|-------------|
| `feat` | `feature` | 🚀 Features | New functionality |
| `fix` | `fix` | 🐛 Bug Fixes | Bug fixes |
| `perf` | `performance` | ⚡ Performance | Performance improvements |
| `security` | `security` | 🔒 Security | Security fixes |
| `docs` | `documentation` | 📚 Documentation | Documentation only |
| `test` | `test` | 🧪 Tests | Test additions/changes |
| `refactor` | `refactor` | 🔧 Maintenance | Code refactoring |
| `chore` | `chore` | 🔧 Maintenance | Build, CI, dependencies |
| `ci` | `chore` | 🔧 Maintenance | CI/CD changes |
| `build` | `chore` | 🔧 Maintenance | Build system changes |

### Breaking Changes

Add `!` after the type for breaking changes:
```
feat!: remove deprecated API
fix(auth)!: change token format
```

### Examples

```
feat(cli): add watch mode for continuous testing
fix(runner): handle async disposal correctly
perf(cache): eliminate redundant file hashing
docs: update README with new examples
refactor(scripting): extract common cache base class
chore(deps): update TUnit to v1.8.0
```

### Scope (Optional)

The scope indicates the affected area:
- `cli` - Command-line tool
- `runner` - Spec runner
- `dsl` - DSL API
- `mtp` - Testing Platform adapter
- `mcp` - MCP server
- `scripting` - CSX compilation
- `formatters` - Output formatters
- `deps` - Dependencies

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
