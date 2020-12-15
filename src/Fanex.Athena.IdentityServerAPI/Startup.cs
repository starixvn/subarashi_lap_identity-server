using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Test;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServerAPI
{

    public static class ValidationContextExtension
    {
    }

    public interface ILrfPasswordGrantValidator
    {

    }

    public class LrfPasswordGrantValidator : ILrfPasswordGrantValidator
    {
        public LrfPasswordGrantValidator()
        {
        } 
    }

    public class LrfPasswordValidator : IExtensionGrantValidator
    {
        public string GrantType => "lrf_password";

        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var param = context.Request.Raw;
            var username = param["username"];
            var password = param["password"];
            var loginToken = param["login_token"];


            context.Result = new GrantValidationResult(
                    "818727",
                    OidcConstants.AuthenticationMethods.Password, DateTime.UtcNow,
                    new[] { new Claim("licensee_info", @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json) });

            return Task.CompletedTask;
        }
    }

    public class LapTokenValidator : IExtensionGrantValidator
    {
        public string GrantType => "lap_token";

        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            context.Result = new GrantValidationResult(
                    "818727",
                    OidcConstants.AuthenticationMethods.Password, DateTime.UtcNow,
                    new[] { new Claim("licensee_info", @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json) });

            return Task.CompletedTask;
        }
    }

    public class StatementPasswordValidator : IExtensionGrantValidator
    {
        public string GrantType => "statement_password";

        public Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            context.Result = new GrantValidationResult(
                    "818727",
                    OidcConstants.AuthenticationMethods.Password, DateTime.UtcNow,
                    new[] { new Claim("licensee_info", @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServer4.IdentityServerConstants.ClaimValueTypes.Json) });

            return Task.CompletedTask;
        }
    }


    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }

        public IHostEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var operationalStoreOptions = new ConfigurationOptions
            {
                DefaultDatabase = 4,
                EndPoints = { { "ha.nexdev.net", 16379 } },
                Password = "foo1@bared"
            };

            var fileName = Path.Combine(Env.ContentRootPath, "pair-key.secrect");

            string xml = System.IO.File.ReadAllText(fileName);
            var csp = new RSACryptoServiceProvider(2048);
            csp.FromXmlString(xml);
            var securityKey = new RsaSecurityKey(csp);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

            services.AddIdentityServer()
               .AddInMemoryIdentityResources(Config.Ids)
               .AddInMemoryApiResources(Config.Apis)
               .AddInMemoryApiScopes(Config.ScopeApis)
               .AddInMemoryClients(Config.Clients)
               .AddExtensionGrantValidator<LrfPasswordValidator>()
               .AddExtensionGrantValidator<LapTokenValidator>()
               .AddExtensionGrantValidator<StatementPasswordValidator>()
               .AddOperationalStore(options =>
               {
                   options.ConfigurationOptions = operationalStoreOptions;
                   options.KeyPrefix = "LAP:IdentityServer";
               })
               .AddSigningCredential(credentials);


        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.Use(async (req, q) =>
            {
                await q();
            });

            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
