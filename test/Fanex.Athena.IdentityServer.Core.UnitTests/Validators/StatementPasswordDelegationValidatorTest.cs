namespace Fanex.Athena.IdentityServer.Core.UnitTests.Validators
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Constants;
    using Core.Services;
    using Core.Settings;
    using Core.Validators;
    using IdentityModel;
    using IdentityServer.UnitTests.Common.DataAttributes;
    using IdentityServer4.Validation;
    using Models;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    public class StatementPasswordDelegationValidatorTest
    {
        [Theory]
        [AutoMoqData]
        public async Task ValidateAsync_LicenseeIsNull_ThenGrantValidationResultIsInvalidRequestError(
            string siteName,
            string userName,
            string isShowIpVn,
            [Frozen] Mock<IExtraInformationService> extraInformationService,
            StatementPasswordDelegationValidator validator)
        {
            var context = new ExtensionGrantValidationContext
            {
                Request = new ValidatedTokenRequest
                {
                    Raw = new NameValueCollection
                    {
                        { "sitename", siteName },
                        { "username", userName },
                        { "isshowipvn", isShowIpVn }
                    }
                }
            };
            extraInformationService.Setup(x => x.GetLicensee(siteName))
                .ReturnsAsync((LicenseeInformation)null);

            await validator.ValidateAsync(context);

            Assert.Equal("invalid_request", context.Result.Error);
        }

        [Theory]
        [AutoMoqData]
        public async Task ValidateAsync_InputValidData_ThenGrantValidationResultIsCorrect(
            string siteName,
            string userName,
            string isShowIpVn,
            LicenseeInformation licenseeInfo,
            string offeredSubProducts,
            bool isEnableSingleWallet,
            List<CurrencyAgent> currencyAgents,
            LrfSpecialPlatform lrfSpecialPlatform,
            string offereOddGroups,
            bool isOfferShowIP,
            bool isFakeLicensee,
            List<AuditCurrency> auditHardCodeCurrencies,
            [Frozen] Mock<IExtraInformationService> extraInformationService,
            StatementPasswordDelegationValidator validator)
        {
            var context = new ExtensionGrantValidationContext
            {
                Request = new ValidatedTokenRequest
                {
                    Raw = new NameValueCollection
                    {
                        { "sitename", siteName },
                        { "username", userName },
                        { "isshowipvn", isShowIpVn }
                    }
                }
            };
            extraInformationService.Setup(x => x.GetLicensee(siteName))
                .ReturnsAsync(licenseeInfo);
            extraInformationService.Setup(x => x.GetOfferedSubProducts(licenseeInfo.Id))
                .ReturnsAsync(offeredSubProducts);
            extraInformationService.Setup(x => x.IsEnableSingleWallet(licenseeInfo.Name))
                .ReturnsAsync(isEnableSingleWallet);
            extraInformationService.Setup(x => x.GetCurrencyAgentForExtService(licenseeInfo.Name, It.IsAny<int>()))
                .ReturnsAsync(currencyAgents);
            extraInformationService.Setup(x => x.GetSpecialPlatformByLicenseeName(licenseeInfo.Name))
                .ReturnsAsync(lrfSpecialPlatform);
            extraInformationService.Setup(x => x.GetOffereOddGroups(licenseeInfo.Name))
                .Returns(offereOddGroups);
            extraInformationService.Setup(x => x.IsOfferShowIP(licenseeInfo.Name))
                .Returns(isOfferShowIP);
            extraInformationService.Setup(x => x.IsFakeLicensee(licenseeInfo.Name))
                .Returns(isFakeLicensee);
            extraInformationService.Setup(x => x.GetAuditHardCodeCurrencies(licenseeInfo.Name))
                .Returns(auditHardCodeCurrencies);
            var identityName = $"ext_{siteName}_{userName}";

            await validator.ValidateAsync(context);

            var claims = context.Result.Subject.Claims;
            Assert.Equal(identityName, claims.First(x => x.Type == JwtClaimTypes.Name).Value);
            Assert.Equal(Roles.StatementServiceRoleUser, claims.First(x => x.Type == JwtClaimTypes.Role).Value);
            Assert.Equal(isShowIpVn, claims.First(x => x.Type == CustomClaimType.IsShowIPVN).Value);
            Assert.Equal(licenseeInfo.Id.ToString(), claims.First(x => x.Type == CustomClaimType.LicenseeId).Value);
            Assert.Equal(licenseeInfo.Name, claims.First(x => x.Type == CustomClaimType.SiteName).Value);
            Assert.Equal(lrfSpecialPlatform.SpecialPlatform.ToString(), claims.First(x => x.Type == CustomClaimType.SpecialPlatform).Value);
            Assert.Equal(lrfSpecialPlatform.SpecialPlatformType.ToString(), claims.First(x => x.Type == CustomClaimType.SpecialPlatformType).Value);
            Assert.Equal(licenseeInfo.SiteDBServer, claims.First(x => x.Type == CustomClaimType.SiteDBServer).Value);
            Assert.Equal(offeredSubProducts, claims.First(x => x.Type == CustomClaimType.OfferedSubProducts).Value);
            Assert.Equal(offereOddGroups, claims.First(x => x.Type == CustomClaimType.OfferedOddsGroups).Value);
            Assert.Equal(licenseeInfo.IsAllowedCustomerComm.ToString(), claims.First(x => x.Type == CustomClaimType.IsAllowedCustomerComm).Value);
            Assert.Equal(isEnableSingleWallet.ToString(), claims.First(x => x.Type == CustomClaimType.IsEnableSingleWallet).Value);
            Assert.Equal(isOfferShowIP.ToString(), claims.First(x => x.Type == CustomClaimType.IsShowIP).Value);
            Assert.Equal(isFakeLicensee.ToString(), claims.First(x => x.Type == CustomClaimType.IsFakeLicensee).Value);
            Assert.Equal(JsonConvert.SerializeObject(auditHardCodeCurrencies), claims.First(x => x.Type == CustomClaimType.AuditCurrencies).Value);
            Assert.Equal(JsonConvert.SerializeObject(currencyAgents), claims.First(x => x.Type == CustomClaimType.CurrencyAgent).Value);
        }
    }
}
