using System;
using System.Threading.Tasks;
using Fanex.Athena.IdentityServer.Core.Helpers;
using Moq;
using Xunit;

namespace Fanex.Athena.IdentityServer.Core.UnitTests.Helpers
{
    public class LockHelperTest
    {
        [Fact]
        public void Lock_WhenCall_InvokeMethod()
        {
            // Arrange
            const string key = "key";
            const string value = "value";
            var doing = new Mock<Func<object>>();
            doing.Setup(x => x.Invoke()).Returns(value);

            // Act
            var result = LockHelper.Lock(key, doing.Object);

            // Assert
            Assert.Equal(value, result);
            doing.Verify(x => x.Invoke());
        }

        [Fact]
        public async Task LockAsync_WhenCall_InvokeMethod()
        {
            // Arrange
            const string key = "key";
            const string value = "value";
            var doing = new Mock<Func<Task<string>>>();
            doing.Setup(x => x.Invoke()).Returns(Task.FromResult(value));

            // Act
            var result = await LockHelper.LockAsync(key, doing.Object);

            // Assert
            Assert.Equal(value, result);
            doing.Verify(x => x.Invoke());
        }
    }
}
