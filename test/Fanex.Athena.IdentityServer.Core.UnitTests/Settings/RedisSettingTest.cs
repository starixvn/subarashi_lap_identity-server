namespace Fanex.Athena.IdentityServer.Core.UnitTests.Settings
{
    using Core.Settings;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using Xunit;

    public class RedisSettingTest
    {
        [Theory]
        [AutoMoqData]
        public void Server_SetAndGet_ReturnsCorrectValue(string server, RedisSetting setting)
        {
            setting.Server = server;

            Assert.Equal(server, setting.Server);
        }

        [Theory]
        [AutoMoqData]
        public void Port_SetAndGet_ReturnsCorrectValue(int port, RedisSetting setting)
        {
            setting.Port = port;

            Assert.Equal(port, setting.Port);
        }

        [Theory]
        [AutoMoqData]
        public void Pass_SetAndGet_ReturnsCorrectValue(string pass, RedisSetting setting)
        {
            setting.Pass = pass;

            Assert.Equal(pass, setting.Pass);
        }

        [Theory]
        [AutoMoqData]
        public void AppName_SetAndGet_ReturnsCorrectValue(string appName, RedisSetting setting)
        {
            setting.AppName = appName;

            Assert.Equal(appName, setting.AppName);
        }

        [Theory]
        [AutoMoqData]
        public void Database_SetAndGet_ReturnsCorrectValue(int database, RedisSetting setting)
        {
            setting.Database = database;

            Assert.Equal(database, setting.Database);
        }

        [Fact]
        public void Constructor_CreatesInstance_PropertiesAreDefault()
        {
            var setting = new RedisSetting();

            Assert.Null(setting.Server);
            Assert.Equal(0, setting.Port);
            Assert.Null(setting.Pass);
            Assert.Null(setting.AppName);
            Assert.Equal(0, setting.Database);
        }
    }
}
