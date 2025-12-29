#load "../spec_helper.csx"
using static DraftSpec.Dsl;
using TodoApi.Models;
using TodoApi.Services;

// ============================================================================
// DraftSpec Features Showcase
// ============================================================================
// This file systematically demonstrates ALL DraftSpec features.
// Use it as a reference for the framework's capabilities.
// ============================================================================

// ============================================
// SECTION 1: Context Nesting (describe/context)
// ============================================

describe("1. Context Nesting", () =>
{
    describe("describe blocks", () =>
    {
        it("creates a context for grouping specs", () =>
        {
            expect(true).toBeTrue();
        });

        describe("can be nested", () =>
        {
            describe("to any depth", () =>
            {
                it("for organizing related specs", () =>
                {
                    expect(1 + 1).toBe(2);
                });
            });
        });
    });

    context("context blocks", () =>
    {
        it("are an alias for describe", () =>
        {
            expect("context").toContain("context");
        });

        context("and can also be nested", () =>
        {
            it("for describing different scenarios", () =>
            {
                expect(true).toBeTrue();
            });
        });
    });
});

// ============================================
// SECTION 2: Spec Types (it/fit/xit/pending)
// ============================================

describe("2. Spec Types", () =>
{
    it("regular specs run normally", () =>
    {
        expect(42).toBe(42);
    });

    // Uncomment to see focus mode in action:
    // fit("focused specs - ONLY these run when any exist", () =>
    // {
    //     expect(true).toBeTrue();
    // });

    xit("skipped specs are explicitly disabled", () =>
    {
        // This body never executes
        throw new Exception("Should not run");
    });

    it("pending specs have no body - they remind you to implement later");
});

// ============================================
// SECTION 3: All Assertion Types
// ============================================

describe("3. Assertions", () =>
{
    describe("toBe - equality", () =>
    {
        it("compares primitive values", () =>
        {
            expect(1 + 1).toBe(2);
            expect("hello").toBe("hello");
        });

        it("compares objects by value equality", () =>
        {
            var user1 = new User(1, "Alice", "alice@test.com");
            var user2 = new User(1, "Alice", "alice@test.com");
            expect(user1).toBe(user2);
        });
    });

    describe("toBeNull / toNotBeNull", () =>
    {
        it("checks for null", () =>
        {
            string nullString = null;
            expect(nullString).toBeNull();
        });

        it("checks for not null", () =>
        {
            object value = "value";
            expect(value).toNotBeNull();
        });
    });

    describe("Numeric Comparisons", () =>
    {
        it("toBeGreaterThan", () =>
        {
            expect(10).toBeGreaterThan(5);
        });

        it("toBeLessThan", () =>
        {
            expect(5).toBeLessThan(10);
        });

        it("toBeAtLeast (>=)", () =>
        {
            expect(5).toBeAtLeast(5);
            expect(6).toBeAtLeast(5);
        });

        it("toBeAtMost (<=)", () =>
        {
            expect(5).toBeAtMost(5);
            expect(4).toBeAtMost(5);
        });

        it("toBeInRange (inclusive)", () =>
        {
            expect(5).toBeInRange(1, 10);
            expect(1).toBeInRange(1, 10);
            expect(10).toBeInRange(1, 10);
        });

        it("toBeCloseTo - for floating point", () =>
        {
            expect(0.1 + 0.2).toBeCloseTo(0.3, 0.0001);
            expect(Math.PI).toBeCloseTo(3.14159, 0.00001);
        });
    });

    describe("Boolean Assertions", () =>
    {
        it("toBeTrue", () =>
        {
            expect(1 == 1).toBeTrue();
            expect("hello".StartsWith("h")).toBeTrue();
        });

        it("toBeFalse", () =>
        {
            expect(1 == 2).toBeFalse();
            expect(string.IsNullOrEmpty("hello")).toBeFalse();
        });
    });

    describe("String Assertions", () =>
    {
        var greeting = "Hello, World!";

        it("toContain - substring", () =>
        {
            expect(greeting).toContain("World");
        });

        it("toStartWith - prefix", () =>
        {
            expect(greeting).toStartWith("Hello");
        });

        it("toEndWith - suffix", () =>
        {
            expect(greeting).toEndWith("!");
        });

        it("toBeNullOrEmpty", () =>
        {
            expect("").toBeNullOrEmpty();
            string nullStr = null;
            expect(nullStr).toBeNullOrEmpty();
        });
    });

    describe("Collection Assertions", () =>
    {
        var items = new[] { "apple", "banana", "cherry" };

        it("toContain - single item", () =>
        {
            expect(items).toContain("banana");
        });

        it("toNotContain", () =>
        {
            expect(items).toNotContain("grape");
        });

        it("toContainAll - multiple items", () =>
        {
            expect(items).toContainAll("apple", "cherry");
        });

        it("toHaveCount", () =>
        {
            expect(items).toHaveCount(3);
        });

        it("toBeEmpty", () =>
        {
            expect(Array.Empty<int>()).toBeEmpty();
        });

        it("toNotBeEmpty", () =>
        {
            expect(items).toNotBeEmpty();
        });

        it("toBe - sequence equality", () =>
        {
            expect(items).toBe("apple", "banana", "cherry");
        });
    });

    describe("Exception Assertions", () =>
    {
        it("toThrow<T> - specific exception type", () =>
        {
            expect(() => int.Parse("not a number"))
                .toThrow<FormatException>();
        });

        it("toThrow<T> - returns exception for inspection", () =>
        {
            Action throwAction = () => throw new InvalidOperationException("test error");
            var ex = expect(throwAction).toThrow<InvalidOperationException>();

            expect(ex.Message).toBe("test error");
        });

        it("toThrow - any exception", () =>
        {
            Action throwAction = () => throw new Exception("something went wrong");
            var ex = expect(throwAction).toThrow();

            expect(ex.Message).toContain("wrong");
        });

        it("toNotThrow - no exception", () =>
        {
            expect(() =>
            {
                var x = 1 + 1;
            }).toNotThrow();
        });
    });
});

// ============================================
// SECTION 4: Hooks
// ============================================

describe("4. Hooks", () =>
{
    var log = new List<string>();

    beforeAll(() =>
    {
        log.Add("beforeAll - runs once before all specs");
    });

    afterAll(() =>
    {
        log.Add("afterAll - runs once after all specs");
        // Console.WriteLine($"Hook log: {string.Join(" -> ", log)}");
    });

    before(() =>
    {
        log.Add("before");
    });

    after(() =>
    {
        log.Add("after");
    });

    it("first spec", () =>
    {
        log.Add("spec1");
        expect(log).toContain("beforeAll - runs once before all specs");
        expect(log).toContain("before");
    });

    it("second spec", () =>
    {
        log.Add("spec2");
        // Before runs again for each spec
        expect(log.Count(x => x == "before")).toBeGreaterThan(1);
    });
});

// ============================================
// SECTION 5: Async Support
// ============================================

describe("5. Async Support", () =>
{
    it("supports async specs", async () =>
    {
        await Task.Delay(10);
        expect(true).toBeTrue();
    });

    it("can await async operations", async () =>
    {
        var repo = CreateRepository();
        var service = CreateService(repo);

        var todo = await service.CreateAsync("Async task", Priority.High);

        expect(todo.Title).toBe("Async task");
        expect(todo.Priority).toBe(Priority.High);
    });
});

// ============================================
// SECTION 6: Tags
// ============================================

describe("6. Tags", () =>
{
    tag("fast", () =>
    {
        it("has the 'fast' tag", () =>
        {
            expect(1 + 1).toBe(2);
        });
    });

    tags(["integration", "slow"], () =>
    {
        it("has multiple tags", () =>
        {
            expect(true).toBeTrue();
        });

        tag("database", () =>
        {
            it("tags accumulate when nested", () =>
            {
                // This spec has: integration, slow, database
                expect(true).toBeTrue();
            });
        });
    });
});

// ============================================
// SECTION 7: Custom Matchers (Extension Methods)
// ============================================

describe("7. Domain-Specific Assertions", () =>
{
    // Custom matchers work via extension methods on Expectation<T>
    // In CSX scripts, define them as static methods in a separate assembly
    // Here we demonstrate the patterns using built-in assertions

    it("checking if todo is overdue", () =>
    {
        var overdueTodo = new Todo
        {
            Id = 1,
            Title = "Overdue task",
            DueDate = DateTime.UtcNow.AddDays(-1),
            IsComplete = false
        };

        // Pattern: check due date is in the past and not complete
        expect(overdueTodo.DueDate).toNotBeNull();
        expect(overdueTodo.DueDate < DateTime.UtcNow).toBeTrue();
        expect(overdueTodo.IsComplete).toBeFalse();
    });

    it("checking for specific tag", () =>
    {
        var taggedTodo = new Todo
        {
            Id = 1,
            Title = "Tagged task",
            Tags = ["urgent", "work", "review"]
        };

        expect(taggedTodo.Tags).toContain("urgent");
        expect(taggedTodo.Tags.Contains("WORK", StringComparer.OrdinalIgnoreCase)).toBeTrue();
    });

    it("checking priority level", () =>
    {
        var criticalTodo = new Todo
        {
            Id = 1,
            Title = "Critical task",
            Priority = Priority.Critical
        };

        expect(criticalTodo.Priority).toBe(Priority.Critical);
    });

    it("checking user assignment", () =>
    {
        var assignedTodo = new Todo
        {
            Id = 1,
            Title = "Assigned task",
            AssignedUserId = 42
        };

        expect(assignedTodo.AssignedUserId).toBe(42);
    });
});

// ============================================
// SECTION 8: Configuration (commented examples)
// ============================================

// Configuration is typically done once at the top of a spec file or in spec_helper.csx
// Uncomment to see these in action:

// configure(runner => runner
//     .WithTimeout(5000)        // 5 second timeout per spec
//     .WithRetry(2)             // Retry failed specs up to 2 times
//     .WithTagFilter("fast")    // Only run specs with "fast" tag
//     .WithoutTags("slow")      // Exclude specs with "slow" tag
//     .WithParallelExecution()  // Run specs in parallel
// );

