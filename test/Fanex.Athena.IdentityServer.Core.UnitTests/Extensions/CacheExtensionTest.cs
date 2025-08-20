namespace Fanex.Athena.IdentityServer.Core.UnitTests.Extensions
{
    using System;
    using System.Threading.Tasks;
    using Core.Extensions;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using Microsoft.Extensions.Caching.Memory;
    using Moq;
    using Xunit;

    public class CacheExtensionTest
    {
        [Theory] [AutoCacheData]
        public async Task TryGetOrAddFromCache_ExistData_ReturnData(
            IMemoryCache memoryCache,
            string cacheKey,
            TimeSpan cacheTime)
        {
            // Arrange
            const string expected = "Value";
            var func = new Func<Task<string>>(() => Task.FromResult("Any"));
            memoryCache.Set(cacheKey, expected);

            // Act
            var result = await memoryCache.TryGetOrAddFromCache(cacheKey, cacheTime, func);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory] [AutoCacheData]
        public async Task TryGetOrAddFromCache_NullData_SetData(IMemoryCache memoryCache, string cacheKey)
        {
            // Arrange
            var time = TimeSpan.FromMinutes(1);

            const string expected = "Value";
            var func = new Mock<Func<Task<string>>>();
            func.Setup(x => x.Invoke()).Returns(Task.FromResult(expected));

            // Act
            var result = await memoryCache.TryGetOrAddFromCache(cacheKey, time, func.Object);
            var cachedValue = memoryCache.Get(cacheKey);

            // Assert
            Assert.Equal(expected, result);
            Assert.Equal(expected, cachedValue);

            func.Verify(_ => _.Invoke());
        }
    }
}
