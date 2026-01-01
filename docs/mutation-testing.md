# Mutation Testing with Stryker.NET

DraftSpec uses [Stryker.NET](https://stryker-mutator.io/) for mutation testing to verify test effectiveness beyond code coverage metrics.

## What is Mutation Testing?

Mutation testing introduces small changes (mutations) to your source code and runs your tests against each mutation. If a test fails, the mutation is "killed" - your tests detected the bug. If all tests pass, the mutation "survived" - indicating a gap in test coverage.

**Example mutations:**
- `if (a > b)` becomes `if (a >= b)` or `if (a < b)`
- `return x + y` becomes `return x - y`
- `count++` becomes `count--`

## Running Locally

```bash
# Install the Stryker tool
dotnet tool restore

# Run mutation testing on full solution
dotnet stryker

# Run on specific project
dotnet stryker --project src/DraftSpec/DraftSpec.csproj

# Open HTML report
open StrykerOutput/*/reports/mutation-report.html
```

## Configuration

The `stryker-config.json` in the repository root controls mutation testing:

| Setting | Value | Description |
|---------|-------|-------------|
| `threshold-high` | 80% | Green zone - excellent test quality |
| `threshold-low` | 60% | Yellow zone - acceptable but needs improvement |
| `threshold-break` | 50% | Red zone - build fails below this |
| `timeout-ms` | 30000 | Max time per mutation test run |
| `concurrency` | 4 | Parallel mutation test runners |

## CI Integration

Mutation testing runs:
- **Weekly** on Sundays at 2 AM UTC (scheduled)
- **On demand** via manual workflow dispatch

Results are uploaded as artifacts and posted to the [Stryker Dashboard](https://dashboard.stryker-mutator.io/reports/github.com/juvistr/draftspec/main).

### Manual Run

1. Go to **Actions** > **Mutation Testing**
2. Click **Run workflow**
3. Select project (or "all" for full solution)
4. View results in workflow summary and artifacts

## Understanding Results

| Metric | Meaning |
|--------|---------|
| **Mutation Score** | Percentage of mutations killed by tests |
| **Killed** | Mutations detected by failing tests |
| **Survived** | Mutations that passed all tests (gaps) |
| **Timeout** | Mutations causing infinite loops (counted as killed) |
| **No Coverage** | Code not covered by any test |

### Improving Mutation Score

When a mutation survives:

1. **Identify the mutation** - Check the HTML report for the exact change
2. **Write a targeted test** - Add a test that would fail for that mutation
3. **Re-run Stryker** - Verify the mutation is now killed

Example: If `a > b` surviving as `a >= b`, add a test with `a == b` boundary case.

## Excluding Code

Some code shouldn't be mutated (e.g., logging, generated code):

```json
{
  "stryker-config": {
    "mutate": [
      "src/**/*.cs",
      "!**/*AssemblyInfo.cs",
      "!**/obj/**/*.cs"
    ],
    "ignore-mutations": ["string"]
  }
}
```

## Dashboard

Track mutation score trends at: https://dashboard.stryker-mutator.io/reports/github.com/juvistr/draftspec/main

The dashboard requires the `STRYKER_DASHBOARD_API_KEY` secret to be set in GitHub repository settings.

## Resources

- [Stryker.NET Documentation](https://stryker-mutator.io/docs/stryker-net/introduction/)
- [Stryker Dashboard](https://dashboard.stryker-mutator.io/)
- [Mutation Testing Patterns](https://stryker-mutator.io/docs/mutation-testing-elements/mutant-states-and-metrics/)
