using MediatR;
using FluentValidation;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ERP.Core.Application.Behaviors;


namespace ERP.Core.Warehouse.Api.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var currentAssembly = Assembly.GetExecutingAssembly();

            // services.AddAutoMapper(typeof(ConfigureServices).Assembly); //<-- version antigua y maliciosa
            

            //Se usa ecpresion lambda para buscar los perfiles de mapeo
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(currentAssembly);
            });

            // services.AddValidatorsFromAssembly(typeof(ConfigureServices).Assembly);
            services.AddValidatorsFromAssembly(currentAssembly); //<-- Se aprovecha la misma variable
             
            services.AddMediatR(cfg => {
//                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.RegisterServicesFromAssembly(currentAssembly); //<-- Y aqui
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            return services;
        }
    }

}