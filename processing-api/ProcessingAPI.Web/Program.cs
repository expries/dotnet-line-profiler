using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using ProcessingAPI.Services;
using ProcessingAPI.Services.Models;
using ProcessingAPI.Web.Routes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContextPool<MethodCallContext>(configure =>
{
    var dbConnectionString = builder.Configuration.GetConnectionString("Database");
    configure.UseNpgsql(dbConnectionString);
    var dbContext = new MethodCallContext(configure.Options);
    dbContext.Database.EnsureCreated();
});
    
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});


builder.Services.AddTransient<MethodCallService>();
builder.Services.AddTransient<MethodService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app
    .AddOtelRoutes()
    .AddGrafanaRoutes();

app.Run();
