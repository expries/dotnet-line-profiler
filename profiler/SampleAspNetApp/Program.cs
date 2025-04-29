using SampleAspNetApp;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () =>
{
    var customerCount = CustomerService.GetNumberOfCustomers();
    var valid = customerCount > 0;
    return "Customers: " + customerCount + ", valid: " + valid;
});

app.Run();