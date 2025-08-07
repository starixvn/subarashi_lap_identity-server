namespace Fanex.Athena.IdentityServer
{
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using Core;
    using Core.Extensions;
    using Core.Middlewares;
    using Core.Settings;
    using Core.Validators;
    using Data;
    using Data.SqlServer;
    using Data.Vault;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.ResponseCompression;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Tokens;

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

            ConfigFanexData(services);

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
                .AddExtensionGrantValidator<StatementPasswordDelegationValidator>()
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

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = new[] { "application/json" };
            });

            services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UsePing();
            app.UseForwardedHeaders();

            if (env.IsEnvironment("PROD") || string.Equals(Configuration["UseErrorHandler"], "1"))
            {
                app.UseMiddleware<ErrorHandler>();
            }
            else
            {
                app.UseDeveloperExceptionPage(); //NOSONAR not used in secure context
            }

            app.UseHealthCheck();
            app.UseRouting();
            app.UseResponseCompression();
            app.UseIdentityServer();
            app.UseEndpoints(endpoints => { endpoints.MapGet("/", async context => await context.Response.WriteAsync("LAP Identity Server is running")); });
        }

        private void ConfigFanexData(IServiceCollection services)
        {
            DbSettingProviderManager
                .StartNewSession()
                .WithSqlServer(Configuration)
                .UseDefaultDbSettingProvider("~/App_Data/SpConfig/FanexSpName.xml", true)
                .Run();

            services.TryAddSingleton<VaultDbAliasService, VaultDbAliasServiceImp>();
        }
    }
}