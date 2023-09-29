using System;
using System.Threading.Tasks;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Athena.IdentityServer.Core.Services;
using Fanex.Athena.IdentityServer.Core.Settings;
using Fanex.Athena.IdentityServer.UnitTests.Common.DataAttributes;
using Fanex.Data;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fanex.Athena.IdentityServer.Core.UnitTests.Services
{
    public class TokenCleanupServiceTest
    {
        [Theory] [AutoMoqData]
        public async Task CleanupExpiredTokenAsync_WhenCall_ExecuteQuery(
            Mock<IDbFactory> dbFactory,
            Mock<IObjectDb> objDb,
            OperationalStoreSetting options,
            Mock<ILogger<TokenCleanupService>> logger)
        {
            // Arrange
            dbFactory.Setup(x => x.GetObjectDb("LAP_IdentityServer_ExpiredTokenDel", It.IsAny<int>()))
                     .Returns(objDb.Object);
            var service = new TokenCleanupService(dbFactory.Object, options, logger.Object);

            // Act
            await service.CleanupExpiredTokenAsync();

            // Assert
            objDb.Verify(x => x.ExecuteNonQueryAsync(It.IsAny<object>(), null, default));
            logger.Verify(
                          x => x.Log(
                                     It.Is<LogLevel>(l => l == LogLevel.Warning),
                                     It.IsAny<EventId>(),
                                     It.Is<It.IsAnyType>((v, t) => v.ToString().Equals("Start cleanup expired tokens")),
                                     It.IsAny<Exception>(),
                                     It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }
    }
}
