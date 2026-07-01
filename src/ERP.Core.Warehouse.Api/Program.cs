using System.Text.Json;
using Microsoft.OpenApi;
using ERP.Core.Warehouse.Api.Application;
using ERP.Core.Manager.Api.Infrastructure;
using System.Text.Json.Serialization;
using ERP.Core.Manager.Api.Infrastructure.Middlewares;
using ERP.Core.Infrastructure.Middlewares;


var builder = WebApplication.CreateBuilder(args);

var root = builder.Environment.ContentRootPath;
var envPath = Path.Combine(root, "..", "..", ".env");

if (File.Exists(envPath)) DotNetEnv.Env.Load(envPath);
else DotNetEnv.Env.Load();

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")  
    .Get<string[]>();

builder.Services.AddCors(Options =>
{
    Options.AddPolicy("VitelocalPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins ?? [])
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());        
    });

builder.Logging.AddFilter("LuckyPennySoftware.MediatE.License", LogLevel.None);

//Configuración de la documentacion del swagger de las APIs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP.Core.Warehouse.Api",
        Version = "v1",
        Description = "Dominio para bodegas "
    });
});

var app = builder.Build();

// app.UseStaticFiles(new StaticFileOptions
// {
//     OnPrepareResponse = ctx =>
//     {
//         ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
//         ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, OPTIONS");
//         ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type");

//     }
// });

app.UseMiddleware<ExceptionMiddleware>();
app.UseRouting();

app.UseCors("ViteLocalPolicy");

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

app.UseHsts();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
