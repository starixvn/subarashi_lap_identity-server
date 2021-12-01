using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Athena.IdentityServer.Core.Services;
using Fanex.Athena.IdentityServer.Core.Settings;
using Fanex.Data;
using LAP.ONEBook.Security;
using LAP.ONEBook.Security.StorageProvider.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Fanex.Athena.IdentityServer.Core
{
    public static class DefendencyInjection
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
            services.AddSingleton<IExtraInformationService, ExtraInformationService>();
            services.AddSingleton<ILrfAuthService, LrfAuthService>();
            services.AddSingleton<ILrfFunctionService, LrfFunctionService>();

            var securityConnection = GetOneBookSecurityConnection(configuration);
            services.AddSingleton<IAuth<DefaultAuthInfo<LapFunction, CurrencyAgent>>>(new Auth<DefaultAuthInfo<LapFunction, CurrencyAgent>>(new SqlServerProvider(securityConnection)));

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