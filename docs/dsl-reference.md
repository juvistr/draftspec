# DSL Reference

Complete API reference for DraftSpec's describe/it/expect DSL.

## Import

```csharp
using static DraftSpec.Dsl;
```

This brings all DSL functions into scope: `describe`, `it`, `expect`, and hooks.

---

## Context Blocks

### describe(description, body)

Groups related specs. The first `describe` creates the root context; nested calls create child contexts.

```csharp
describe("Calculator", () =>
{
    describe("Add", () =>
    {
        it("sums numbers", () => { /* ... */ });
    });
});
```

### context(description, body)

Alias for `describe`. Use for sub-groupings when it reads better:

```csharp
describe("User", () =>
{
    context("when logged in", () =>
    {
        it("shows dashboard", () => { /* ... */ });
    });

    context("when logged out", () =>
    {
        it("redirects to login", () => { /* ... */ });
    });
});
```

---

## Specs

### it(description, body)

Defines a spec (test case). Supports both sync and async bodies.

**Sync:**
```csharp
it("adds numbers", () =>
{
    expect(1 + 1).toBe(2);
});
```

**Async:**
```csharp
it("fetches data", async () =>
{
    var result = await client.GetAsync("/api/users");
    expect(result.StatusCode).toBe(HttpStatusCode.OK);
});
```

### it(description)

Defines a **pending** spec (no implementation yet). Pending specs are reported but don't fail.

```csharp
it("handles edge case");  // Pending - reminds you to implement later
```

### fit(description, body)

**Focused** spec. When any `fit` exists, only focused specs run. Non-focused specs are skipped.

```csharp
fit("only this runs", () =>
{
    expect(true).toBeTrue();
});

it("this is skipped", () =>
{
    // Won't execute when fit exists
});
```

Supports both sync and async bodies.

### xit(description, body?)

**Skipped** spec. Explicitly disabled; reported as skipped.

```csharp
xit("temporarily disabled", () =>
{
    // Won't execute
});

xit("also skipped");  // Body is optional
```

---

## Hooks

Hooks run setup/teardown code around specs. All hooks support both sync and async.

### before(hook)

Runs **before each spec** in the current context and nested contexts.

```csharp
describe("Database", () =>
{
    before(() => { db.BeginTransaction(); });
    before(async () => { await db.BeginTransactionAsync(); });  // async

    it("inserts record", () => { /* ... */ });
    it("updates record", () => { /* ... */ });
});
```

### after(hook)

Runs **after each spec** in the current context and nested contexts.

```csharp
describe("Database", () =>
{
    after(() => { db.RollbackTransaction(); });

    it("inserts record", () => { /* ... */ });
});
```

### beforeAll(hook)

Runs **once before all specs** in the current context.

```csharp
describe("Integration tests", () =>
{
    beforeAll(() => { server.Start(); });
    beforeAll(async () => { await server.StartAsync(); });

    it("connects", () => { /* ... */ });
    it("sends request", () => { /* ... */ });
});
```

### afterAll(hook)

Runs **once after all specs** in the current context.

```csharp
describe("Integration tests", () =>
{
    afterAll(() => { server.Stop(); });

    it("test 1", () => { /* ... */ });
    it("test 2", () => { /* ... */ });
});
```

### Hook Execution Order

For nested contexts, hooks execute in this order:

```
beforeAll (outer)
beforeAll (inner)
  before (outer) → before (inner) → spec → after (inner) → after (outer)
afterAll (inner)
afterAll (outer)
```

Example:
```csharp
describe("Outer", () =>
{
    beforeAll(() => Console.WriteLine("1. beforeAll outer"));
    before(() => Console.WriteLine("2. before outer"));
    after(() => Console.WriteLine("5. after outer"));
    afterAll(() => Console.WriteLine("7. afterAll outer"));

    describe("Inner", () =>
    {
        beforeAll(() => Console.WriteLine("   beforeAll inner"));
        before(() => Console.WriteLine("3. before inner"));
        after(() => Console.WriteLine("4. after inner"));
        afterAll(() => Console.WriteLine("6. afterAll inner"));

        it("spec", () => Console.WriteLine("   --- spec ---"));
    });
});
```

---

## Tags

Tags categorize specs for filtering.

### tag(name, body)

Apply a single tag to specs within the block:

```csharp
tag("slow", () =>
{
    it("takes a while", () => { /* ... */ });  // has tag "slow"
});
```

### tags(names, body)

Apply multiple tags to specs within the block:

```csharp
tags(["integration", "database"], () =>
{
    it("connects to database", () => { /* ... */ });  // has both tags
});
```

### Nested Tags

Tags accumulate when nested:

```csharp
tag("slow", () =>
{
    tag("integration", () =>
    {
        it("test", () => { });  // has both "slow" and "integration"
    });
});
```

### Filtering by Tag

Use CLI flags to filter specs by tag:

```bash
# Run only specs with "fast" tag
draftspec run . --tags fast

# Run specs with either "unit" or "fast" tag
draftspec run . --tags unit,fast

# Exclude specs with "slow" tag
draftspec run . --exclude-tags slow
```

---

## Table-Driven Tests

Generate specs from data collections using `withData`. Ideal for testing multiple inputs with similar behavior.

### withData(data, specFactory)

Iterate over a collection and call a factory function for each item:

```csharp
describe("String validation", () =>
{
    withData([
        new { input = "hello", expected = 5 },
        new { input = "world", expected = 5 },
        new { input = "", expected = 0 }
    ], data =>
    {
        it($"'{data.input}' has length {data.expected}", () =>
        {
            expect(data.input.Length).toBe(data.expected);
        });
    });
});
```

### Tuple Destructuring

For tuples, parameters are automatically destructured:

**2-tuples:**
```csharp
withData([
    ("hello", 5),
    ("world", 5),
    ("", 0)
], (input, expected) =>
{
    it($"'{input}' has length {expected}", () =>
    {
        expect(input.Length).toBe(expected);
    });
});
```

**3-tuples:**
```csharp
withData([
    (1, 1, 2),
    (2, 3, 5),
    (-1, 1, 0)
], (a, b, expected) =>
{
    it($"{a} + {b} = {expected}", () =>
    {
        expect(a + b).toBe(expected);
    });
});
```

Supports up to 6-tuples: `(T1, T2)` through `(T1, T2, T3, T4, T5, T6)`.

### Named Test Cases (Dictionary)

Use a dictionary to provide explicit test case names:

```csharp
withData(new Dictionary<string, (int, int, int)>
{
    ["positive numbers"] = (1, 2, 3),
    ["with zero"] = (0, 5, 5),
    ["negative result"] = (1, -5, -4)
}, (name, data) =>
{
    it(name, () =>
    {
        var (a, b, expected) = data;
        expect(a + b).toBe(expected);
    });
});
```

### Combining with Contexts

`withData` works naturally with nested contexts:

```csharp
describe("Calculator", () =>
{
    describe("Add", () =>
    {
        withData([
            (1, 1, 2),
            (0, 0, 0)
        ], (a, b, expected) =>
        {
            it($"{a} + {b} = {expected}", () =>
            {
                expect(calculator.Add(a, b)).toBe(expected);
            });
        });
    });

    describe("Subtract", () =>
    {
        withData([
            (5, 3, 2),
            (0, 0, 0)
        ], (a, b, expected) =>
        {
            it($"{a} - {b} = {expected}", () =>
            {
                expect(calculator.Subtract(a, b)).toBe(expected);
            });
        });
    });
});
```

---

## Complete Example

```csharp
#r "nuget: DraftSpec"
using static DraftSpec.Dsl;

var db = new TestDatabase();

describe("UserRepository", () =>
{
    beforeAll(() => db.Connect());
    afterAll(() => db.Disconnect());

    before(() => db.BeginTransaction());
    after(() => db.Rollback());

    describe("Create", () =>
    {
        it("inserts a user", () =>
        {
            var user = new User("test@example.com");
            db.Users.Add(user);
            expect(user.Id).toNotBeNull();
        });

        it("validates email", () =>
        {
            expect(() => new User("invalid"))
                .toThrow<ArgumentException>();
        });

        tag("slow", () =>
        {
            it("handles bulk insert", async () =>
            {
                var users = Enumerable.Range(0, 1000)
                    .Select(i => new User($"user{i}@test.com"));
                await db.Users.AddRangeAsync(users);
                expect(db.Users.Count()).toBe(1000);
            });
        });
    });

    describe("Delete", () =>
    {
        it("removes user");  // pending

        xit("cascades to orders", () =>
        {
            // skipped for now
        });
    });
});
```

Run with: `draftspec run UserRepository.spec.csx`

Filter slow tests: `draftspec run . --exclude-tags slow`

---

## See Also

- **[Assertions](assertions.md)** - Full `expect()` API
- **[CLI Reference](cli.md)** - Command-line options
- **[Configuration](configuration.md)** - Plugins and middleware
