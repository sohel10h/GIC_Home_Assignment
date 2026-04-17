var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    message = "Order Processing API bootstrap"
}));

app.MapControllers();

app.Run();
