using Fanex.Athena.IdentityServer.Infrastructure.Extensions;
using Fanex.Athena.IdentityServer.Infrastructure.Validators;
using Fanex.Data;
using Fanex.Data.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServerAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }

        public IHostEnvironment Env { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddMemoryCache();
            services.AddInfrastructure(Configuration);

            ConfigFanexData();

            var fileName = Path.Combine(Env.ContentRootPath, "pair-key.secrect");
            string xml = File.ReadAllText(fileName);
            var csp = new RSACryptoServiceProvider(2048);
            csp.FromXmlString(xml);
            var securityKey = new RsaSecurityKey(csp);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

            services.AddIdentityServer()
               .AddInMemoryIdentityResources(Config.Ids)
               .AddInMemoryApiResources(Config.Apis)
               .AddInMemoryClients(Config.Clients)
               .AddInMemoryApiScopes(Config.ScopeApis)
               .AddExtensionGrantValidator<LapTokenDelegationValidator>()
               .AddOperationalStoreWithRedis(Configuration)
               .AddSigningCredential(credentials);

            services.AddHealthCheck(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthCheck();
            app.UseRouting();
            app.UseIdentityServer();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void ConfigFanexData()
        {
            var listConnectionName = Configuration.GetSection("ConnectionStrings").GetChildren().Select(_ => _.Key);
            var connectionStrings = new Dictionary<string, ConnectionConfiguration>(listConnectionName
                .Select(connectionName => new KeyValuePair<string, ConnectionConfiguration>(connectionName,
                    new ConnectionConfiguration(connectionName, Configuration.GetConnectionString(connectionName)))
                ));

            DbSettingProviderManager
                .StartNewSession()
                .WithSqlServer(resourcePath: "~/App_Data/SpConfig/FanexSpName.xml")
                .Use(connectionStrings)
                .Run();
        }
    }
}