# Environment Variables

DraftSpec uses environment variables for runtime configuration. This reference covers all supported variables, their defaults, and common use cases.

## Quick Reference

| Variable | Purpose | Default |
|----------|---------|---------|
| `DRAFTSPEC_UPDATE_SNAPSHOTS` | Enable snapshot update mode | `false` |
| `DRAFTSPEC_JSON_OUTPUT_FILE` | Write JSON results to file | - |
| `DRAFTSPEC_PROGRESS_STREAM` | Enable progress streaming | `false` |
| `DRAFTSPEC_FILTER_TAGS` | Include only these tags | - |
| `DRAFTSPEC_EXCLUDE_TAGS` | Exclude specs with these tags | - |
| `DRAFTSPEC_FILTER_NAME` | Include specs matching pattern | - |
| `DRAFTSPEC_EXCLUDE_NAME` | Exclude specs matching pattern | - |

## Configuration Precedence

DraftSpec configuration follows this precedence (highest to lowest):

1. **CLI flags** - Always win
2. **Environment variables** - Override config file
3. **Configuration file** (`draftspec.json`) - Project defaults
4. **Built-in defaults** - Fallback values

**Example:**

```bash
# draftspec.json has: "parallel": false
# Environment has: (not set)
# CLI has: --parallel

# Result: parallel execution is ENABLED (CLI wins)
```

## Detailed Reference

### DRAFTSPEC_UPDATE_SNAPSHOTS

Enable snapshot update mode. When enabled, snapshot assertions update stored snapshots instead of comparing against them.

**Values:** `true`, `1` (enabled) | Any other value or unset (disabled)

**Use cases:**
- Accept intentional output changes
- Initialize snapshots for new tests

```bash
# Update all snapshots
DRAFTSPEC_UPDATE_SNAPSHOTS=true draftspec run .

# Windows PowerShell
$env:DRAFTSPEC_UPDATE_SNAPSHOTS = "true"
draftspec run .

# Windows Command Prompt
set DRAFTSPEC_UPDATE_SNAPSHOTS=true
draftspec run .
```

**See also:** [Snapshot Testing](snapshot-testing.md)

---

### DRAFTSPEC_JSON_OUTPUT_FILE

Write JSON results to a file automatically. When set, DraftSpec adds a FileReporter that writes the complete test report in JSON format.

**Values:** File path (absolute or relative)

**Use cases:**
- CI/CD artifact generation
- Integration with external tools
- MCP server integration

```bash
# Write results to file
DRAFTSPEC_JSON_OUTPUT_FILE=./results.json draftspec run .

# In CI/CD (GitHub Actions)
- name: Run tests
  run: draftspec run .
  env:
    DRAFTSPEC_JSON_OUTPUT_FILE: ./test-results/report.json

- name: Upload results
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: ./test-results/report.json
```

---

### DRAFTSPEC_PROGRESS_STREAM

Enable progress streaming output. When enabled, DraftSpec outputs progress events prefixed with `DRAFTSPEC_PROGRESS:` for real-time monitoring.

**Values:** `true`, `1` (enabled) | Any other value or unset (disabled)

**Use cases:**
- MCP server integration
- Real-time test monitoring
- IDE integrations

```bash
# Enable progress streaming
DRAFTSPEC_PROGRESS_STREAM=true draftspec run .
```

**Output format:**
```
DRAFTSPEC_PROGRESS:{"type":"spec_started","spec":"Calculator > adds numbers"}
DRAFTSPEC_PROGRESS:{"type":"spec_completed","spec":"Calculator > adds numbers","status":"passed","durationMs":12}
```

---

### DRAFTSPEC_FILTER_TAGS

Filter specs to only run those with specified tags. Multiple tags are comma-separated. A spec runs if it has **any** of the specified tags.

**Values:** Comma-separated tag names

**Use cases:**
- Run only unit tests in development
- Skip slow tests locally
- CI/CD test partitioning

```bash
# Run only unit tests
DRAFTSPEC_FILTER_TAGS=unit draftspec run .

# Run unit or fast tests
DRAFTSPEC_FILTER_TAGS=unit,fast draftspec run .
```

**Equivalent CLI:** `--tags unit,fast`

---

### DRAFTSPEC_EXCLUDE_TAGS

Exclude specs with specified tags. Multiple tags are comma-separated. A spec is excluded if it has **any** of the specified tags.

**Values:** Comma-separated tag names

**Use cases:**
- Skip slow integration tests locally
- Exclude flaky tests temporarily
- Skip platform-specific tests

```bash
# Skip slow and flaky tests
DRAFTSPEC_EXCLUDE_TAGS=slow,flaky draftspec run .

# Skip integration tests
DRAFTSPEC_EXCLUDE_TAGS=integration draftspec run .
```

**Equivalent CLI:** `--exclude-tags slow,flaky`

---

### DRAFTSPEC_FILTER_NAME

Filter specs by name pattern. Uses regex matching against the full spec description (including context path).

**Values:** Regex pattern

**Use cases:**
- Run specific feature tests
- Debug a particular spec
- Focus on a subsystem

```bash
# Run only UserService specs
DRAFTSPEC_FILTER_NAME="UserService" draftspec run .

# Run specs containing "create" or "update"
DRAFTSPEC_FILTER_NAME="create|update" draftspec run .

# Run specs starting with "should"
DRAFTSPEC_FILTER_NAME="^.*should" draftspec run .
```

**Equivalent CLI:** `--filter-name "pattern"`

---

### DRAFTSPEC_EXCLUDE_NAME

Exclude specs by name pattern. Uses regex matching against the full spec description.

**Values:** Regex pattern

**Use cases:**
- Skip work-in-progress specs
- Exclude known failing tests
- Skip expensive tests

```bash
# Exclude specs marked WIP
DRAFTSPEC_EXCLUDE_NAME="WIP|TODO" draftspec run .

# Exclude database specs
DRAFTSPEC_EXCLUDE_NAME="database|sql" draftspec run .
```

**Equivalent CLI:** `--exclude-name "pattern"`

---

## CI/CD Examples

### GitHub Actions

```yaml
name: Test Suite
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    env:
      # Global environment for all steps
      DRAFTSPEC_JSON_OUTPUT_FILE: ./test-results/report.json
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install CLI
        run: dotnet tool install -g DraftSpec.Cli --prerelease

      - name: Run unit tests
        run: draftspec run .
        env:
          DRAFTSPEC_FILTER_TAGS: unit

      - name: Run integration tests
        run: draftspec run .
        env:
          DRAFTSPEC_FILTER_TAGS: integration
          DRAFTSPEC_JSON_OUTPUT_FILE: ./test-results/integration.json

      - name: Upload results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: ./test-results/
```

### GitLab CI

```yaml
variables:
  DRAFTSPEC_JSON_OUTPUT_FILE: ./results.json

test:unit:
  script:
    - dotnet tool install -g DraftSpec.Cli --prerelease
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - draftspec run .
  variables:
    DRAFTSPEC_FILTER_TAGS: unit
  artifacts:
    when: always
    paths:
      - results.json

test:integration:
  script:
    - dotnet tool install -g DraftSpec.Cli --prerelease
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - draftspec run .
  variables:
    DRAFTSPEC_FILTER_TAGS: integration
    DRAFTSPEC_EXCLUDE_TAGS: flaky
```

### Azure Pipelines

```yaml
variables:
  DRAFTSPEC_JSON_OUTPUT_FILE: $(Build.ArtifactStagingDirectory)/results.json

steps:
- task: UseDotNet@2
  inputs:
    version: '10.0.x'

- script: dotnet tool install -g DraftSpec.Cli --prerelease
  displayName: Install DraftSpec CLI

- script: draftspec run .
  displayName: Run Tests
  env:
    DRAFTSPEC_FILTER_TAGS: $(TestTags)

- task: PublishBuildArtifacts@1
  condition: always()
  inputs:
    pathToPublish: $(Build.ArtifactStagingDirectory)
    artifactName: test-results
```

## Local Development

### Shell Profile

Add commonly used configurations to your shell profile:

```bash
# ~/.bashrc or ~/.zshrc

# Always exclude slow tests locally
export DRAFTSPEC_EXCLUDE_TAGS="slow,integration"

# Alias for quick unit test runs
alias ds-unit='DRAFTSPEC_FILTER_TAGS=unit draftspec run .'

# Alias for full test run
alias ds-full='DRAFTSPEC_EXCLUDE_TAGS="" draftspec run .'

# Alias for updating snapshots
alias ds-snap='DRAFTSPEC_UPDATE_SNAPSHOTS=true draftspec run .'
```

### direnv Integration

Use `.envrc` for project-specific defaults:

```bash
# .envrc
export DRAFTSPEC_EXCLUDE_TAGS="slow"
export DRAFTSPEC_JSON_OUTPUT_FILE="./test-results/local.json"
```

Remember to run `direnv allow` after creating the file.

## Troubleshooting

### Environment Variable Not Working

1. **Check spelling**: Variable names are case-sensitive
2. **Check value format**: Use `true` or `1` for boolean variables
3. **Check precedence**: CLI flags override environment variables
4. **Verify it's set**: Run `echo $DRAFTSPEC_FILTER_TAGS` (bash) or `$env:DRAFTSPEC_FILTER_TAGS` (PowerShell)

### Variable Set But Ignored

Check if a CLI flag is overriding it:

```bash
# Environment says exclude "slow"
export DRAFTSPEC_EXCLUDE_TAGS="slow"

# But CLI overrides with empty value
draftspec run . --exclude-tags ""
# Result: "slow" tests WILL run (CLI wins)
```

### Windows-Specific Issues

Use the correct syntax for your shell:

```powershell
# PowerShell (use $env:)
$env:DRAFTSPEC_UPDATE_SNAPSHOTS = "true"
draftspec run .

# Command Prompt (use set)
set DRAFTSPEC_UPDATE_SNAPSHOTS=true
draftspec run .

# Single command (PowerShell)
$env:DRAFTSPEC_UPDATE_SNAPSHOTS="true"; draftspec run .
```

## See Also

- **[CLI Reference](cli.md)** - Command-line options
- **[Configuration](configuration.md)** - Configuration file format
- **[Snapshot Testing](snapshot-testing.md)** - Snapshot testing guide
- **[CI/CD Integration](ci-cd-integration.md)** - CI/CD setup guides
