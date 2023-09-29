using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Athena.IdentityServer.Core.Services;
using Fanex.Athena.IdentityServer.Core.Settings;
using Fanex.Athena.IdentityServer.UnitTests.Common;
using Fanex.Athena.IdentityServer.UnitTests.Common.ServiceMoqExtensions;
using Fanex.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Fanex.Athena.IdentityServer.Core.UnitTests.Services
{
    public class ExtraInformationServiceTest
    {
        private readonly Mock<IOptions<AppSetting>> _appSetting;
        private readonly Mock<IDbFactory> _dbFactory;
        private readonly IMemoryCache _memoryCache;

        private readonly Mock<IObjectDb> _objDb;
        private readonly Mock<IDataReader> _reader;

        private readonly Mock<IObjectDb> _siteDbConfigObject;
        private readonly Mock<IDataReader> _siteDbConfigReader;

        public ExtraInformationServiceTest()
        {
            _dbFactory = new Mock<IDbFactory>();
            _objDb = new Mock<IObjectDb>();
            _reader = new Mock<IDataReader>();

            _siteDbConfigObject = new Mock<IObjectDb>();
            _siteDbConfigReader = new Mock<IDataReader>();

            var cacheMock = new Mock<IOptions<MemoryCacheOptions>>();
            cacheMock.Setup(x => x.Value).Returns(new MemoryCacheOptions { Clock = new SystemClock() });
            _memoryCache = new MemoryCache(cacheMock.Object);

            _appSetting = new Mock<IOptions<AppSetting>>();
        }

        [Theory] [AutoData]
        public async Task GetLicensee_WhenCall_ReturnValue(
            LicenseeInformation licenseeInformation,
            string licenseeName,
            string dbServer)
        {
            // Arrange
            _siteDbConfigReader.SetupToggleRead(new
            {
                licenseeInformation.SiteDBGroupID,
                AGIP = dbServer
            });
            SetupSiteDbMock();

            _objDb.Setup(x => x.QueryEntityAsync<LicenseeInformation>(It.IsAny<object>(), null, default))
                  .Returns(Task.FromResult(licenseeInformation));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeSelBySiteName", It.IsAny<int>())).Returns(_objDb.Object);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetLicensee(licenseeName);

            // Assert
            Assert.Equal(licenseeInformation, result);
            Assert.Equal(dbServer, result.SiteDBServer);
        }

        [Theory]
        [AutoData]
        public async Task GetLicensee_Cached_ReturnCacheValue(
            LicenseeInformation licenseeInformation,
            string licenseeName)
        {
            // Arrange
            _memoryCache.Set($"GetLicensee_{licenseeName}", licenseeInformation);
            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetLicensee(licenseeName);

            // Assert
            Assert.Equal(licenseeInformation, result);
        }

        [Theory]
        [AutoData]
        public async Task GetLicensee_NoSiteDBGroupID_SkipAssignDBInfo(
            LicenseeInformation licenseeInformation,
            string licenseeName)
        {
            // Arrange
            SetupSiteDbMock();

            licenseeInformation.SiteDBGroupID = 0;
            licenseeInformation.SiteDBServer = null;

            _objDb.Setup(x => x.QueryEntityAsync<LicenseeInformation>(It.IsAny<object>(), null, default))
                  .Returns(Task.FromResult(licenseeInformation));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeSelBySiteName", It.IsAny<int>())).Returns(_objDb.Object);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetLicensee(licenseeName);

            // Assert
            Assert.Equal(licenseeInformation, result);
            Assert.Null(result.SiteDBServer);
        }

        [Theory]
        [AutoData]
        public async Task GetLicensee_NotFoundSiteDBGroupID_AssignEmptyDBInfo(
            LicenseeInformation licenseeInformation,
            string licenseeName)
        {
            // Arrange
            SetupSiteDbMock();

            _objDb.Setup(x => x.ExecuteReader(null, null)).Returns(_reader.Object);
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeSelBySiteName", It.IsAny<int>())).Returns(_objDb.Object);
            _objDb.Setup(x => x.QueryEntityAsync<LicenseeInformation>(It.IsAny<object>(), null, default))
                  .Returns(Task.FromResult(licenseeInformation));

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetLicensee(licenseeName);

            // Assert
            Assert.Equal(licenseeInformation, result);
            Assert.Equal(string.Empty, result.SiteDBServer);
        }

        [Theory] [AutoData]
        public void IsFakeLicensee_FoundFakeLicensee_ReturnTrue(AppSetting setting, string licenseeName)
        {
            // Arrange
            setting.FakeLicensees = new List<string> { licenseeName };
            _appSetting.Setup(x => x.Value).Returns(setting);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = service.IsFakeLicensee(licenseeName);

            // Assert
            Assert.True(result);
        }

        [Theory] [AutoData]
        public void IsFakeLicensee_NotFoundFakeLicensee_ReturnFalse(AppSetting setting)
        {
            // Arrange
            const string licenseeName = "Any";

            setting.FakeLicensees = new List<string>();
            _appSetting.Setup(x => x.Value).Returns(setting);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = service.IsFakeLicensee(licenseeName);

            // Assert
            Assert.False(result);
        }

        [Theory] [AutoData]
        public void GetAuditHardCodeCurrencies_FoundHardCodeLicensee_ReturnTrue(
            AppSetting setting,
            AuditHardCodeCurrency codeCurrency,
            string licenseeName)
        {
            // Arrange
            codeCurrency.Licensee = licenseeName;
            setting.AuditHardCodeCurrencies = new List<AuditHardCodeCurrency> { codeCurrency };
            _appSetting.Setup(x => x.Value).Returns(setting);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = service.GetAuditHardCodeCurrencies(licenseeName);

            // Assert
            Assert.Equal(codeCurrency.AuditCurrencies, result);
        }

        [Theory] [AutoData]
        public void GetAuditHardCodeCurrencies_NotFoundHardCodeLicensee_ReturnEmpty(
            AppSetting setting,
            AuditHardCodeCurrency codeCurrency)
        {
            // Arrange
            const string licenseeName = "Any";

            setting.AuditHardCodeCurrencies = new List<AuditHardCodeCurrency> { codeCurrency };
            _appSetting.Setup(x => x.Value).Returns(setting);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = service.GetAuditHardCodeCurrencies(licenseeName);

            // Assert
            Assert.Empty(result);
        }

        [Theory] [AutoData]
        public void GetAuditHardCodeCurrencies_NullAuditCurrencies_ReturnEmpty(
            AppSetting setting,
            AuditHardCodeCurrency codeCurrency,
            string licenseeName)
        {
            // Arrange
            codeCurrency.Licensee = licenseeName;
            codeCurrency.AuditCurrencies = null;
            setting.AuditHardCodeCurrencies = new List<AuditHardCodeCurrency> { codeCurrency };
            _appSetting.Setup(x => x.Value).Returns(setting);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = service.GetAuditHardCodeCurrencies(licenseeName);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [AutoData]
        public async Task GetLicenseeBelongingLrfUser_WhenCall_ReturnValue(
            LicenseeInformation licenseeInformation,
            int lrfUserId)
        {
            // Arrange
            _siteDbConfigReader.SetupToggleRead(new
            {
                licenseeInformation.SiteDBGroupID,
                AGIP = licenseeInformation.SiteDBServer
            });
            SetupSiteDbMock();

            _reader.SetupToggleRead();

            const int licenseeIdOrdinal = 0;
            _reader.Setup(x => x.GetOrdinal("LicenseeID")).Returns(licenseeIdOrdinal);
            _reader.Setup(x => x.GetInt32(licenseeIdOrdinal)).Returns(licenseeInformation.Id);

            const int licenseeNameOrdinal = 1;
            _reader.Setup(x => x.GetOrdinal("LicenseeName")).Returns(licenseeNameOrdinal);
            _reader.Setup(x => x.GetString(licenseeNameOrdinal)).Returns(licenseeInformation.Name);

            const int siteDbGroupIdOrdinal = 2;
            _reader.Setup(x => x.GetOrdinal("SiteDBGroupID")).Returns(siteDbGroupIdOrdinal);
            _reader.Setup(x => x.GetInt16(siteDbGroupIdOrdinal)).Returns(licenseeInformation.SiteDBGroupID);

            const int isAllowedMemberCommOrdinal = 3;
            _reader.Setup(x => x.GetOrdinal("IsAllowedMemberComm")).Returns(isAllowedMemberCommOrdinal);
            _reader.Setup(x => x.GetBoolean(isAllowedMemberCommOrdinal))
                   .Returns(licenseeInformation.IsAllowedCustomerComm);

            const int enableBaOrdinal = 4;
            _reader.Setup(x => x.GetOrdinal("EnableBA")).Returns(enableBaOrdinal);
            _reader.Setup(x => x.GetBoolean(enableBaOrdinal)).Returns(licenseeInformation.IsEnableBA);

            _objDb.Setup(x => x.ExecuteReaderAsync(It.IsAny<object>(), null, default))
                  .Returns(Task.FromResult(_reader.Object));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeSel", It.IsAny<int>())).Returns(_objDb.Object);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetLicenseeBelongingLrfUser(licenseeInformation.Id, lrfUserId);

            // Assert
            Assert.Equal(licenseeInformation.Id, result.Id);
            Assert.Equal(licenseeInformation.SiteDBGroupID, result.SiteDBGroupID);
            Assert.Equal(licenseeInformation.IsAllowedCustomerComm, result.IsAllowedCustomerComm);
            Assert.Equal(licenseeInformation.IsEnableBA, result.IsEnableBA);
            Assert.Equal(licenseeInformation.Name, result.Name);
            Assert.Equal(licenseeInformation.SiteDBServer, result.SiteDBServer);
        }

        [Theory]
        [AutoData]
        public async Task GetLicenseeBelongingLrfUser_NotMatchLicenseeId_ReturnNull(
            LicenseeInformation licenseeInformation,
            int lrfUserId)
        {
            // Arrange
            const int licenseeId = 1;
            licenseeInformation.Id = 2;

            _reader.SetupToggleRead();
            const int licenseeIdOrdinal = 0;
            _reader.Setup(x => x.GetOrdinal("LicenseeID")).Returns(licenseeIdOrdinal);
            _reader.Setup(x => x.GetInt32(licenseeIdOrdinal)).Returns(licenseeId);

            _objDb.Setup(x => x.ExecuteReaderAsync(It.IsAny<object>(), null, default))
                  .Returns(Task.FromResult(_reader.Object));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeSel", It.IsAny<int>())).Returns(_objDb.Object);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetLicenseeBelongingLrfUser(licenseeInformation.Id, lrfUserId);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [AutoData]
        public async Task GetLicenseeBelongingLrfUser_Cached_ReturnCacheValue(
            LicenseeInformation licenseeInformation, int lrfUserId)
        {
            // Arrange
            _memoryCache.Set($"GetLicenseeBelongingLrfUser_{licenseeInformation.Id}_{lrfUserId}", licenseeInformation);
            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetLicenseeBelongingLrfUser(licenseeInformation.Id, lrfUserId);

            // Assert
            Assert.Equal(licenseeInformation, result);
        }

        [Fact]
        public async Task GetOfferedSubProducts_WithSubProducts_ReturnValue()
        {
            // Arrange
            var licenseeId = FakeHelper.Create<int>();

            _reader.SetupToggleRead(new List<object>
            {
                new { productprefix = "SLT" },
                new { productprefix = "ANY" },
                new { productprefix = "BTC" }
            });

            _objDb.Setup(x => x.ExecuteReaderAsync(null, null, default)).Returns(Task.FromResult(_reader.Object));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeProduct3rdSel", It.IsAny<int>())).Returns(_objDb.Object);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetOfferedSubProducts(licenseeId);

            // Assert
            _objDb.Verify(x => x.SetParameterValue("licenseeid", licenseeId));

            Assert.Equal("SLT,BTC", result);
        }

        [Fact]
        public async Task GetOfferedSubProducts_WithoutSubProducts_ReturnNull()
        {
            // Arrange
            var licenseeId = FakeHelper.Create<int>();

            _reader.SetupToggleRead(new { productprefix = "Any" });

            _objDb.Setup(x => x.ExecuteReaderAsync(null, null, default)).Returns(Task.FromResult(_reader.Object));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_LicenseeProduct3rdSel", It.IsAny<int>())).Returns(_objDb.Object);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetOfferedSubProducts(licenseeId);

            // Assert
            _objDb.Verify(x => x.SetParameterValue("licenseeid", licenseeId));

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetOfferedSubProducts_Cached_ReturnCacheValue()
        {
            // Arrange
            var cached = new List<string> { "SLT", "BTC" };
            var licenseeId = FakeHelper.Create<int>();
            _memoryCache.Set($"GetOfferedSubProducts_{licenseeId}", cached);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetOfferedSubProducts(licenseeId);
            var codes = result.Split(",");

            // Assert
            Assert.Equal(cached, codes);
        }

        [Fact]
        public void GetOfferOddGroups_WithNoOfferBy_ReturnValue()
        {
            // Arrange
            var licenseeName = FakeHelper.Create<string>();
            var oddsGroup = FakeHelper.CreateMany<OddsGroup>(x => x.OfferedBy, null).ToList();
            var appSetting = FakeHelper.Create<AppSetting>(x => x.OddsGroups, oddsGroup);
            _appSetting.Setup(x => x.Value).Returns(appSetting);

            var expected = oddsGroup.Select(x => x.Value).ToList();

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = service.GetOffereOddGroups(licenseeName);
            var codes = result.Split(",");

            // Assert
            Assert.Equal(expected, codes);
        }

        [Fact]
        public void GetOfferOddGroups_WithOfferBy_ReturnValue()
        {
            // Arrange
            var licenseeName = FakeHelper.Create<string>();
            var oddsGroup = new List<OddsGroup>
            {
                FakeHelper.Create<OddsGroup>(x => x.OfferedBy, FakeOffers(licenseeName)),
                FakeHelper.Create<OddsGroup>(x => x.OfferedBy, FakeOffers(licenseeName)),
                FakeHelper.Create<OddsGroup>(x => x.OfferedBy, FakeOffers(licenseeName))
            };
            var expected = oddsGroup.Select(x => x.Value).ToList();
            oddsGroup.Add(FakeHelper.Create<OddsGroup>());

            var appSetting = FakeHelper.Create<AppSetting>(x => x.OddsGroups, oddsGroup);
            _appSetting.Setup(x => x.Value).Returns(appSetting);


            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = service.GetOffereOddGroups(licenseeName);
            var codes = result.Split(",");

            // Assert
            Assert.Equal(expected, codes);
        }

        [Fact]
        public void GetOfferOddGroups_WhenCall_ReturnValue()
        {
            // Arrange
            var licenseeName = FakeHelper.Create<string>();
            var oddsGroup = new List<OddsGroup>
            {
                FakeHelper.Create<OddsGroup>(x => x.OfferedBy, FakeOffers(licenseeName)),
                FakeHelper.Create<OddsGroup>(x => x.OfferedBy, null),
                FakeHelper.Create<OddsGroup>(x => x.OfferedBy, FakeOffers(licenseeName))
            };
            var expected = oddsGroup.Select(x => x.Value).ToList();
            oddsGroup.Add(FakeHelper.Create<OddsGroup>());

            var appSetting = FakeHelper.Create<AppSetting>(x => x.OddsGroups, oddsGroup);
            _appSetting.Setup(x => x.Value).Returns(appSetting);


            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = service.GetOffereOddGroups(licenseeName);
            var codes = result.Split(",");

            // Assert
            Assert.Equal(expected, codes);
        }

        private static List<string> FakeOffers(string licenseeName)
        {
            var offers = FakeHelper.CreateMany<string>().ToList();
            offers.Add(licenseeName);
            return offers;
        }

        [Theory] [AutoData]
        public async Task GetCurrencyAgentListByLrfUser_WhenCall_ReturnValue(int licenseeId, int userId)
        {
            // Arrange
            var lrfCurrencyAgents = new List<LrfCurrencyAgent>
            {
                FakeHelper.Create<LrfCurrencyAgent>(x => x.IsActive, true),
                FakeHelper.Create<LrfCurrencyAgent>(x => x.IsActive, false)
            };

            _objDb.Setup(x => x.QueryAsync<LrfCurrencyAgent>(It.IsAny<object>(), null, default))
                  .Returns(Task.FromResult(lrfCurrencyAgents.AsEnumerable()));
            _dbFactory.Setup(x => x.GetObjectDb("Lrf_SelCurrencyByLicenseeUser", It.IsAny<int>()))
                      .Returns(_objDb.Object);

            var expected = new CurrencyAgent
            {
                CurrencyId = lrfCurrencyAgents[0].CurrencyId,
                Currency = lrfCurrencyAgents[0].CurrencyName,
                AgentId = new[] { lrfCurrencyAgents[0].AgentId }
            };

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetCurrencyAgentListByLrfUser(licenseeId, userId);
            var actual = result.ToList();
            var count = actual.Count;

            // Assert
            Assert.Equal(expected.CurrencyId, actual[0].CurrencyId);
            Assert.Equal(expected.Currency, actual[0].Currency);
            Assert.Equal(expected.AgentId, actual[0].AgentId);
            Assert.Equal(1, count);
        }

        [Theory]
        [InlineData("Any", "Any")]
        [InlineData(null, "")]
        public async Task GetLapUserInfo_WhenCall_ReturnValue(string baseCurrency, string expectedBaseCurrency)
        {
            // Arrange
            var userSEQ = FakeHelper.Create<int>();
            var vendorId = FakeHelper.Create<string>();
            var lapUserInfo = FakeHelper.Create<LapUserInfo>(x => x.BaseCurrency, baseCurrency);
            _objDb.Setup(x => x.GetParameterValue("UserLevel")).Returns(lapUserInfo.UserLevel);
            _objDb.Setup(x => x.GetParameterValue("SubsidiaryEnabled")).Returns(lapUserInfo.IsEnabledSubsidiary);
            _objDb.Setup(x => x.GetParameterValue("OperatorID")).Returns(lapUserInfo.OperatorID);
            _objDb.Setup(x => x.GetParameterValue("BaseCurrency")).Returns(lapUserInfo.BaseCurrency);

            _dbFactory.Setup(x => x.GetObjectDb("Lrf_GetUserLevelInfo", It.IsAny<int>())).Returns(_objDb.Object);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.GetLapUserInfo(userSEQ, vendorId);

            // Assert
            _objDb.Verify(x => x.SetParameterValue("VendorID", vendorId));
            _objDb.Verify(x => x.SetParameterValue("UserSEQ", userSEQ));
            Assert.Equal(lapUserInfo.UserLevel, result.UserLevel);
            Assert.Equal(lapUserInfo.IsEnabledSubsidiary, result.IsEnabledSubsidiary);
            Assert.Equal(lapUserInfo.OperatorID, result.OperatorID);
            Assert.Equal(expectedBaseCurrency, result.BaseCurrency);
        }

        [Theory]
        [InlineData(false, "Production")]
        [InlineData(true, "UAT")]
        public async Task IsEnableSingleWallet_NotCached_ReturnFalse(bool isUAT, string environment)
        {
            // Arrange
            const string keyName = "EnableSingleWallet";

            var licenseeName = FakeHelper.Create<string>();
            var appSetting = FakeHelper.Create<AppSetting>(x => x.IsUATTW, isUAT);
            _appSetting.Setup(x => x.Value).Returns(appSetting);

            _dbFactory.Setup(x => x.GetObjectDb("Lrf_GetConfigDetailByKeyName", It.IsAny<int>()))
                      .Returns(_objDb.Object);

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.IsEnableSingleWallet(licenseeName);

            // Assert
            _objDb.Verify(x => x.SetParameterValue("SiteName", licenseeName));
            _objDb.Verify(x => x.SetParameterValue("Environment", environment));
            _objDb.Verify(x => x.SetParameterValue("KeyName", keyName));

            Assert.False(result);
        }

        [Theory]
        [InlineData(false, "Production")]
        [InlineData(true, "UAT")]
        public async Task IsEnableSingleWallet_Cached_ReturnTrue(bool isUAT, string environment)
        {
            // Arrange
            const string keyName = "EnableSingleWallet";

            var licenseeName = FakeHelper.Create<string>();
            var keyValue = FakeHelper.Create<string>();
            var appSetting = FakeHelper.Create<AppSetting>(x => x.IsUATTW, isUAT);
            _appSetting.Setup(x => x.Value).Returns(appSetting);
            _objDb.Setup(x => x.GetParameterValue("KeyValue")).Returns(keyValue);

            _dbFactory.Setup(x => x.GetObjectDb("Lrf_GetConfigDetailByKeyName", It.IsAny<int>()))
                      .Returns(_objDb.Object);
            _memoryCache.Set($"{keyName}_{licenseeName}_{environment}", "true");

            var service = new ExtraInformationService(_dbFactory.Object, _memoryCache, _appSetting.Object);

            // Act
            var result = await service.IsEnableSingleWallet(licenseeName);

            // Assert
            Assert.True(result);
        }

        #region TestHelpers

        private void SetupSiteDbMock()
        {
            _siteDbConfigObject.Setup(x => x.ExecuteReader(null, null)).Returns(_siteDbConfigReader.Object);
            _dbFactory.Setup(x => x.GetObjectDb("Acc_SiteDBConfigSel", It.IsAny<int>()))
                      .Returns(_siteDbConfigObject.Object);
        }

        #endregion
    }
}
