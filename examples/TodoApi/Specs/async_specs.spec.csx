#load "../spec_helper.csx"
using static DraftSpec.Dsl;
using TodoApi.Models;
using TodoApi.Services;

// ============================================================================
// Async Specs - Demonstrating Async/Await Patterns
// ============================================================================

describe("Async Patterns", () =>
{
    describe("Async Specs", () =>
    {
        it("can use async/await", async () =>
        {
            await Task.Delay(10);
            expect(true).toBeTrue();
        });

        it("can await multiple operations", async () =>
        {
            var task1 = Task.FromResult(1);
            var task2 = Task.FromResult(2);

            var results = await Task.WhenAll(task1, task2);

            expect(results).toBe(1, 2);
        });

        it("can test async service methods", async () =>
        {
            var service = CreateService();

            var todo = await service.CreateAsync("Async created", Priority.High);

            expect(todo.Title).toBe("Async created");
            expect(todo.Priority).toBe(Priority.High);
        });
    });

    describe("Async Hooks", () =>
    {
        User currentUser = null;
        List<Todo> seedData = [];

        beforeAll(async () =>
        {
            // Simulate async initialization (e.g., database setup)
            await Task.Delay(5);
            currentUser = new User(1, "Test User", "test@example.com");
        });

        before(async () =>
        {
            // Simulate async per-test setup
            await Task.Delay(1);
            seedData = [];
        });

        afterAll(async () =>
        {
            // Simulate async cleanup
            await Task.Delay(1);
            currentUser = null;
        });

        it("can use data initialized in async beforeAll", () =>
        {
            expect((object)currentUser).toNotBeNull();
            expect(currentUser.Name).toBe("Test User");
        });

        it("runs async before hook for each spec", () =>
        {
            expect(seedData).toBeEmpty();
            seedData.Add(new Todo { Id = 1, Title = "Added in spec" });
        });

        it("async before hook resets state", () =>
        {
            // Previous spec's addition should not be visible
            // because async before() runs fresh for each spec
            expect(seedData).toBeEmpty();
        });
    });

    describe("Async Exception Testing", () =>
    {
        it("can test async methods that throw", () =>
        {
            var service = CreateService();

            // For async exceptions, need to unwrap
            expect(() => service.CreateAsync("").GetAwaiter().GetResult())
                .toThrow<ValidationException>();
        });

        it("can test async success with awaited result", async () =>
        {
            var service = CreateService();

            // This should not throw
            var todo = await service.CreateAsync("Valid title");
            expect(todo).toNotBeNull();
        });
    });

    describe("Parallel Async Operations", () =>
    {
        it("can await parallel tasks", async () =>
        {
            var service = CreateService();

            var tasks = new[]
            {
                service.CreateAsync("Task 1", Priority.Low),
                service.CreateAsync("Task 2", Priority.Medium),
                service.CreateAsync("Task 3", Priority.High)
            };

            var todos = await Task.WhenAll(tasks);

            expect(todos.Length).toBe(3);
            expect(todos.Select(t => t.Priority).Distinct().Count()).toBe(3);
        });
    });
});

run();
