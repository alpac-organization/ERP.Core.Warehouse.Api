using System.Text.Json;
using Microsoft.OpenApi;
using ERP.Core.Warehouse.Api.Application;
using System.Text.Json.Serialization;
using ERP.Core.Infrastructure.Middlewares;
using ERP.Core.Warehouse.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var root = builder.Environment.ContentRootPath;
var envPath = Path.Combine(root, "..", "..", ".env");

if (File.Exists(envPath)) DotNetEnv.Env.Load(envPath);
else DotNetEnv.Env.Load();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApplicationServices();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP.Core.Warehouse.Api",
        Version = "v1",
        Description = "Dominio de compras y almacenes..."
    });
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")  
    .Get<string[]>();

builder.Services.AddCors(Options =>
{
    Options.AddPolicy("ViteLocalPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins ?? [])
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());        
    });

builder.Logging.AddFilter("LuckyPennySoftware.MediatE.License", LogLevel.None);

var app = builder.Build();

//Casos de uso, (Middlewares, Cors..., etc)
app.UseRouting();

app.UseMiddleware<ERP.Core.Warehouse.Api.Middlewares.RequestLoggingMiddleware>();

app.UseCors("ViteLocalPolicy");

//Middlewares..
app.UseMiddleware<ExceptionMiddleware>();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseMiddleware<AuthMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
       options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
       options.RoutePrefix = "swagger/docs"; 
    });
}

app.UseSwagger();
app.MapControllers();

app.Run();
