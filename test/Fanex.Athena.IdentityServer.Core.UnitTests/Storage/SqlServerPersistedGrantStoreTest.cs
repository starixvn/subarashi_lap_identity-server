using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Athena.IdentityServer.Core.Storage;
using Fanex.Data;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fanex.Athena.IdentityServer.Core.UnitTests.Storage
{
    public class SqlServerPersistedGrantStoreTest
    {
        private readonly Mock<IDbFactory> _dbFactory;
        private readonly Mock<ILogger<SqlServerPersistedGrantStore>> _logger;
        private readonly Mock<IObjectDb> _objDb;

        public SqlServerPersistedGrantStoreTest()
        {
            _logger = new Mock<ILogger<SqlServerPersistedGrantStore>>();
            _dbFactory = new Mock<IDbFactory>();
            _objDb = new Mock<IObjectDb>();
        }

        [Theory] [AutoData]
        public async Task GetAllAsync_WhenCall_ReturnValue(PersistedGrantFilter filter,
                                                           IEnumerable<PersistedGrant> expected)
        {
            // Arrange
            _objDb.Setup(x => x.QueryAsync<PersistedGrant>(It.IsAny<PersistedGrantFilter>(), null, default))
                  .Returns(Task.FromResult(expected));
            _dbFactory.Setup(x => x.GetObjectDb("LAP_IdentityServer_TokenSel", It.IsAny<int>())).Returns(_objDb.Object);

            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act
            var result = await storage.GetAllAsync(filter);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetAllAsync_InvalidFilter_ThrowArgumentNullException()
        {
            // Arrange
            var filter = new PersistedGrantFilter();

            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => storage.GetAllAsync(filter));
        }

        [Fact]
        public async Task GetAllAsync_NullFilter_ThrowArgumentNullException()
        {
            // Arrange
            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => storage.GetAllAsync(null));
        }

        [Theory] [AutoData]
        public async Task GetAsync_WhenCall_ReturnValue(string key, PersistedGrant expected)
        {
            // Arrange
            _objDb.Setup(x => x.QueryEntityAsync<PersistedGrant>(It.IsAny<object>(), null, default))
                  .Returns(Task.FromResult(expected));
            _dbFactory.Setup(x => x.GetObjectDb("LAP_IdentityServer_TokenSel", It.IsAny<int>())).Returns(_objDb.Object);

            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act
            var result = await storage.GetAsync(key);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory] [AutoData]
        public async Task RemoveAllAsync_WhenCall_ExecuteQuery(PersistedGrantFilter filter)
        {
            // Arrange
            _dbFactory.Setup(x => x.GetObjectDb("LAP_IdentityServer_TokenDel", It.IsAny<int>())).Returns(_objDb.Object);

            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act
            await storage.RemoveAllAsync(filter);

            // Assert
            _objDb.Verify(x => x.ExecuteNonQueryAsync(filter, null, default));
        }

        [Theory] [AutoData]
        public async Task RemoveAllAsync_ThrowException_HandleException(PersistedGrantFilter filter,
                                                                        Exception exception)
        {
            // Arrange
            _objDb.Setup(x => x.ExecuteNonQueryAsync(filter, null, default)).Throws(exception);
            _dbFactory.Setup(x => x.GetObjectDb("LAP_IdentityServer_TokenDel", It.IsAny<int>())).Returns(_objDb.Object);

            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act
            await storage.RemoveAllAsync(filter);

            // Assert
            _logger.Verify(
                           x => x.Log(
                                      It.Is<LogLevel>(l => l == LogLevel.Critical),
                                      It.IsAny<EventId>(),
                                      It.Is<It.IsAnyType>((v, t) => true),
                                      exception,
                                      It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Theory] [AutoData]
        public async Task RemoveAsync_WhenCall_ExecuteQuery(string key)
        {
            // Arrange
            _dbFactory.Setup(x => x.GetObjectDb("LAP_IdentityServer_TokenDel", It.IsAny<int>())).Returns(_objDb.Object);

            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act
            await storage.RemoveAsync(key);

            // Assert
            _objDb.Verify(x => x.ExecuteNonQueryAsync(It.IsAny<object>(), null, default));
        }

        [Theory] [AutoData]
        public async Task RemoveAsync_ThrowException_HandleException(string key, Exception exception)
        {
            // Arrange
            _objDb.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<object>(), null, default)).Throws(exception);
            _dbFactory.Setup(x => x.GetObjectDb("LAP_IdentityServer_TokenDel", It.IsAny<int>())).Returns(_objDb.Object);

            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act
            await storage.RemoveAsync(key);

            // Assert
            _logger.Verify(
                           x => x.Log(
                                      It.Is<LogLevel>(l => l == LogLevel.Critical),
                                      It.IsAny<EventId>(),
                                      It.Is<It.IsAnyType>((v, t) => true),
                                      exception,
                                      It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Theory] [AutoData]
        public async Task StoreAsync_WhenCall_ExecuteQuery(PersistedGrant grant)
        {
            // Arrange
            _dbFactory.Setup(x => x.GetObjectDb("LAP_IdentityServer_TokenInsOrUpd", It.IsAny<int>()))
                      .Returns(_objDb.Object);

            var storage = new SqlServerPersistedGrantStore(_dbFactory.Object, _logger.Object);

            // Act
            await storage.StoreAsync(grant);

            // Assert
            _objDb.Verify(x => x.ExecuteNonQueryAsync(grant, null, default));
        }
    }
}
