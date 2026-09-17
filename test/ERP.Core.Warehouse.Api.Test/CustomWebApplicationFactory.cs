using Respawn;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;

using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ERP.Core.Database.Infrastructure.Persistence.Context;
using ERP.Core.Warehouse.Api.Test.Common.Utils;
using ERP.Core.Testing.Seeding;

namespace ERP.Core.Warehouse.Api.Test
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        #region Private Fields
        private PostgreSqlContainer? _container;

        #endregion

        #region Public Fields
        public bool IsDockerAvailable { get; private set; }

        #endregion

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //Usamos el entorno de prueba para probar nuestras APIs. y configuramos todos los enviorement Mokiados
            builder.UseEnvironment("Testing");

            EnvironmentManager.ApplyEnvironmentAws();
            EnvironmentManager.ApplyEnvironmentCorsAndSecurity();

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
                    ["Jwt:SecretKey"] = EnvironmentManager.JwtKey
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

        //Metodo para reiniciar la base de datos cada que se levante el contenedor.
        public async Task ResetDatabase()
        {
            var scope = Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

            var connection = dbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                WithReseed = false
            });

            await respawner.ResetAsync(connection);
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


        //Metodo para sembrar los datos base (compañías, áreas, sucursales, usuarios, perfiles).
        public async Task SeedDatabase()
        {
            var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

            var data = ErpSeedDataFactory.CreateScenario();
            
            await ErpDatabaseSeeder.SeedAsync(dbContext,data);
        }
    }
}