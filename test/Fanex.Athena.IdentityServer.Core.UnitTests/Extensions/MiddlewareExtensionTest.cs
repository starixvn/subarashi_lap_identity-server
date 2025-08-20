namespace Fanex.Athena.IdentityServer.Core.UnitTests.Extensions
{
    using System;
    using AutoFixture.Xunit2;
    using Core.Extensions;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Xunit;

    public class MiddlewareExtensionTest
    {
        [Theory]
        [AutoMoqData]
        public void AddHealthCheck_WithConfiguration_AddsHealthChecks(
            [Frozen] Mock<IServiceCollection> services,
            Mock<IConfiguration> configuration)
        {
            // Arrange
            var redisServer = "localhost";
            var redisPort = "6379";
            var redisPass = "password";

            configuration.Setup(c => c["Redis:Server"]).Returns(redisServer);
            configuration.Setup(c => c["Redis:Port"]).Returns(redisPort);
            configuration.Setup(c => c["Redis:Pass"]).Returns(redisPass);

            // Act
            services.Object.AddHealthCheck(configuration.Object);

            // Assert
            services.Verify(s => s.Add(It.IsAny<ServiceDescriptor>()), Times.AtLeastOnce);
        }

        [Theory]
        [AutoMoqData]
        public void UsePing_WithApplicationBuilder_ConfiguresPingMiddleware(
            Mock<IApplicationBuilder> app)
        {
            // Act
            app.Object.UsePing();

            // Assert
            app.Verify(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public void UseHealthCheck_WithApplicationBuilder_ConfiguresHealthCheckMiddleware(
            Mock<IApplicationBuilder> app)
        {
            // Act
            app.Object.UseHealthCheck();

            // Assert - UseHealthCheck calls UseHealthChecks which should be verified
            app.Verify(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.AtLeastOnce);
        }

        [Theory]
        [AutoMoqData]
        public void AddHealthCheck_WithNullConfiguration_ThrowsException(
            Mock<IServiceCollection> services)
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => services.Object.AddHealthCheck(null));
        }

        [Theory]
        [AutoMoqData]
        public void AddHealthCheck_WithMissingRedisConfig_StillAddsHealthChecks(
            [Frozen] Mock<IServiceCollection> services,
            Mock<IConfiguration> configuration)
        {
            // Arrange - setup configuration to return null for Redis values
            configuration.Setup(c => c["Redis:Server"]).Returns((string)null);
            configuration.Setup(c => c["Redis:Port"]).Returns((string)null);
            configuration.Setup(c => c["Redis:Pass"]).Returns((string)null);

            // Act
            var exception = Record.Exception(() => services.Object.AddHealthCheck(configuration.Object));

            // Assert - should not throw even with missing config
            Assert.Null(exception);
            services.Verify(s => s.Add(It.IsAny<ServiceDescriptor>()), Times.AtLeastOnce);
        }

        [Theory]
        [AutoMoqData]
        public void AddHealthCheck_RegistersExpectedHealthChecks(
            Mock<IServiceCollection> services,
            Mock<IConfiguration> configuration)
        {
            // Arrange
            configuration.Setup(c => c["Redis:Server"]).Returns("localhost");
            configuration.Setup(c => c["Redis:Port"]).Returns("6379");
            configuration.Setup(c => c["Redis:Pass"]).Returns("password");

            // Act
            services.Object.AddHealthCheck(configuration.Object);

            // Assert - verify that services are being added
            services.Verify(s => s.Add(It.IsAny<ServiceDescriptor>()), Times.AtLeastOnce);
        }

        [Fact]
        public void UsePing_WithNullApplicationBuilder_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => MiddlewareExtension.UsePing(null));
        }

        [Fact]
        public void UseHealthCheck_WithNullApplicationBuilder_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => MiddlewareExtension.UseHealthCheck(null));
        }
    }
}
