# CLI Reference

Complete reference for the DraftSpec command-line interface.

## Installation

```bash
dotnet tool install -g DraftSpec.Cli --prerelease
```

After installation, the `draftspec` command is available globally.

---

## Commands

### draftspec run

Run spec files and output results.

```bash
draftspec run <path> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<path>` | File or directory to run. Defaults to current directory (`.`) |

**Options:**

| Option | Description |
|--------|-------------|
| `--format, -f <format>` | Output format: `console` (default), `json`, `markdown`, `html` |
| `-o, --output <file>` | Write output to file instead of stdout |
| `--css-url <url>` | Custom CSS URL for HTML format |
| `--parallel, -p` | Run spec files in parallel |
| `--tags, -t <tags>` | Only run specs with these tags (comma-separated) |
| `--exclude-tags <tags>` | Exclude specs with these tags (comma-separated) |
| `--bail, -b` | Stop on first spec failure |
| `--no-cache` | Disable dotnet-script caching |

**Examples:**

```bash
# Run all specs in current directory
draftspec run .

# Run specific file
draftspec run Calculator.spec.csx

# Run specs in a folder
draftspec run ./specs

# JSON output to stdout
draftspec run . --format json

# HTML report to file
draftspec run . --format html -o report.html

# Markdown report
draftspec run . --format markdown -o results.md

# Custom CSS for HTML
draftspec run . --format html --css-url ./custom.css -o report.html

# Parallel execution
draftspec run . --parallel

# Tag filtering
draftspec run . --tags unit,fast           # Only run these tags
draftspec run . --exclude-tags slow,flaky  # Exclude these tags

# Bail on first failure
draftspec run . --bail
```

**Exit Codes:**

| Code | Meaning |
|------|---------|
| `0` | All specs passed |
| `1` | One or more specs failed |

---

### draftspec watch

Run specs and re-run automatically when files change.

```bash
draftspec watch <path>
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<path>` | Directory to watch. Defaults to current directory (`.`) |

**Behavior:**

- Runs all specs immediately on start
- Watches for changes to `.cs`, `.csx`, and `.csproj` files
- Re-runs all specs when changes are detected
- Press `Ctrl+C` to stop watching

**Example:**

```bash
# Watch current directory
draftspec watch .

# Watch specific folder
draftspec watch ./specs
```

**Output:**

```
Watching for changes... (Ctrl+C to stop)

Calculator.spec.csx
  Calculator
    ✓ adds numbers
    ✓ handles negatives

2 passed

[12:34:56] Watching for changes...
[12:35:10] Change detected, re-running...
```

---

### draftspec init

Initialize DraftSpec in a project directory.

```bash
draftspec init [path] [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `[path]` | Directory to initialize. Defaults to current directory (`.`) |

**Options:**

| Option | Description |
|--------|-------------|
| `--force` | Overwrite existing files |

**Created Files:**

| File | Purpose |
|------|---------|
| `spec_helper.csx` | Shared references and fixtures for spec files |
| `omnisharp.json` | IDE configuration for IntelliSense support |

**Example:**

```bash
# Initialize current directory
draftspec init

# Initialize specific directory
draftspec init ./MyProject

# Overwrite existing files
draftspec init --force
```

**Generated spec_helper.csx:**

```csharp
#r "nuget: DraftSpec"
#r "bin/Debug/net10.0/MyProject.dll"

using static DraftSpec.Dsl;

// Add shared fixtures below:
```

**Generated omnisharp.json:**

```json
{
  "script": {
    "enableScriptNuGetReferences": true,
    "defaultTargetFramework": "net10.0"
  }
}
```

---

### draftspec new

Create a new spec file from a template.

```bash
draftspec new <name> [path]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<name>` | Name for the spec (creates `<name>.spec.csx`) |
| `[path]` | Directory to create file in. Defaults to current directory (`.`) |

**Example:**

```bash
# Create UserService.spec.csx
draftspec new UserService

# Create in specific directory
draftspec new OrderProcessor ./specs
```

**Generated File:**

```csharp
#load "spec_helper.csx"
using static DraftSpec.Dsl;

describe("UserService", () => {
    it("works", () => pending());
});
```

---

### draftspec list

Discover and list specs without executing them. Uses static parsing to analyze spec structure from CSX files.

```bash
draftspec list <path> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<path>` | File or directory to scan. Defaults to current directory (`.`) |

**Options:**

| Option | Description |
|--------|-------------|
| `--list-format <format>` | Output format: `tree` (default), `flat`, `json` |
| `--show-line-numbers` | Show line numbers (default: true) |
| `--no-line-numbers` | Hide line numbers |
| `--focused-only` | Show only focused specs (`fit`) |
| `--pending-only` | Show only pending specs (no body) |
| `--skipped-only` | Show only skipped specs (`xit`) |
| `--filter-name <pattern>` | Filter by spec name (regex or substring) |
| `--filter-tags <tags>` | Filter by tags (comma-separated) |
| `-o, --output <file>` | Write output to file instead of stdout |

**Examples:**

```bash
# List all specs in tree format (default)
draftspec list .

# List specs in flat format (one line per spec, grep-friendly)
draftspec list . --list-format flat

# Export specs as JSON for tooling integration
draftspec list . --list-format json -o specs.json

# Show only focused specs
draftspec list . --focused-only

# Show only pending specs
draftspec list . --pending-only

# Filter by name pattern
draftspec list . --filter-name "User"

# Hide line numbers
draftspec list . --no-line-numbers
```

**Output Formats:**

**Tree format** (default):
```
features_showcase.spec.csx
├─ DraftSpec Features
│  ├─ Basic Syntax
│  │  ├─ [.] describes behavior with 'it' :15
│  │  ├─ [?] marks pending specs :20
│  │  └─ [-] explicitly skips specs with 'xit' :25
│  └─ Assertions
│     └─ [.] uses expect API :32

Summary: 4 specs (1 pending, 1 skipped) in 1 file
```

Icons:
- `[.]` - Regular spec
- `[*]` - Focused spec (`fit`)
- `[-]` - Skipped spec (`xit`)
- `[?]` - Pending spec (no body)
- `[!]` - Compilation error

**Flat format**:
```
features_showcase.spec.csx:15  DraftSpec Features > Basic Syntax > describes behavior with 'it'
features_showcase.spec.csx:20  DraftSpec Features > Basic Syntax > marks pending specs [PENDING]
features_showcase.spec.csx:25  DraftSpec Features > Basic Syntax > explicitly skips specs with 'xit' [SKIPPED]
```

**JSON format**:
```json
{
  "specs": [
    {
      "id": "features_showcase.spec.csx:DraftSpec Features/Basic Syntax/describes behavior",
      "description": "describes behavior with 'it'",
      "displayName": "DraftSpec Features > Basic Syntax > describes behavior with 'it'",
      "contextPath": ["DraftSpec Features", "Basic Syntax"],
      "relativeSourceFile": "features_showcase.spec.csx",
      "lineNumber": 15,
      "type": "regular",
      "isPending": false,
      "isSkipped": false,
      "isFocused": false
    }
  ],
  "summary": {
    "totalSpecs": 85,
    "focusedCount": 0,
    "skippedCount": 5,
    "pendingCount": 3,
    "errorCount": 0,
    "totalFiles": 4
  },
  "errors": []
}
```

**Exit Codes:**

| Code | Meaning |
|------|---------|
| `0` | Success (specs found or no specs) |
| `1` | Error (invalid path, invalid options) |

**Use Cases:**

- **CI/CD**: Generate spec inventory for test planning
- **IDE Integration**: Pre-populate test trees without execution
- **Documentation**: Export spec structure for review
- **Analysis**: Find focused/skipped specs before commits

---

### draftspec validate

Validate spec structure without execution. Uses static parsing to detect structural issues before running expensive test suites.

```bash
draftspec validate <path> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<path>` | File or directory to validate. Defaults to current directory (`.`) |

**Options:**

| Option | Description |
|--------|-------------|
| `--static` | Use static parsing only (default, for documentation) |
| `--strict` | Treat warnings as errors (exit code 2) |
| `--quiet, -q` | Show only errors, suppress progress output |
| `--files <files>` | Validate specific files (comma-separated, for pre-commit hooks) |

**Exit Codes:**

| Code | Meaning |
|------|---------|
| `0` | Validation passed (no errors, warnings OK) |
| `1` | Validation failed (structural errors found) |
| `2` | Warnings found with `--strict` mode |

**Examples:**

```bash
# Validate all specs in current directory
draftspec validate .

# CI mode: quiet output, strict warnings
draftspec validate --static --quiet --strict

# Validate specific files (for pre-commit hooks)
draftspec validate --files "user.spec.csx,order.spec.csx"

# Validate with full output
draftspec validate ./specs
```

**Output:**

```
Validating spec structure...

✓ calculator.spec.csx - 5 specs
✓ user_service.spec.csx - 12 specs
⚠ legacy.spec.csx - 3 specs
  Line 15: 'describe' has dynamic description - cannot analyze statically
✗ broken.spec.csx
  Line 8: missing description argument

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Files: 4 | Specs: 20 | Errors: 1 | Warnings: 1
```

**Detected Issues:**

| Type | Severity | Example |
|------|----------|---------|
| Missing description | Error | `it()` with no arguments |
| Empty description | Error | `it("")` |
| Parse/syntax error | Error | Invalid C# syntax |
| Dynamic description | Warning | `it($"test {name}")` |

**Use Cases:**

- **CI/CD**: Fail fast before expensive test execution
- **Pre-commit hooks**: Validate changed specs locally
- **Code review**: Check spec structure in PRs
- **IDE integration**: Quick validation without running tests

---

### draftspec flaky

Manage flaky test detection and history.

```bash
draftspec flaky <path> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<path>` | File or directory to analyze. Defaults to current directory (`.`) |

**Options:**

| Option | Description |
|--------|-------------|
| `--show-history` | Show historical pass/fail patterns for specs |
| `--threshold <n>` | Minimum failures to flag as flaky (default: 2) |
| `--window <n>` | Number of recent runs to analyze (default: 10) |

**Examples:**

```bash
# Show flaky specs
draftspec flaky .

# Show detailed history
draftspec flaky . --show-history

# Customize detection threshold
draftspec flaky . --threshold 3 --window 20
```

---

### draftspec estimate

Estimate test run duration based on historical data.

```bash
draftspec estimate <path> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<path>` | File or directory to estimate. Defaults to current directory (`.`) |

**Options:**

| Option | Description |
|--------|-------------|
| `--percentile <n>` | Duration percentile to use (default: 95) |
| `--format <format>` | Output format: `human` (default), `json` |

**Examples:**

```bash
# Estimate run time
draftspec estimate .

# Use median (50th percentile)
draftspec estimate . --percentile 50

# JSON output for tooling
draftspec estimate . --format json
```

---

### draftspec cache

Manage the spec compilation and results cache.

```bash
draftspec cache <subcommand> [options]
```

**Subcommands:**

| Subcommand | Description |
|------------|-------------|
| `clear` | Remove all cached data |
| `stats` | Show cache statistics |
| `prune` | Remove stale entries |

**Examples:**

```bash
# Clear all cached data
draftspec cache clear

# Show cache statistics
draftspec cache stats

# Remove stale entries
draftspec cache prune
```

---

### draftspec docs

Generate living documentation from spec structure. Uses static parsing to discover specs without executing them.

```bash
draftspec docs <path> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<path>` | File or directory containing specs. Defaults to current directory (`.`) |

**Options:**

| Option | Description |
|--------|-------------|
| `--format, -f <format>` | Output format: `markdown` (default), `html` |
| `--context <pattern>` | Filter to specific describe/context blocks |
| `--with-results` | Include test results from a previous run |
| `--results-file <file>` | Path to JSON results file (requires `--with-results`) |
| `--filter-name <pattern>` | Filter specs by name pattern |

**Examples:**

```bash
# Generate markdown documentation
draftspec docs .

# Generate HTML documentation
draftspec docs . --format html

# Filter to specific context
draftspec docs . --context "UserService"

# Include results from previous run
draftspec docs . --with-results --results-file results.json

# Filter by spec name
draftspec docs . --filter-name "should.*create"
```

**Output:**

The `docs` command produces structured documentation showing your spec hierarchy:

**Markdown format:**
```markdown
# Spec Documentation

Generated: 2026-01-03

## UserService

### Authentication

- [✓] validates user credentials
- [✓] handles invalid passwords
- [?] supports two-factor auth (pending)

### Authorization

- [✓] checks user permissions
- [-] enforces rate limits (skipped)
```

**Use Cases:**

- **Living Documentation**: Keep documentation in sync with tests
- **Review**: Generate readable spec summaries for non-technical stakeholders
- **Audit**: Track which specs are pending or skipped
- **CI/CD**: Generate documentation artifacts alongside test reports

---

### draftspec coverage-map

Analyze spec coverage of source code methods. Uses static parsing to map specs to the source methods they reference.

```bash
draftspec coverage-map <source-path> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<source-path>` | Path to source files or directory to analyze |

**Options:**

| Option | Description |
|--------|-------------|
| `--spec-path <path>` | Path to spec files (defaults to project root) |
| `--format, -f <format>` | Output format: `console` (default), `json` |
| `--gaps-only` | Show only uncovered methods |
| `--namespace-filter <ns>` | Filter to specific namespaces (comma-separated) |

**Examples:**

```bash
# Analyze source coverage
draftspec coverage-map ./src

# Specify spec location
draftspec coverage-map ./src --spec-path ./specs

# Show only uncovered methods
draftspec coverage-map ./src --gaps-only

# Filter by namespace
draftspec coverage-map ./src --namespace-filter "MyApp.Services,MyApp.Models"

# JSON output for tooling
draftspec coverage-map ./src --format json
```

**Output:**

**Console format:**
```
Coverage Map: src/ → specs/

UserService.cs
  ✓ CreateUser (3 specs)
  ✓ UpdateUser (2 specs)
  ✗ DeleteUser (0 specs) <- UNCOVERED
  ✓ GetUserById (1 spec)

OrderService.cs
  ✓ CreateOrder (4 specs)
  ✗ CancelOrder (0 specs) <- UNCOVERED

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Coverage: 8/10 methods (80%)
Uncovered: DeleteUser, CancelOrder
```

**JSON format:**
```json
{
  "sourcePath": "src/",
  "specPath": "specs/",
  "methods": [
    {
      "name": "CreateUser",
      "file": "UserService.cs",
      "namespace": "MyApp.Services",
      "coveredBy": ["UserService.spec.csx:15", "UserService.spec.csx:23"]
    }
  ],
  "uncoveredMethods": [
    {"name": "DeleteUser", "file": "UserService.cs", "namespace": "MyApp.Services"}
  ],
  "summary": {
    "totalMethods": 10,
    "coveredMethods": 8,
    "coveragePercent": 80
  }
}
```

**Exit Codes:**

| Code | Meaning |
|------|---------|
| `0` | Success (or no uncovered methods in `--gaps-only` mode) |
| `1` | Uncovered methods found (in `--gaps-only` mode) |

**Use Cases:**

- **Coverage Gaps**: Identify which methods lack test coverage
- **CI/CD Enforcement**: Fail builds when coverage drops below threshold
- **Documentation**: Generate coverage reports for code review
- **Planning**: Prioritize which code needs more testing

---

### draftspec schema

Output the JSON schema for DraftSpec configuration and output formats.

```bash
draftspec schema [options]
```

**Options:**

| Option | Description |
|--------|-------------|
| `-o, --output <file>` | Write schema to file instead of stdout |

**Examples:**

```bash
# Output schema to stdout
draftspec schema

# Write schema to file
draftspec schema -o list-schema.json
```

**Output:**

The schema describes the structure of `draftspec list --format json` output:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "properties": {
    "specs": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "id": { "type": "string" },
          "description": { "type": "string" },
          "displayName": { "type": "string" },
          "contextPath": { "type": "array", "items": { "type": "string" } },
          "relativeSourceFile": { "type": "string" },
          "lineNumber": { "type": "integer" },
          "isPending": { "type": "boolean" },
          "isSkipped": { "type": "boolean" },
          "isFocused": { "type": "boolean" }
        }
      }
    },
    "summary": { ... },
    "errors": { ... }
  }
}
```

**Use Cases:**

- **IDE Integration**: Validate JSON output from `draftspec list`
- **Tooling**: Build tools that consume DraftSpec output
- **Documentation**: Reference for JSON output structure

---

## Output Formats

### Console (default)

Human-readable output with colors and symbols:

```
Calculator
  Add
    ✓ returns 0 for empty input
    ✓ returns the number for single input
    ✓ sums multiple numbers
  Subtract
    ✗ subtracts numbers
      Expected result to be 5, but was 3

3 passed, 1 failed
```

### JSON

Structured data for tooling integration:

```bash
draftspec run . --format json
```

```json
{
  "timestamp": "2025-01-15T10:30:00Z",
  "source": "/path/to/specs",
  "summary": {
    "total": 4,
    "passed": 3,
    "failed": 1,
    "pending": 0,
    "skipped": 0,
    "durationMs": 234.5
  },
  "contexts": [
    {
      "description": "Calculator",
      "specs": [],
      "contexts": [
        {
          "description": "Add",
          "specs": [
            {
              "description": "returns 0 for empty input",
              "status": "passed",
              "durationMs": 12.3
            }
          ]
        }
      ]
    }
  ]
}
```

### Markdown

Documentation-friendly format:

```bash
draftspec run . --format markdown -o results.md
```

```markdown
# Spec Results

**Total:** 4 | **Passed:** 3 | **Failed:** 1

## Calculator

### Add

- ✓ returns 0 for empty input
- ✓ returns the number for single input

### Subtract

- ✗ subtracts numbers
  > Expected result to be 5, but was 3
```

### HTML

Browser-viewable report:

```bash
draftspec run . --format html -o report.html
```

Opens in browser with:
- Collapsible context sections
- Color-coded results
- Error details with stack traces
- Summary statistics

Custom styling:
```bash
draftspec run . --format html --css-url ./my-theme.css -o report.html
```

---

## CI/CD Integration

### Pre-flight Validation

Validate spec structure before running expensive test suites:

```yaml
# GitHub Actions - Validate then test
name: Test Suite
on: [push, pull_request]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Install DraftSpec CLI
        run: dotnet tool install -g DraftSpec.Cli --prerelease
      - name: Validate spec structure
        run: draftspec validate --static --strict --quiet
        # Fails fast if specs have structural issues

  test:
    needs: validate  # Only run if validation passes
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Install DraftSpec CLI
        run: dotnet tool install -g DraftSpec.Cli --prerelease
      - name: Run specs
        run: draftspec run . --format json -o results.json
      - name: Upload results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: spec-results
          path: results.json
```

### Pre-commit Hook

Validate changed spec files locally before committing:

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Get changed spec files (staged for commit)
CHANGED_SPECS=$(git diff --cached --name-only --diff-filter=ACM | grep '\.spec\.csx$')

if [ -n "$CHANGED_SPECS" ]; then
  echo "Validating spec files..."

  # Convert newlines to commas for --files flag
  FILES=$(echo "$CHANGED_SPECS" | tr '\n' ',' | sed 's/,$//')

  draftspec validate --static --quiet --files "$FILES"

  if [ $? -ne 0 ]; then
    echo ""
    echo "❌ Spec validation failed. Fix errors before committing."
    exit 1
  fi

  echo "✓ Spec validation passed"
fi
```

To install the hook:

```bash
# Copy hook to .git/hooks
cp pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit

# Or use a hook manager like husky or lefthook
```

### GitHub Actions

```yaml
- name: Run specs
  run: |
    dotnet tool install -g DraftSpec.Cli --prerelease
    draftspec run . --format json -o results.json

- name: Upload results
  uses: actions/upload-artifact@v4
  with:
    name: spec-results
    path: results.json
```

### GitLab CI

```yaml
# .gitlab-ci.yml
stages:
  - validate
  - test

variables:
  DOTNET_VERSION: "10.0"

.dotnet_setup:
  image: mcr.microsoft.com/dotnet/sdk:10.0
  before_script:
    - dotnet tool install -g DraftSpec.Cli --prerelease
    - export PATH="$PATH:$HOME/.dotnet/tools"

validate:specs:
  extends: .dotnet_setup
  stage: validate
  script:
    - draftspec validate --static --strict --quiet
  rules:
    - if: $CI_PIPELINE_SOURCE == "merge_request_event"
    - if: $CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH

test:specs:
  extends: .dotnet_setup
  stage: test
  needs: ["validate:specs"]
  script:
    - draftspec run . --format json -o results.json
  artifacts:
    when: always
    paths:
      - results.json
    reports:
      dotenv: results.json
```

### Exit Code Handling

```bash
# Fail CI on spec failures
draftspec run . || exit 1

# Continue on failure, capture result
draftspec run . --format json -o results.json
RESULT=$?
# Process results...
exit $RESULT
```

### Parallel Execution

For faster CI runs with multiple spec files:

```bash
draftspec run . --parallel
```

### Partitioned Execution

Split specs across multiple CI jobs for maximum parallelism:

```yaml
# GitHub Actions matrix strategy
jobs:
  test:
    strategy:
      matrix:
        partition: [0, 1, 2, 3]
    steps:
      - name: Run partition ${{ matrix.partition }}
        run: |
          draftspec run . --partition 4 --partition-index ${{ matrix.partition }}
```

---

## Spec File Discovery

The CLI discovers spec files using these rules:

1. **Single file:** If path is a `.spec.csx` file, run that file
2. **Directory:** Find all `*.spec.csx` files recursively
3. **Pattern:** Files must end with `.spec.csx`

```bash
# These all work
draftspec run Calculator.spec.csx     # Single file
draftspec run .                        # Current directory
draftspec run ./specs                  # Subdirectory
draftspec run ../other-project        # Relative path
```

---

## Security

### Path Validation

The CLI validates paths to prevent directory traversal:

- Output files (`-o`) must be within the current directory
- Spec files must be within the specified base directory
- Attempts to escape these boundaries throw `SecurityException`

```bash
# These are rejected
draftspec run . -o ../outside/report.json  # Error: outside current dir
draftspec run ../../../etc/passwd          # Error: path traversal
```

---

## Configuration File

DraftSpec supports a `draftspec.json` configuration file for project-level defaults.

### File Location

Place `draftspec.json` in your project root (same directory where you run `draftspec`).

### Configuration Options

```json
{
  "specPattern": "**/*.spec.csx",
  "timeout": 10000,
  "parallel": true,
  "maxParallelism": 4,
  "reporters": ["console", "json"],
  "outputDirectory": "./test-results",
  "tags": {
    "include": ["unit", "fast"],
    "exclude": ["slow", "integration"]
  },
  "bail": false,
  "noCache": false,
  "format": "console"
}
```

| Option | Type | Description |
|--------|------|-------------|
| `specPattern` | string | Glob pattern for spec files |
| `timeout` | number | Spec timeout in milliseconds |
| `parallel` | boolean | Run spec files in parallel |
| `maxParallelism` | number | Maximum concurrent spec files |
| `reporters` | string[] | Reporter names to use |
| `outputDirectory` | string | Directory for output files |
| `tags.include` | string[] | Only run specs with these tags |
| `tags.exclude` | string[] | Exclude specs with these tags |
| `bail` | boolean | Stop on first failure |
| `noCache` | boolean | Disable dotnet-script caching |
| `format` | string | Default output format |

### CLI Override

CLI options always override config file values:

```bash
# Config has parallel: false, but CLI enables it
draftspec run . --parallel

# Config has tags.include: ["unit"], CLI overrides
draftspec run . --tags integration
```

### Validation

Invalid configuration produces clear error messages:

```
Error: draftspec.json: timeout must be a positive number
Error: draftspec.json: maxParallelism must be a positive number
```

---

## Troubleshooting

### "No spec files found"

- Check the path contains `*.spec.csx` files
- Verify file extension is exactly `.spec.csx`

### "Could not load assembly"

- Run `dotnet build` before running specs
- Check `spec_helper.csx` references the correct DLL path

### IDE IntelliSense not working

- Run `draftspec init` to create `omnisharp.json`
- Restart your IDE/language server

### Specs run slowly

- Use `--parallel` for multiple spec files
- Check for slow `beforeAll`/`afterAll` hooks
- Set timeout via `draftspec.json` or CLI flags

---

## Alternative: Microsoft Testing Platform

For projects that need IDE Test Explorer integration or `dotnet test` compatibility, consider using the [MTP Integration](mtp-integration.md) instead of the CLI.

| Feature | CLI | MTP (`dotnet test`) |
|---------|-----|---------------------|
| Standalone execution | Yes | No (requires project) |
| Watch mode | Yes | No |
| IDE Test Explorer | No | Yes |
| Code coverage | External | Built-in |
| CI/CD | Custom | Standard `dotnet test` |

The CLI is ideal for:
- Quick prototyping and exploration
- Standalone spec scripts
- Watch mode development

MTP is better for:
- Enterprise/team environments
- IDE-centric workflows
- Standard .NET CI/CD pipelines

---

## See Also

- **[Getting Started](getting-started.md)** - First-time setup
- **[DSL Reference](dsl-reference.md)** - describe/it/hooks API
- **[MTP Integration](mtp-integration.md)** - dotnet test and IDE support
- **[Configuration](configuration.md)** - Middleware and plugins
