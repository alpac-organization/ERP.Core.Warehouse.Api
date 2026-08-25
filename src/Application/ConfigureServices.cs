using MediatR;
using FluentValidation;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ERP.Core.Application.Behaviors;
using Microsoft.Extensions.Options;


namespace ERP.Core.Warehouse.Api.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var currentAssembly = Assembly.GetExecutingAssembly();

            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(currentAssembly);
            });

            services.AddValidatorsFromAssembly(currentAssembly);
             
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(currentAssembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            return services;
        }
    }

}