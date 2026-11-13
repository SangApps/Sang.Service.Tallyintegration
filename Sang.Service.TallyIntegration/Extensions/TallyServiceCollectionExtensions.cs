using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sang.Service.TallyIntegration.Builders;
using Sang.Service.TallyIntegration.Configurations;
using Sang.Service.TallyIntegration.Services;

namespace Sang.Service.TallyIntegration.Extensions
{
    public static class TallyServiceCollectionExtensions
    {
        public static IServiceCollection AddTallyIntegration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<TallyOptions>(
                configuration.GetSection("Tally"));

            services.AddHttpClient("TallyPrime");

            services.TryAddSingleton<TallyMasterJsonBuilder>();
            services.TryAddSingleton<TallyMasterXmlBuilder>();

            services.TryAddScoped<
                ITallyTransactionService,
                TallyTransactionService>();

            services.TryAddScoped<
                ITallyService,
                TallyService>();

            return services;
        }
    }
}