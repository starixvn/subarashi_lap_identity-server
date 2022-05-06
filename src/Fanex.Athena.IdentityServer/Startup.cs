using Fanex.Athena.IdentityServer.Core;
using Fanex.Athena.IdentityServer.Core.Extensions;
using Fanex.Athena.IdentityServer.Core.Middlewares;
using Fanex.Athena.IdentityServer.Core.Settings;
using Fanex.Athena.IdentityServer.Core.Validators;
using Fanex.Data;
using Fanex.Data.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
                                                                           
namespace Fanex.Athena.IdentityServer
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
               .AddInMemoryApiResources(Config.GetApis(Configuration))
               .AddInMemoryClients(Config.GetClients(Configuration))
               .AddInMemoryApiScopes(Config.ScopeApis)
               .AddExtensionGrantValidator<LapTokenDelegationValidator>()
               .AddExtensionGrantValidator<LrfPasswordDelegationValidator>()
               .AddOperationalStore(Configuration)
               .AddSigningCredential(credentials);

            services.AddHealthCheck(Configuration);

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.Configure<AppSetting>(Configuration.GetSection("App"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (string.Equals(Configuration["UseErrorHandler"], "1"))
            {
                app.UseMiddleware<ErrorHandler>();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthCheck();
            app.UseRouting();
            app.UseIdentityServer();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => await context.Response.WriteAsync("LAP Identity Server is running"));
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