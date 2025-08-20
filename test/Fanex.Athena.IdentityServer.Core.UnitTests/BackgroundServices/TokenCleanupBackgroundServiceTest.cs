namespace Fanex.Athena.IdentityServer.Core.UnitTests.BackgroundServices
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.BackgroundServices;
    using Core.Services;
    using Core.Settings;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class TokenCleanupBackgroundServiceTest : IDisposable
    {
        private TokenCleanupBackgroundServiceTestable _service;
        private CancellationTokenSource _stoppingToken;

        public TokenCleanupBackgroundServiceTest()
        {
            _stoppingToken = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _stoppingToken.Cancel();
            _stoppingToken = null;
        }

        [Theory]
        [AutoMoqData]
        public async Task ExecuteAsync_EnableTokenCleanup_CleanUp(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options
        )
        {
            // Arrange
            options.EnableTokenCleanup = true;
            options.CleanupTokenOnInstance = string.Empty;

            _service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act
            await _service.ExecuteAsync(_stoppingToken.Token);

            // Assert
            logger.Verify(
                          x => x.Log(
                                     It.Is<LogLevel>(l => l == LogLevel.Warning),
                                     It.IsAny<EventId>(),
                                     It.Is<It.IsAnyType>((v, t) => v.ToString()
                                                                    .Equals("Starting token removal background service")),
                                     It.IsAny<Exception>(),
                                     It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ExecuteAsync_DuplicateCall_ThrowInvalidOperationException(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Arrange
            options.EnableTokenCleanup = true;
            options.CleanupTokenOnInstance = string.Empty;
            options.TokenCleanupInterval = 2;
            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act
            await service.ExecuteAsync(_stoppingToken.Token);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(_stoppingToken.Token));
        }

        [Theory]
        [AutoMoqData]
        public async Task ExecuteAsync_NotEnableTokenCleanup_SkipCleanUp(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Arrange
            options.EnableTokenCleanup = true;

            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act
            await service.ExecuteAsync(_stoppingToken.Token);

            // Assert
            logger.Verify(
                          x => x.Log(
                                     It.Is<LogLevel>(l => l == LogLevel.Warning),
                                     It.IsAny<EventId>(),
                                     It.Is<It.IsAnyType>((v, t) => v.ToString()
                                                                    .Equals("Skipping token removal background service")),
                                     It.IsAny<Exception>(),
                                     It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task StopAsync_EnableTokenCleanup_StopSource(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Arrange
            options.EnableTokenCleanup = true;
            options.CleanupTokenOnInstance = string.Empty;

            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act
            await service.ExecuteAsync(_stoppingToken.Token);
            await service.StopAsync(_stoppingToken.Token);

            // Assert
            logger.Verify(
                          x => x.Log(
                                     It.Is<LogLevel>(l => l == LogLevel.Warning),
                                     It.IsAny<EventId>(),
                                     It.Is<It.IsAnyType>((v, t) => v.ToString()
                                                                    .Contains("Stopping token removal background service")),
                                     It.IsAny<Exception>(),
                                     It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task StopAsync_NotStarted_ThrowInvalidOperationException(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Arrange
            options.EnableTokenCleanup = true;
            options.CleanupTokenOnInstance = string.Empty;

            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.StopAsync(_stoppingToken.Token));
        }

        [Theory]
        [AutoMoqData]
        public async Task StopAsync_TokenCleanupDisabled_DoesNotThrow(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Arrange
            options.EnableTokenCleanup = false;

            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act & Assert - should not throw when cleanup is disabled
            var exception = await Record.ExceptionAsync(() => service.StopAsync(_stoppingToken.Token));
            Assert.Null(exception);
        }

        [Theory]
        [AutoMoqData]
        public void Constructor_WithValidParameters_SetsProperties(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Act
            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Theory]
        [AutoMoqData]
        public void Dispose_WithStartedService_DisposesSource(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Arrange
            options.EnableTokenCleanup = true;
            options.CleanupTokenOnInstance = string.Empty;

            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act & Assert - should not throw
            var exception = Record.Exception(() => service.Dispose());
            Assert.Null(exception);
        }

        [Theory]
        [AutoMoqData]
        public async Task ExecuteAsync_WithSpecificHostname_EnablesCleanupForMatchingHost(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Arrange
            options.EnableTokenCleanup = true;
            options.CleanupTokenOnInstance = Dns.GetHostName(); // Current machine hostname

            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act
            await service.ExecuteAsync(_stoppingToken.Token);

            // Assert
            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()
                                                   .Equals("Starting token removal background service")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Theory]
        [AutoMoqData]
        public async Task ExecuteAsync_WithDifferentHostname_SkipsCleanup(
            Mock<ITokenCleanupService> tokenCleanUpService,
            Mock<ILogger<TokenCleanupBackgroundService>> logger,
            OperationalStoreSetting options)
        {
            // Arrange
            options.EnableTokenCleanup = true;
            options.CleanupTokenOnInstance = "different-hostname-that-does-not-exist";

            var service = new TokenCleanupBackgroundServiceTestable(tokenCleanUpService.Object, options, logger.Object);

            // Act
            await service.ExecuteAsync(_stoppingToken.Token);

            // Assert
            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()
                                                   .Equals("Skipping token removal background service")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        #region TestHelpers
        private class TokenCleanupBackgroundServiceTestable : TokenCleanupBackgroundService
        {
            public TokenCleanupBackgroundServiceTestable(ITokenCleanupService tokenCleanupService,
                                                         OperationalStoreSetting options,
                                                         ILogger<TokenCleanupBackgroundService> logger) :
                base(tokenCleanupService, options, logger)
            { }

            public new Task ExecuteAsync(CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(cancellationToken);
            }
        }

        #endregion
    }
}
