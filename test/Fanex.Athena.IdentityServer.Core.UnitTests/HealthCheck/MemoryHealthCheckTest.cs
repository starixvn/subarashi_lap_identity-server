namespace Fanex.Athena.IdentityServer.Core.UnitTests.HealthCheck
{
    using System;
    using System.Threading.Tasks;
    using Core.HealthCheck;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Xunit;

    public class MemoryHealthCheckTest
    {
        private readonly MemoryHealthCheck _healthChecks = new();

        [Fact]
        public async Task MemoryHealthCheck_Should_ReturnCorrectData()
        {
            // Arrange
            var status = GC.GetTotalMemory(false) / (1024L * 1024L) < 1024L ? HealthStatus.Healthy : HealthStatus.Degraded;

            // Action
            var result = await _healthChecks.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.Equal(status, result.Status);
            Assert.Equal("Reports degraded status if allocated bytes >= 1024 mbs.", result.Description);
            Assert.Null(result.Exception);
        }
    }
}