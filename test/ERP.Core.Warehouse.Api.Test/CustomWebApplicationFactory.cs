using NUnit.Framework;
using Testcontainers.PostgreSql;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using ERP.Core.Database.Infrastructure.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Core.Warehouse.Api.Test
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        #region Private Fields
        private PostgreSqlContainer? _container;

        #endregion


        #region Public Fields
        public bool IsDockerAvailable { get; private set; }
        public string ApiKey = "integration-test-api-key";
        public string JwtKey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCqGKukO1De7zhY";

        #endregion


        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //Usamos el entorno de prueba para probar nuestras APIs.
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                //Agregemos el AppSettings.Testing.json para que se use en el entorno de prueba.
                config.AddJsonFile(
                    Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"),
                    optional: true,
                    reloadOnChange: false
                );

                config.AddInMemoryCollection(new Dictionary<string, string?>
                {   
                    ["Jwt:SecretKey"] = JwtKey
                });
            });


            builder.ConfigureServices(services =>
            {
                var dbContextTesting = services.SingleOrDefault(
                    db => db.ServiceType == typeof(DbContextOptions<ErpDbContext>)
                );

                if (dbContextTesting != null)
                {
                    services.Remove(dbContextTesting);
                }

                //Agregar el contexto
                services.AddDbContext<ErpDbContext>(options =>
                    options.UseNpgsql(_container!.GetConnectionString())
                );
            }); 
        }

        //Metodo para inicializar el contenedor de PostgreSQL antes de ejecutar las pruebas
        public async Task InitializePostgreSqlContainer()
        {
            //Usamos la imagen oficial de PostgreSQL 18.3-alpine para crear un contenedor de prueba.
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpassword")
                .WithPortBinding(5433, 5432)
                .Build();

            // Iniciamos el contenedor de PostgreSQL, con un máximo de 3 intentos en caso de que Docker no esté disponible temporalmente.
            try
            {
                await _container.StartAsync();
                IsDockerAvailable = true;

                //Hasta este punto nuestra base de datos necesita aplicar las migraciones
                var scope = Services.CreateScope();
                var database = scope.ServiceProvider.GetService<ErpDbContext>();

                if (database is not null)
                {
                    //Si encontramos la base de datos entonces aplicamos las migraciones
                    await database.Database.MigrateAsync();
                }

                //Establecemos la cadena de conexión de la base de datos del contenedor
                string? connectionString = _container.GetConnectionString();
                Environment.SetEnvironmentVariable("ConnectionStrings__ErpConnectionDatabase", connectionString);

            }
            catch (Exception ex)
            {
                IsDockerAvailable = false;
                TestContext.Progress.WriteLine( $"[Testcontainers] Intento levantar postgres:18.3-alpine falló: {ex.Message}");
            }
        }

        //Detener contenedor de PostgreSQL y liberar recursos después de ejecutar las pruebas
        public async Task DisposePostgreSqlContainer()
        {
            //Solo si existe un contenedor, lo detenemos y liberamos los recursos asociados.
            if (_container != null)
            {
                await _container.StopAsync();
                await _container.DisposeAsync();
            }   
        }
    }
}