#r "../../src/DraftSpec/bin/Debug/net10.0/DraftSpec.dll"
#r "../../src/DraftSpec.Formatters.Abstractions/bin/Debug/net10.0/DraftSpec.Formatters.Abstractions.dll"
#r "../../src/DraftSpec.Formatters.Console/bin/Debug/net10.0/DraftSpec.Formatters.Console.dll"
#r "bin/Debug/net10.0/TodoApi.dll"

using static DraftSpec.Dsl;
using DraftSpec;
using TodoApi.Models;
using TodoApi.Services;

// ============================================
// Shared Fixtures
// ============================================

InMemoryTodoRepository CreateRepository() => new();

TodoService CreateService(ITodoRepository repo = null)
    => new(repo ?? CreateRepository());
