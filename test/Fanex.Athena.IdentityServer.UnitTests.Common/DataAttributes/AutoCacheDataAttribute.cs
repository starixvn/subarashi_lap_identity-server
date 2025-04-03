using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Moq;

namespace Fanex.Athena.IdentityServer.UnitTests.Common.DataAttributes
{
    public class AutoCacheDataAttribute : AutoDataAttribute
    {
        public AutoCacheDataAttribute() :
            base(() =>
                 {
                     var fixture = new Fixture();
                     fixture.Customize(new AutoMoqCustomization());

                     RegisterMemoryCache(fixture);

                     return fixture;
                 }) { }

        private static void RegisterMemoryCache(IFixture fixture)
        {
            var cacheMock = new Mock<IOptions<MemoryCacheOptions>>();
            cacheMock.Setup(x => x.Value).Returns(new MemoryCacheOptions { Clock = new SystemClock() });
            fixture.Register<IMemoryCache>(() => new MemoryCache(cacheMock.Object));
        }
    }
}
