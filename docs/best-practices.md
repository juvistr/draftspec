# Spec Authoring Best Practices

This guide covers patterns that work well with DraftSpec's static parsing, ensuring maximum tooling compatibility and a smooth developer experience.

## Why This Matters

DraftSpec uses static parsing to discover specs without executing them. This enables:
- Fast spec listing in `draftspec list`
- IDE integration and test discovery
- Accurate line numbers for navigation
- Efficient watch mode

Following these practices ensures your specs work seamlessly with all DraftSpec tooling.

## 1. Use Literal Descriptions

Static parsing can only detect string literals. Dynamic descriptions prevent tooling from discovering specs.

**Good** (static parsing works):
```csharp
it("creates a user with valid data", () => { });
it("validates email format", () => { });
```

**Avoid** (dynamic - cannot parse statically):
```csharp
var testName = "creates a user";
it($"{testName} with valid data", () => { });  // Dynamic

foreach (var scenario in scenarios)
{
    it($"handles {scenario}", () => { });  // Loop-generated
}
```

## 2. Use withData for Parameterized Tests

The `withData` method is designed for data-driven tests while maintaining static discoverability.

**Good** (static parsing detects base spec):
```csharp
withData(
    ("valid email", "test@example.com", true),
    ("invalid email", "invalid", false),
    ("empty email", "", false)
).it("validates email: {0}", (name, email, expected) => {
    expect(validator.IsValid(email)).toBe(expected);
});
```

**Avoid** (loop-generated):
```csharp
var cases = new[] { ("valid", true), ("invalid", false) };
foreach (var (name, expected) in cases)
{
    it($"case: {name}", () => { });  // Cannot discover statically
}
```

See [DSL Reference - Data-Driven Tests](dsl-reference.md#data-driven-tests) for more examples.

## 3. Keep Specs in describe Blocks

Specs should be nested within `describe` blocks to provide context and proper organization.

**Good**:
```csharp
describe("UserService", () => {
    describe("CreateAsync", () => {
        it("creates a user", () => { });
        it("validates input", () => { });
    });
});
```

**Avoid**:
```csharp
// Orphaned spec - outside describe block
it("orphaned test", () => { });  // No context path
```

## 4. Use xit Instead of Conditionals

Use `xit` to skip specs explicitly rather than conditionally defining them.

**Good**:
```csharp
xit("handles edge case - needs database", () => { });  // Explicitly skipped

// Or use tags for conditional running
it("slow integration test", () => { }).tags("slow", "integration");
```

**Avoid**:
```csharp
if (Environment.GetEnvironmentVariable("RUN_SLOW_TESTS") != null)
{
    it("slow test", () => { });  // May not be discovered
}
```

## 5. Limit Nesting Depth

Deep nesting makes specs hard to read and navigate. Aim for 3 levels maximum.

**Good** (3 levels):
```csharp
describe("UserService", () => {
    describe("CreateAsync", () => {
        it("creates a user with valid data", () => { });
        it("throws when email is invalid", () => { });
    });
});
```

**Avoid** (too deep):
```csharp
describe("A", () => {
    describe("B", () => {
        describe("C", () => {
            describe("D", () => {
                describe("E", () => {
                    it("deeply nested", () => { });  // Hard to navigate
                });
            });
        });
    });
});
```

If you need more organization, consider splitting into multiple spec files.

## 6. Use Descriptive Names

Write descriptions that read like sentences and clearly explain the expected behavior.

**Good**:
```csharp
describe("ShoppingCart", () => {
    it("calculates total with tax", () => { });
    it("applies discount codes", () => { });
    it("prevents negative quantities", () => { });
});
```

**Avoid**:
```csharp
describe("ShoppingCart", () => {
    it("test1", () => { });
    it("works", () => { });
    it("bug fix", () => { });
});
```

## 7. One Assertion Per Spec (When Practical)

Each spec should test one specific behavior. This makes failures easier to diagnose.

**Good**:
```csharp
describe("User.FullName", () => {
    it("combines first and last name", () => {
        var user = new User("John", "Doe");
        expect(user.FullName).toBe("John Doe");
    });

    it("trims whitespace", () => {
        var user = new User("  John  ", "  Doe  ");
        expect(user.FullName).toBe("John Doe");
    });
});
```

**Avoid** (multiple unrelated assertions):
```csharp
it("user works", () => {
    var user = new User("John", "Doe");
    expect(user.FullName).toBe("John Doe");
    expect(user.Email).toBeNull();
    expect(user.IsActive).toBeFalse();
    expect(user.CreatedAt).toNotBeNull();
});
```

## 8. Use Hooks Appropriately

Place setup and teardown code in the appropriate hooks.

**Good**:
```csharp
describe("Database tests", () => {
    beforeAll(() => {
        // One-time setup: create schema
        db.Migrate();
    });

    afterAll(() => {
        // One-time cleanup: drop schema
        db.Drop();
    });

    before(() => {
        // Per-spec setup: clean tables
        db.Truncate();
    });

    it("inserts records", () => { });
    it("queries records", () => { });
});
```

**Avoid** (setup in each spec):
```csharp
describe("Database tests", () => {
    it("inserts records", () => {
        db.Migrate();  // Repeated setup
        db.Truncate();
        // actual test
    });

    it("queries records", () => {
        db.Migrate();  // Repeated setup
        db.Truncate();
        // actual test
    });
});
```

## 9. Keep Specs Independent

Each spec should be able to run in isolation without depending on other specs.

**Good**:
```csharp
describe("Counter", () => {
    it("starts at zero", () => {
        var counter = new Counter();
        expect(counter.Value).toBe(0);
    });

    it("increments", () => {
        var counter = new Counter();
        counter.Increment();
        expect(counter.Value).toBe(1);
    });
});
```

**Avoid** (specs depend on each other):
```csharp
Counter counter;

describe("Counter", () => {
    it("starts at zero", () => {
        counter = new Counter();
        expect(counter.Value).toBe(0);
    });

    it("increments", () => {
        counter.Increment();  // Depends on previous spec
        expect(counter.Value).toBe(1);
    });
});
```

## 10. Organize by Feature, Not by Method

Group specs by the feature or behavior being tested, not by class methods.

**Good**:
```csharp
describe("User registration", () => {
    it("creates account with valid email", () => { });
    it("sends welcome email", () => { });
    it("rejects duplicate email", () => { });
});

describe("User authentication", () => {
    it("logs in with correct password", () => { });
    it("rejects incorrect password", () => { });
    it("locks after 3 failed attempts", () => { });
});
```

**Avoid**:
```csharp
describe("UserService", () => {
    describe("CreateAsync", () => { /* ... */ });
    describe("ValidateAsync", () => { /* ... */ });
    describe("SendEmailAsync", () => { /* ... */ });
});
```

## Migration Tips

If you have existing specs that use dynamic patterns:

1. **Replace loops with withData**: Convert `foreach` loops generating specs into `withData` calls
2. **Extract variables to literals**: Replace interpolated strings with literal descriptions
3. **Remove conditionals**: Use `xit` for skipped specs or tags for conditional running
4. **Run `draftspec list`**: Verify all specs are discoverable after migration

## See Also

- [DSL Reference](dsl-reference.md) - Full API documentation
- [Static Parsing](static-parsing.md) - How static discovery works
- [Configuration](configuration.md) - Project configuration options
