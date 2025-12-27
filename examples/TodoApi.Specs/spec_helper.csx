// spec_helper.csx - Shared setup for MTP-based specs
// This file is loaded by other spec files using #load directive

#r "nuget: DraftSpec, *"
#r "../../examples/TodoApi/bin/Debug/net10.0/TodoApi.dll"

using static DraftSpec.Dsl;
using DraftSpec;
using TodoApi.Models;
using TodoApi.Services;

// ============================================
// Shared Fixtures
// ============================================

InMemoryTodoRepository CreateRepository() => new();

TodoService CreateService(ITodoRepository? repo = null)
    => new(repo ?? CreateRepository());
