# CI/CD Integration Guide

This guide covers integrating DraftSpec into your CI/CD pipelines with ready-to-use workflow templates and best practices.

## Quick Start

### GitHub Actions (Minimal)

```yaml
name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet tool install -g DraftSpec.Cli --prerelease
      - run: draftspec run .
```

## Workflow Templates

We provide four ready-to-use workflow templates in [`docs/workflows/`](workflows/):

| Template | Purpose | When to Use |
|----------|---------|-------------|
| [`parallel-tests.yml`](workflows/parallel-tests.yml) | Dynamic partitioning | Large test suites (100+ specs) |
| [`pr-tests.yml`](workflows/pr-tests.yml) | Incremental testing | Fast PR feedback |
| [`full-suite.yml`](workflows/full-suite.yml) | Complete validation | Main branch, nightly |
| [`coverage.yml`](workflows/coverage.yml) | Coverage reporting | Track test coverage |

### Installation

Copy templates to your `.github/workflows/` directory:

```bash
# Copy all templates
cp docs/workflows/*.yml .github/workflows/

# Or copy specific ones
cp docs/workflows/pr-tests.yml .github/workflows/
```

---

## Workflow Details

### 1. Parallel Tests (`parallel-tests.yml`)

Dynamically partitions specs across multiple runners based on spec count.

**How it works:**

1. **Discover phase**: Counts total specs, calculates optimal partition count
2. **Test phase**: Runs partitions in parallel using matrix strategy
3. **Aggregate phase**: Combines results for summary

**Configuration:**

```yaml
# Adjust partition calculation in discover job
PARTITIONS=$(( (SPEC_COUNT + 49) / 50 ))  # 1 partition per 50 specs
PARTITIONS=$(( PARTITIONS > 4 ? 4 : PARTITIONS ))  # Max 4 partitions
```

**When to use:**
- Test suite takes > 5 minutes
- 100+ specs
- Want faster CI feedback

---

### 2. PR Tests (`pr-tests.yml`)

Runs only specs that changed in the PR for fast feedback.

**How it works:**

1. **Analyze phase**: Identifies changed `.spec.csx` files
2. **Decision**: Run changed files only, or fall back to full suite
3. **Validate phase**: Quick structural check on changed specs
4. **Test phase**: Execute affected specs

**Falls back to full suite when:**
- More than 10 spec files changed
- Non-spec C# files changed (might affect tests)

**Configuration:**

```yaml
env:
  MAX_CHANGED_FILES: 10  # Threshold for full suite fallback
```

**When to use:**
- PRs with focused changes
- Want sub-minute feedback on small changes

---

### 3. Full Suite (`full-suite.yml`)

Comprehensive test run with validation and reporting.

**How it works:**

1. **Validate phase**: Static analysis of all specs
2. **Test phase**: Run all specs with detailed output
3. **Notify phase**: Create issue on nightly failures (optional)

**Features:**
- Scheduled nightly runs
- Manual trigger with bail option
- Multiple output formats
- GitHub job summary with results

**Configuration:**

```yaml
on:
  schedule:
    - cron: '0 2 * * *'  # Adjust nightly time (UTC)
```

**When to use:**
- Main branch protection
- Nightly regression testing
- Release validation

---

### 4. Coverage (`coverage.yml`)

Collects code coverage and uploads to coverage services.

**How it works:**

1. Runs specs with `--coverage` flag
2. Generates Cobertura XML and HTML reports
3. Uploads to Codecov (configurable)
4. Optional coverage gate for PRs

**Prerequisites:**

```bash
# Codecov token (add to repository secrets)
CODECOV_TOKEN=xxx
```

**Configuration:**

```yaml
env:
  MIN_COVERAGE: 80  # Minimum coverage percentage for PR gate
```

**When to use:**
- Track coverage trends
- Enforce coverage minimums
- Generate coverage reports

---

## CI Platform Examples

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
```

### Azure Pipelines

```yaml
# azure-pipelines.yml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'

  - script: dotnet tool install -g DraftSpec.Cli --prerelease
    displayName: 'Install DraftSpec'

  - script: draftspec validate --static --strict
    displayName: 'Validate specs'

  - script: draftspec run . --format json -o $(Build.ArtifactStagingDirectory)/results.json
    displayName: 'Run specs'

  - task: PublishBuildArtifacts@1
    condition: always()
    inputs:
      pathToPublish: '$(Build.ArtifactStagingDirectory)'
      artifactName: 'test-results'
```

### CircleCI

```yaml
# .circleci/config.yml
version: 2.1

jobs:
  test:
    docker:
      - image: mcr.microsoft.com/dotnet/sdk:10.0
    steps:
      - checkout
      - run:
          name: Install DraftSpec
          command: dotnet tool install -g DraftSpec.Cli --prerelease
      - run:
          name: Validate specs
          command: ~/.dotnet/tools/draftspec validate --static --strict
      - run:
          name: Run specs
          command: ~/.dotnet/tools/draftspec run . --format json -o results.json
      - store_artifacts:
          path: results.json

workflows:
  test:
    jobs:
      - test
```

---

## Pre-commit Hooks

Validate specs locally before committing.

### Bash Hook

```bash
#!/bin/bash
# .git/hooks/pre-commit

CHANGED_SPECS=$(git diff --cached --name-only --diff-filter=ACM | grep '\.spec\.csx$')

if [ -n "$CHANGED_SPECS" ]; then
  echo "Validating spec files..."
  FILES=$(echo "$CHANGED_SPECS" | tr '\n' ',' | sed 's/,$//')

  draftspec validate --static --quiet --files "$FILES"

  if [ $? -ne 0 ]; then
    echo "Spec validation failed. Fix errors before committing."
    exit 1
  fi

  echo "Spec validation passed"
fi
```

### Husky (Node.js projects)

```json
// package.json
{
  "husky": {
    "hooks": {
      "pre-commit": "draftspec validate --static --quiet"
    }
  }
}
```

### Lefthook

```yaml
# lefthook.yml
pre-commit:
  commands:
    validate-specs:
      glob: "*.spec.csx"
      run: draftspec validate --static --quiet --files {staged_files}
```

---

## Exit Codes

Use exit codes for CI decisions:

| Code | Meaning | Action |
|------|---------|--------|
| `0` | All specs passed | Continue pipeline |
| `1` | Specs failed or error | Fail build |
| `2` | Warnings (with `--strict`) | Fail or warn |

```yaml
- name: Run specs
  run: draftspec run .
  continue-on-error: false  # Fail job on non-zero exit
```

---

## Output Formats

| Format | Flag | Use Case |
|--------|------|----------|
| Console | `--format console` | Human-readable CI logs |
| JSON | `--format json` | Artifact storage, parsing |
| Markdown | `--format markdown` | PR comments, summaries |
| HTML | `--format html` | Browse-able reports |

### GitHub Job Summary

```yaml
- name: Run specs with summary
  run: |
    draftspec run . --format markdown -o results.md
    cat results.md >> $GITHUB_STEP_SUMMARY
```

---

## Badges

Add status badges to your README:

### GitHub Actions

```markdown
[![Tests](https://github.com/USER/REPO/actions/workflows/tests.yml/badge.svg)](https://github.com/USER/REPO/actions/workflows/tests.yml)
```

### Codecov

```markdown
[![codecov](https://codecov.io/gh/USER/REPO/graph/badge.svg)](https://codecov.io/gh/USER/REPO)
```

### Custom Badge (shields.io)

```markdown
[![specs](https://img.shields.io/badge/specs-passing-brightgreen)](https://github.com/USER/REPO/actions)
```

---

## Best Practices

### 1. Validate First

Always validate before running - it's much faster:

```yaml
jobs:
  validate:
    steps:
      - run: draftspec validate --static --strict
  test:
    needs: validate
    steps:
      - run: draftspec run .
```

### 2. Use Partitioning for Large Suites

```yaml
strategy:
  matrix:
    partition: [0, 1, 2, 3]
steps:
  - run: draftspec run . --partition 4 --partition-index ${{ matrix.partition }}
```

### 3. Cache Dependencies

```yaml
- uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: nuget-${{ hashFiles('**/*.csproj') }}
```

### 4. Artifact Everything

```yaml
- uses: actions/upload-artifact@v4
  if: always()
  with:
    name: test-results
    path: results.json
```

### 5. Use Bail for Fast Failure

```yaml
- run: draftspec run . --bail  # Stop on first failure
```

---

## Troubleshooting

### Tool Not Found

```bash
# Ensure tool path is in PATH
export PATH="$PATH:$HOME/.dotnet/tools"
```

### Permission Denied

```bash
# On self-hosted runners, ensure dotnet tools are executable
chmod +x ~/.dotnet/tools/draftspec
```

### No Specs Found

```bash
# Verify spec file pattern
ls -la **/*.spec.csx

# Check current directory
draftspec list .
```

---

## Related Documentation

- [CLI Reference](cli.md) - Complete CLI options
- [Configuration](configuration.md) - Project configuration file
- [MTP Integration](mtp-integration.md) - `dotnet test` integration
