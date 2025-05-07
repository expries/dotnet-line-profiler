using Microsoft.EntityFrameworkCore;
using ProcessingAPI.Services.Models;

namespace ProcessingAPI.Services;

public class MethodCallService(MethodCallContext context)
{
    private MethodCallContext Context { get; } = context;
    
    public async Task<int> CountTotalTracesAsync()
    {
        return await Context.Spans.Select(span => span.TraceId).Distinct().CountAsync();
    }
    
    public async Task<int> CountMethodCallsDistinctByTraceIdByMethodIdAsync(string methodId)
    {
        return await Context.MethodCalls
            .Where(call => call.Method.Id.Equals(methodId))
            .Select(call => call.Span.TraceId)
            .Distinct()
            .CountAsync();
    }
    
    public async Task<List<MethodCall>> GetMethodCallsAsync()
    {
        return await Context.MethodCalls
            .Include(call => call.Method)
            .Include(call => call.Span)
            .ToListAsync();
    }
    
    public async Task<MethodCall?> GetMethodCallByIdAsync(string callId)
    {
        return await Context.MethodCalls
            .Include(m => m.Method)
            .Include(m => m.Branches)
            .FirstOrDefaultAsync(call => call.Id == callId);
    }
    
    public async Task ProcessMethodCallAsync(List<MethodCall> methodCalls)
    {
        foreach (var call in methodCalls)
        {
            call.Method = await CreateOrUpdateMethodAsync(call.Method);
            var branches = new List<Branch>();
            
            foreach (var callBranch in call.Branches)
            {
                callBranch.Method = call.Method;
                var branch = await CreateOrUpdateBranchAsync(callBranch);
                branches.Add(branch);
            }
            
            call.Branches = branches;
            Context.MethodCalls.Add(call);
        }
        
        await Context.SaveChangesAsync();
    }

    private async Task<Branch> CreateOrUpdateBranchAsync(Branch branch)
    {
        var storedBranch = await Context.Branches.FirstOrDefaultAsync(stored => 
            stored.BlockId.Equals(branch.BlockId) && stored.Method.Id == branch.Method.Id);

        // Branch not stored: add it
        if (storedBranch is null)
        {
            var newBranch = await Context.Branches.AddAsync(branch);
            return newBranch.Entity;
        }
        
        // Branch stored: Update and increment executions
        storedBranch.Executions += branch.Executions;
        storedBranch.LastTested = branch.LastTested ?? storedBranch.LastTested;
        storedBranch.CodeLines = branch.CodeLines;
        return storedBranch;
    }
    
    private async Task<Method> CreateOrUpdateMethodAsync(Method calledMethod)
    {
        var storedMethod = await Context.Methods.FirstOrDefaultAsync(method => 
            method.FullTypeName.ToLower().Equals(calledMethod.FullTypeName.ToLower()) && 
            method.Name.ToLower().Equals(calledMethod.Name.ToLower()));
        
        // Method not stored: add it
        if (storedMethod is null)
        {
            calledMethod.Revision = 1;
            var newMethod = await Context.Methods.AddAsync(calledMethod);
            return newMethod.Entity;
        }
        
        // Method is stored, but outdated: update revision
        if (calledMethod.SourceCode.Length != storedMethod.SourceCode.Length || 
            calledMethod.CodeStartLine != storedMethod.CodeStartLine || 
            calledMethod.CodeEndLine != storedMethod.CodeEndLine)
        {
            calledMethod.Revision = storedMethod.Revision + 1;
            var newMethod = await Context.Methods.AddAsync(calledMethod);
            return newMethod.Entity;  
        }
        
        // Method is stored and correct: reuse it
        storedMethod.SourceCode = calledMethod.SourceCode;
        storedMethod.CodeStartLine = calledMethod.CodeStartLine;
        storedMethod.CodeEndLine = calledMethod.CodeEndLine;
        return storedMethod;
    }
}