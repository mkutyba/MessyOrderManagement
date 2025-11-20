var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.IncludeFields = true;
    });

var app = builder.Build();

app.MapControllers();

app.Run();

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program { }

