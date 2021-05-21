using Fanex.Athena.IdentityServer.Infrastructure.HealthCheck;
using Fanex.Athena.IdentityServer.Infrastructure.Models;
using Fanex.Athena.IdentityServer.Infrastructure.Repositories;
using Fanex.Athena.IdentityServer.Infrastructure.Services;
using Fanex.Athena.IdentityServer.Infrastructure.Settings;
using Fanex.Data;
using LAP.ONEBook.Security;
using LAP.ONEBook.Security.StorageProvider.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Fanex.Athena.IdentityServer.Infrastructure.Extensions
{
    public static class DefendencyInjectionExtension
    {                                                                         
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddServices(configuration)
                .AddRepositories();
        }

        public static IIdentityServerBuilder AddOperationalStoreWithRedis(this IIdentityServerBuilder identityServerBuilder, IConfiguration configuration)
        {
            var redis = new RedisSetting();
            configuration.GetSection("Redis").Bind(redis);
            var operationalStoreOptions = new ConfigurationOptions
            {
                DefaultDatabase = redis.Database,
                EndPoints = { { redis.Server, redis.Port } },
                Password = redis.Pass,
            };

            return identityServerBuilder.AddOperationalStore(options =>
                   {
                       options.ConfigurationOptions = operationalStoreOptions;
                       options.KeyPrefix = redis.AppName;
                   });
        }

        private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IExtraInformationService, ExtraInformationService>();

            var securityConnection = GetOneBookSecurityConnection(configuration);
            services.AddSingleton<IAuth<DefaultAuthInfo<LapPermission, LapCurrencyAgent>>>(new Auth<DefaultAuthInfo<LapPermission, LapCurrencyAgent>>(new SqlServerProvider(securityConnection)));

            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddSingleton<IDbFactory, DbFactory>();

            return services;
        }

        private static string GetOneBookSecurityConnection(IConfiguration configuration)
        {
            var encryptedConnectionString = configuration.GetConnectionString("LAP.OneBookSecurity");
            return Cryptography.DecryptConnectionString(encryptedConnectionString);
        }
    }
}