using Fanex.Athena.IdentityServer.Infrastructure.Extensions;
using Fanex.Athena.IdentityServer.Infrastructure.Validators;
using Fanex.Data;
using Fanex.Data.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

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
            services.AddControllersWithViews();
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

            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var forwardOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All,
            };
            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardOptions);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthChecks("/health");
            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}"
                    );
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
                .WithSqlServer(resourcePath: "~/SpNameConfig/FanexSpName.xml")
                .Use(connectionStrings)
                .Run();
        }
    }
}