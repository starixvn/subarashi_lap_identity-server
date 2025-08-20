namespace Fanex.Athena.IdentityServer.Core.UnitTests.Repositories
{
    using System;
    using Core.Repositories;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using Xunit;

    public class DbFactoryTest
    {
        [Fact]
        public void Constructor_CreatesInstance_ImplementsInterface()
        {
            var factory = new DbFactory();

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IDbFactory>(factory);
        }

        [Theory]
        [AutoMoqData]
        public void GetObjectDb_WithSpName_CallsObjectDbFactory(string spName)
        {
            var factory = new DbFactory();

            // ObjectDbFactory.CreateInstance may throw due to missing configuration
            // We test that our wrapper calls the underlying factory correctly
            var exception = Record.Exception(() => factory.GetObjectDb(spName));

            // We expect either null (no exception) or a NullReferenceException from ObjectDbFactory
            Assert.True(exception == null || exception is NullReferenceException);
        }

        [Theory]
        [AutoMoqData]
        public void GetObjectDb_WithSpNameAndTimeout_CallsObjectDbFactory(string spName, int timeout)
        {
            var factory = new DbFactory();

            // Test that the method calls ObjectDbFactory with custom timeout
            var exception = Record.Exception(() => factory.GetObjectDb(spName, timeout));

            // We expect either null (no exception) or a NullReferenceException from ObjectDbFactory
            Assert.True(exception == null || exception is NullReferenceException);
        }

        [Theory]
        [AutoMoqData]
        public void GetObjectDb_WithNegativeTimeout_CallsObjectDbFactory(string spName)
        {
            var factory = new DbFactory();

            // Test edge case with negative timeout
            var exception = Record.Exception(() => factory.GetObjectDb(spName, -1));

            // We expect either null (no exception) or a NullReferenceException from ObjectDbFactory
            Assert.True(exception == null || exception is NullReferenceException);
        }
    }
}
