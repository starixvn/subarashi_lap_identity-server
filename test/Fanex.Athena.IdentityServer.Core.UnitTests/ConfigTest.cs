namespace Fanex.Athena.IdentityServer.Core.UnitTests
{
    using System.Linq;
    using Constants;
    using IdentityModel;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Xunit;

    public class ConfigTest
    {
        [Fact]
        public void Ids_ReturnsIdentityResources_ContainsOpenIdAndUserInfo()
        {
            var result = Config.Ids.ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Name == "openid");
            Assert.Contains(result, r => r.Name == Config.UserInfoResource.IdentityName);
        }

        [Fact]
        public void ScopeApis_ReturnsApiScopes_ContainsExpectedScopes()
        {
            var result = Config.ScopeApis.ToList();

            Assert.Equal(8, result.Count);

            var expectedScopes = new[]
            {
                "playeradmin_api.external",
                "playeradmin_api.internal",
                "betticket_api",
                "lrf",
                "statement_api",
                "analytics_api",
                "report_api",
                "betlist_api"
            };

            foreach (var expectedScope in expectedScopes)
            {
                Assert.Contains(result, s => s.Name == expectedScope);
            }
        }

        [Fact]
        public void ScopeApis_PlayerAdminExternal_ContainsExpectedClaims()
        {
            var result = Config.ScopeApis.ToList();
            var playerAdminExternal = result.First(s => s.Name == "playeradmin_api.external");

            Assert.Contains(JwtClaimTypes.Name, playerAdminExternal.UserClaims);
            Assert.Contains(JwtClaimTypes.Role, playerAdminExternal.UserClaims);
            Assert.Contains(CustomClaimType.SiteName, playerAdminExternal.UserClaims);
            Assert.Contains(CustomClaimType.Administrator, playerAdminExternal.UserClaims);
            Assert.Contains(CustomClaimType.CurrencyAgent, playerAdminExternal.UserClaims);
            Assert.Contains(CustomClaimType.IsShowIPVN, playerAdminExternal.UserClaims);
        }

        [Fact]
        public void ScopeApis_LrfScope_ContainsExpectedClaims()
        {
            var result = Config.ScopeApis.ToList();
            var lrfScope = result.First(s => s.Name == "lrf");

            Assert.Contains(JwtClaimTypes.Name, lrfScope.UserClaims);
            Assert.Contains(CustomClaimType.VendorID, lrfScope.UserClaims);
            Assert.Contains(CustomClaimType.DefaultReportUrl, lrfScope.UserClaims);
            Assert.Contains(CustomClaimType.AuditCurrencies, lrfScope.UserClaims);
        }

        [Fact]
        public void ScopeApis_BetTicketApi_ContainsMinimalClaims()
        {
            var result = Config.ScopeApis.ToList();
            var betTicketScope = result.First(s => s.Name == "betticket_api");

            Assert.Contains(JwtClaimTypes.Name, betTicketScope.UserClaims);
            Assert.Contains(CustomClaimType.CurrencyAgent, betTicketScope.UserClaims);
            Assert.Contains(CustomClaimType.ClientIP, betTicketScope.UserClaims);

            // Should not contain some claims that other APIs have
            Assert.DoesNotContain(CustomClaimType.Administrator, betTicketScope.UserClaims);
            Assert.DoesNotContain(CustomClaimType.VendorID, betTicketScope.UserClaims);
        }

        [Fact]
        public void GetApis_WithValidConfiguration_CallsGetSection()
        {
            var mockConfig = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();

            mockConfig.Setup(c => c.GetSection("IdentityServer:ApiResources"))
                     .Returns(mockSection.Object);

            var result = Config.GetApis(mockConfig.Object);

            mockConfig.Verify(c => c.GetSection("IdentityServer:ApiResources"), Times.Once);
        }

        [Fact]
        public void GetClients_WithValidConfiguration_CallsGetSection()
        {
            var mockConfig = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();

            mockConfig.Setup(c => c.GetSection("IdentityServer:Clients"))
                     .Returns(mockSection.Object);

            var result = Config.GetClients(mockConfig.Object);

            mockConfig.Verify(c => c.GetSection("IdentityServer:Clients"), Times.Once);
        }

        [Fact]
        public void UserInfoResource_Constructor_SetsCorrectProperties()
        {
            var userInfoResource = new Config.UserInfoResource();

            Assert.Equal(Config.UserInfoResource.IdentityName, userInfoResource.Name);
            Assert.NotNull(userInfoResource.UserClaims);
            Assert.True(userInfoResource.UserClaims.Count > 0);
        }

        [Fact]
        public void UserInfoResource_ContainsBaseUserClaims()
        {
            var userInfoResource = new Config.UserInfoResource();

            Assert.Contains(JwtClaimTypes.Name, userInfoResource.UserClaims);
            Assert.Contains(JwtClaimTypes.Role, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.SiteName, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.UserSEQ, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.UserID, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.Language, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.Username, userInfoResource.UserClaims);
        }

        [Fact]
        public void UserInfoResource_ContainsExtendedClaims()
        {
            var userInfoResource = new Config.UserInfoResource();

            Assert.Contains(CustomClaimType.VendorID, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.Administrator, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.CurrencyAgent, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.AuditCurrencies, userInfoResource.UserClaims);
            Assert.Contains(CustomClaimType.SpecialPlatform, userInfoResource.UserClaims);
        }

        [Fact]
        public void ScopeApis_AllScopesContainBaseClaims()
        {
            var scopes = Config.ScopeApis.ToList();
            var expectedBaseClaims = new[]
            {
                JwtClaimTypes.Name,
                JwtClaimTypes.Role,
                CustomClaimType.SiteName,
                CustomClaimType.UserSEQ,
                CustomClaimType.UserID,
                CustomClaimType.Language,
                CustomClaimType.Username
            };

            foreach (var scope in scopes)
            {
                foreach (var baseClaim in expectedBaseClaims)
                {
                    Assert.Contains(baseClaim, scope.UserClaims);
                }
            }
        }

        [Fact]
        public void UserInfoResource_IdentityName_IsConstant()
        {
            Assert.Equal("userinfo_resource", Config.UserInfoResource.IdentityName);
        }

        [Fact]
        public void ScopeApis_AnalyticsApi_ContainsSpecialPlatformType()
        {
            var result = Config.ScopeApis.ToList();
            var analyticsScope = result.First(s => s.Name == "analytics_api");

            Assert.Contains(CustomClaimType.SpecialPlatformType, analyticsScope.UserClaims);
        }

        [Fact]
        public void ScopeApis_StatementApi_ContainsIsShowIPVN()
        {
            var result = Config.ScopeApis.ToList();
            var statementScope = result.First(s => s.Name == "statement_api");

            Assert.Contains(CustomClaimType.IsShowIPVN, statementScope.UserClaims);
        }
    }
}
