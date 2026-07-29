using ERP.Core.Database.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ERP.Core.Warehouse.Api.Infrastructure.Services;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Infrastructure.Services;

using ERP.Core.Database.Infrastructure.Persistence.Repositories.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories.Warehouse;
using ERP.Core.Database.Infrastructure.Persistence.Repositories.Warehouse;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories.Catalogs;

namespace ERP.Core.Warehouse.Api.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // services.AddJobScheduling();
            services.AddErpDatabaseServices(configuration);
            
            services.AddScoped<IErrorManager, ErrorManager>();

            services.AddScoped<ITransportUnitRepository, TransportUnitRepository>();
            services.AddScoped<IRequestQuotedPurchasesRepository, RequestQuotedPurchasesRepository>();
            services.AddScoped<ICustomsDeclarationDetailsRepository, CustomsDeclarationDetailsRepository>();

            services.AddHttpClient<IScaleServices, ScaleServices>();

            return services;
        }
    }
}
