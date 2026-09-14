using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Infrastructure.Persistence.Context;

namespace ERP.Core.Warehouse.Api.Test
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        #region Private Fields
        private PostgreSqlContainer? _container;

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

                
            }); 
        }

        //Metodo para inicializar el contenedor de PostgreSQL antes de ejecutar las pruebas
        public async Task InitializePostgreSqlContainer()
        {
            //Usamos la imagen oficial de PostgreSQL 18.3-alpine para crear un contenedor de prueba.
            _container = new PostgreSqlBuilder("postgres:18.3-alpine")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpassword")
                .Build();

            // Iniciamos el contenedor de PostgreSQL
            await _container.StartAsync();
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