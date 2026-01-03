# Snapshot Testing

Snapshot testing captures the output of your code and compares it against a stored reference. This is particularly useful for testing complex data structures, serialized objects, or any output that would be tedious to assert manually.

## Quick Start

```csharp
#load "spec_helper.csx"
using static DraftSpec.Dsl;

describe("UserService", () =>
{
    it("returns user profile", () =>
    {
        var profile = service.GetProfile(123);

        // First run: creates snapshot
        // Subsequent runs: compares against stored snapshot
        expect(profile).toMatchSnapshot();
    });
});
```

## How It Works

1. **First run**: DraftSpec serializes the value to JSON and saves it as a snapshot
2. **Subsequent runs**: DraftSpec compares the current value against the stored snapshot
3. **Mismatch**: Test fails with a diff showing what changed
4. **Update mode**: Set `DRAFTSPEC_UPDATE_SNAPSHOTS=true` to accept changes

## Snapshot Storage

Snapshots are stored in a `__snapshots__` directory alongside your spec files:

```
specs/
├── UserService.spec.csx
├── OrderService.spec.csx
└── __snapshots__/
    ├── UserService.spec.snap.json
    └── OrderService.spec.snap.json
```

Each snapshot file contains all snapshots for that spec file, keyed by the full spec description:

```json
{
  "UserService > returns user profile": {
    "id": 123,
    "name": "John Doe",
    "email": "john@example.com"
  },
  "UserService > returns user with roles": {
    "id": 456,
    "name": "Jane Smith",
    "roles": ["admin", "user"]
  }
}
```

## Basic Usage

### Default Snapshot Name

By default, the snapshot is named using the full spec description (context path + spec description):

```csharp
describe("Calculator", () =>
{
    describe("complex operations", () =>
    {
        it("handles matrix multiplication", () =>
        {
            var result = calculator.MultiplyMatrices(a, b);
            expect(result).toMatchSnapshot();
            // Snapshot key: "Calculator > complex operations > handles matrix multiplication"
        });
    });
});
```

### Custom Snapshot Name

Use a custom name when you need multiple snapshots in one spec or want a shorter key:

```csharp
it("transforms data through pipeline", () =>
{
    var input = CreateTestData();

    var step1 = pipeline.Parse(input);
    expect(step1).toMatchSnapshot("after-parse");

    var step2 = pipeline.Transform(step1);
    expect(step2).toMatchSnapshot("after-transform");

    var step3 = pipeline.Validate(step2);
    expect(step3).toMatchSnapshot("after-validate");
});
```

## Updating Snapshots

When your code intentionally changes output, update snapshots using the environment variable:

```bash
# Update all snapshots
DRAFTSPEC_UPDATE_SNAPSHOTS=true draftspec run .

# Windows PowerShell
$env:DRAFTSPEC_UPDATE_SNAPSHOTS="true"; draftspec run .

# Windows Command Prompt
set DRAFTSPEC_UPDATE_SNAPSHOTS=true && draftspec run .
```

**Workflow:**

1. Make your code changes
2. Run tests - they fail because snapshots don't match
3. Review the diff to verify changes are intentional
4. Run with `DRAFTSPEC_UPDATE_SNAPSHOTS=true` to accept changes
5. Commit the updated snapshot files

## Failure Output

When a snapshot doesn't match, DraftSpec shows a diff:

```
Snapshot "UserService > returns user profile" does not match.

Expected:
{
  "id": 123,
  "name": "John Doe",
  "email": "john@example.com"
}

Actual:
{
  "id": 123,
  "name": "John Doe",
  "email": "johndoe@example.com"  // <- changed
}

Diff:
@@ -3,3 +3,3 @@
   "id": 123,
   "name": "John Doe",
-  "email": "john@example.com"
+  "email": "johndoe@example.com"
```

## Best Practices

### 1. Keep Snapshots Small and Focused

Snapshot only the relevant parts of your data:

```csharp
// Bad: Snapshot entire response with timestamps
expect(response).toMatchSnapshot();

// Good: Snapshot only the relevant data
expect(new {
    response.UserId,
    response.Products,
    response.TotalAmount
}).toMatchSnapshot();
```

### 2. Avoid Non-Deterministic Data

Remove or mock values that change between runs:

```csharp
// Bad: Contains timestamp that changes every run
expect(order).toMatchSnapshot();

// Good: Exclude or normalize dynamic values
expect(new {
    order.Id,
    order.Items,
    order.Status,
    // Omit: order.CreatedAt
}).toMatchSnapshot();
```

### 3. Review Snapshots in Code Review

Treat snapshot files as code:
- Review changes carefully in pull requests
- Question unexpected changes
- Keep snapshots in version control

### 4. Use Descriptive Custom Names

When using multiple snapshots, use clear names:

```csharp
it("processes order lifecycle", () =>
{
    expect(order).toMatchSnapshot("initial-state");

    order.Submit();
    expect(order).toMatchSnapshot("after-submit");

    order.Approve();
    expect(order).toMatchSnapshot("after-approval");
});
```

### 5. Organize Large Snapshot Files

For specs with many snapshots, consider splitting into focused spec files:

```
specs/
├── UserService.basic.spec.csx      # Basic CRUD operations
├── UserService.auth.spec.csx       # Authentication scenarios
├── UserService.roles.spec.csx      # Role management
└── __snapshots__/
    ├── UserService.basic.spec.snap.json
    ├── UserService.auth.spec.snap.json
    └── UserService.roles.spec.snap.json
```

## CI/CD Integration

### Prevent Accidental Updates

Never run with `DRAFTSPEC_UPDATE_SNAPSHOTS=true` in CI:

```yaml
# GitHub Actions
- name: Run tests
  run: draftspec run .
  env:
    DRAFTSPEC_UPDATE_SNAPSHOTS: "false"  # Explicit, though default
```

### Check for Uncommitted Snapshots

Add a check for modified snapshot files:

```yaml
- name: Check for uncommitted snapshot changes
  run: |
    if [[ -n $(git status --porcelain '**/\__snapshots__/*') ]]; then
      echo "Error: Uncommitted snapshot changes detected"
      git diff **/__snapshots__/*
      exit 1
    fi
```

## When to Use Snapshot Testing

**Good use cases:**
- Complex object structures (API responses, DTOs)
- Serialized output (JSON, XML, HTML)
- Configuration objects with many properties
- Error messages and formatted output
- Component render output

**Avoid for:**
- Simple values (use `toBe()` instead)
- Values that frequently change
- Large binary data
- Data with non-deterministic elements

## Comparison with Traditional Assertions

| Scenario | Traditional | Snapshot |
|----------|-------------|----------|
| Single value | `expect(x).toBe(5)` | Overkill |
| Object with 2-3 properties | `expect(x.name).toBe("John")` | Either works |
| Object with 10+ properties | Tedious to write | `toMatchSnapshot()` |
| Frequent output changes | Update each assertion | Update one snapshot |
| Reviewing changes | Harder to see full picture | Clear diff |

## Troubleshooting

### "toMatchSnapshot() must be called inside an it() block"

Snapshots require spec context to generate keys:

```csharp
// Wrong: Outside spec
var result = compute();
expect(result).toMatchSnapshot();  // Error!

// Correct: Inside it()
it("computes result", () =>
{
    var result = compute();
    expect(result).toMatchSnapshot();
});
```

### Snapshot key collisions

If two specs have the same description, use custom names:

```csharp
describe("UserService", () =>
{
    it("returns data", () =>
    {
        expect(userResult).toMatchSnapshot("user-data");
    });
});

describe("OrderService", () =>
{
    it("returns data", () =>
    {
        expect(orderResult).toMatchSnapshot("order-data");
    });
});
```

### Snapshots not updating

1. Verify environment variable is set correctly
2. Check for typos: `DRAFTSPEC_UPDATE_SNAPSHOTS` (not `UPDATE_SNAPSHOT`)
3. Value must be `"true"` or `"1"`

## See Also

- **[Assertions](assertions.md)** - Full assertion API reference
- **[DSL Reference](dsl-reference.md)** - describe/it/hooks API
- **[Environment Variables](environment-variables.md)** - All environment variables
