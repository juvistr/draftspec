# CLI Reference

Complete reference for the DraftSpec command-line interface.

## Installation

```bash
dotnet tool install -g DraftSpec.Cli
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

run();
```

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

### GitHub Actions

```yaml
- name: Run specs
  run: |
    dotnet tool install -g DraftSpec.Cli
    draftspec run . --format json -o results.json

- name: Upload results
  uses: actions/upload-artifact@v4
  with:
    name: spec-results
    path: results.json
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
- Consider `configure(runner => runner.WithTimeout(5000))`

---

## See Also

- **[Getting Started](getting-started.md)** - First-time setup
- **[DSL Reference](dsl-reference.md)** - describe/it/hooks API
- **[Configuration](configuration.md)** - Middleware and plugins
