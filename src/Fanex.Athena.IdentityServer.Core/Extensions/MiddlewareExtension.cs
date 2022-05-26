using Fanex.Athena.IdentityServer.Core.HealthCheck;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Extensions
{
    public static class MiddlewareExtension
    {
        public static void AddHealthCheck(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
               .AddCheck<WhoAmIHealthCheck>("WhoAmI")
               .AddCheck<MemoryHealthCheck>("Memory")
               .AddRedis($"{configuration["Redis:Server"]}:{configuration["Redis:Port"]},password={configuration["Redis:Pass"]}", "Redis");
        }

        public static void UsePing(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/ping"))
                {
                    return;
                }
                await next();
            });
        }

        public static void UseHealthCheck(this IApplicationBuilder app)
        {
            Func<HttpContext, HealthReport, Task> customResponseWriter = async (context, report) =>
            {
                var result = JsonConvert.SerializeObject(
                    new
                    {
                        status = report.Status.ToString(),
                        monitors = report.Entries.Select(e => new { name = e.Key, status = Enum.GetName(typeof(HealthStatus), e.Value.Status), values = e.Value.Data })
                    }, Formatting.Indented);

                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(result);
            };

            app.UseHealthChecks("/WhoAmI", options: new HealthCheckOptions
            {
                AllowCachingResponses = false,
                Predicate = _ => _.Name.Equals("WhoAmI"),
                ResponseWriter = customResponseWriter
            }).UseHealthChecks("/health", options: new HealthCheckOptions
            {
                AllowCachingResponses = false,
                Predicate = _ => !_.Name.Equals("WhoAmI"),
                ResponseWriter = customResponseWriter
            });
        }
    }
}