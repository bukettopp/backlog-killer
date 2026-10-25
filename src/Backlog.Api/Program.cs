using System.Text.Json.Serialization;
using Backlog.Api.Data;
using Backlog.Api.Endpoints;
using Backlog.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Enum'lar JSON'da sayı yerine string olarak yazılır/okunur ("Game", "Backlog").
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("BacklogDb")
    ?? throw new InvalidOperationException("Connection string 'BacklogDb' bulunamadı.");

builder.Services.AddDbContext<BacklogDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

// DbContext scoped olduğu için servis de scoped kaydedilir.
builder.Services.AddScoped<SuggestionService>();
builder.Services.AddScoped<StaleItemService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // http://localhost:5080/scalar

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BacklogDbContext>();
    await DevelopmentSeeder.SeedAsync(db);
}

app.MapItemEndpoints();
app.MapSuggestionEndpoints();

app.Run();
