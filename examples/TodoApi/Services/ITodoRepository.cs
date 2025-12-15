using TodoApi.Models;

namespace TodoApi.Services;

public interface ITodoRepository
{
    Task<Todo> AddAsync(Todo todo);
    Task<Todo?> GetByIdAsync(int id);
    Task<IReadOnlyList<Todo>> GetAllAsync();
    Task<Todo> UpdateAsync(Todo todo);
    Task DeleteAsync(int id);
    Task<int> GetNextIdAsync();
}
