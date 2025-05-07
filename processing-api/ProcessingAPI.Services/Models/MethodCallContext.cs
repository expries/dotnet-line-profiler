using Microsoft.EntityFrameworkCore;

namespace ProcessingAPI.Services.Models;

public class MethodCallContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Span> Spans { get; set; }
    public DbSet<MethodCall> MethodCalls { get; set; }
    public DbSet<Method> Methods { get; set; }
    public DbSet<Branch> Branches { get; set; }
}