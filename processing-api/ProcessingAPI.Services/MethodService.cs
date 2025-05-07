using Microsoft.EntityFrameworkCore;
using ProcessingAPI.Services.Models;

namespace ProcessingAPI.Services;

public class MethodService(MethodCallContext context)
{
    private MethodCallContext Context { get; } = context;

    public async Task<List<Method>> GetAllMethodsAsync()
    {
        return await Context.Methods
            .Include(m => m.Calls)
            .ToListAsync();
    }
    
    public async Task<Method?> GetMethodByIdAsync(string id)
    {
        return await Context.Methods
            .Include(m => m.Calls)
            .Include(m => m.Branches)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}