namespace Fanex.Athena.IdentityServer.Core.UnitTests.HealthCheck
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using AutoFixture;
    using Core.HealthCheck;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Primitives;
    using Moq;
    using Xunit;

    public class WhoAmIHealthCheckTest
    {
        private readonly Fixture _fixture;
        private readonly WhoAmIHealthCheck _healthChecks;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IWebHostEnvironment> _webHostEnvironment;

        public WhoAmIHealthCheckTest()
        {
            _fixture = new Fixture();
            _webHostEnvironment = new Mock<IWebHostEnvironment>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _healthChecks = new WhoAmIHealthCheck(_httpContextAccessor.Object, _webHostEnvironment.Object);
        }

        [Fact]
        public async Task ServerHealthCheck_Should_ReturnCorrectData()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            var hostName = Dns.GetHostName();
            var serverIps = await Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false);
            var lastUpdated = File.GetCreationTime(typeof(WhoAmIHealthCheck).Assembly.Location).ToString(CultureInfo.InvariantCulture);
            _webHostEnvironment.Setup(x => x.EnvironmentName).Returns("Test");

            var expected = new Dictionary<string, object>
            {
                { "Env", "Test" },
                { "Host Name", hostName },
                { "Local Ip", httpContext.Connection.LocalIpAddress?.ToString()! },
                { "Local Port", httpContext.Connection.LocalPort! },
                { "Client Ip", httpContext.Connection.RemoteIpAddress?.ToString()! },
                { "Last Updated", lastUpdated },
                { "Host", httpContext.Request.Host },
                { "Schema", httpContext.Request.Scheme },
                { "Accept-Encoding", httpContext.Request.Headers.TryGetValue("Accept-Encoding", out StringValues encoding) ? encoding : string.Empty }
            };

            for (var i = 0; i < serverIps.Length; i++)
            {
                if (serverIps[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    expected.Add($"IP{i}", serverIps[i].ToString());
                }
            }

            // Action
            var result = await _healthChecks.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal("Server Info", result.Description);
            Assert.Null(result.Exception);
            foreach (var pair in expected)
            {
                Assert.Equal(pair.Value, result.Data.GetValueOrDefault(pair.Key, null));
            }
        }
    }
}