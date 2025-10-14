using Microsoft.EntityFrameworkCore;
using ProjectScheduler.Data;
using ProjectScheduler.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection String: {connectionString}");

// Register both DbContext classes (there are two with same name in different namespaces)
builder.Services.AddDbContext<ProjectScheduler.Data.ProjectSchedulerDbContext>(options =>
{
    Console.WriteLine("Registering ProjectScheduler.Data.ProjectSchedulerDbContext");
    options.UseSqlServer(connectionString);
});

builder.Services.AddDbContext<ProjectScheduler.ProjectSchedulerDbContext>(options =>
{
    Console.WriteLine("Registering ProjectScheduler.ProjectSchedulerDbContext");
    options.UseSqlServer(connectionString);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services
builder.Services.AddScoped<ICapacityService, CapacityService>();
builder.Services.AddScoped<IAllocationService, AllocationService>();
builder.Services.AddScoped<IScheduleSuggestionService, ScheduleSuggestionService>();
builder.Services.AddScoped<ISquadRecommendationService, SquadRecommendationService>();
builder.Services.AddScoped<IAiQueryService, AiQueryService>();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Verify DbContext is registered
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetService<ProjectSchedulerDbContext>();
    Console.WriteLine($"DbContext resolved: {context != null}");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disabled for local development
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
