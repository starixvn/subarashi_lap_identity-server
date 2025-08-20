namespace Fanex.Athena.IdentityServer.Core.UnitTests.Validators
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Constants;
    using Core.Services;
    using Core.Settings;
    using Core.Validators;
    using IdentityServer.UnitTests.Common;
    using IdentityServer4.Models;
    using IdentityServer4.Stores;
    using IdentityServer4.Validation;
    using Microsoft.Extensions.Logging;
    using Models;
    using Moq;
    using Xunit;

    public class LrfPasswordDelegationValidatorTest
    {
        private readonly ExtensionGrantValidationContext _context;
        private readonly Mock<IExtraInformationService> _extraInformationService;
        private readonly Mock<ILogger<LrfPasswordDelegationValidator>> _logger;
        private readonly Mock<ILrfAuthService> _lrfAuthService;
        private readonly Mock<ILrfFunctionService> _lrfFunctionService;
        private readonly Mock<IReferenceTokenStore> _referenceTokenStore;

        public LrfPasswordDelegationValidatorTest()
        {
            _logger = new Mock<ILogger<LrfPasswordDelegationValidator>>();
            _lrfAuthService = new Mock<ILrfAuthService>();
            _lrfFunctionService = new Mock<ILrfFunctionService>();
            _extraInformationService = new Mock<IExtraInformationService>();
            _referenceTokenStore = new Mock<IReferenceTokenStore>();

            _context = new ExtensionGrantValidationContext
            {
                Request = new ValidatedTokenRequest
                {
                    Raw = new NameValueCollection()
                }
            };
        }

        [Fact]
        public async Task ValidateAsync_NotRenewToken_ValidatePassword()
        {
            // Arrange
            _context.Request.Raw.Add("is_renew_token", "Any");

            const string username = "username";
            _context.Request.Raw.Add("username", username);

            const string password = "password";
            _context.Request.Raw.Add("password", password);

            const string loginToken = "loginToken";
            _context.Request.Raw.Add("login_token", loginToken);

            const string sessionId = "session_id";
            _context.Request.Raw.Add("session_id", sessionId);

            const string clientIP = "clientIP";
            _context.Request.Raw.Add("client_ip", clientIP);

            var userInfo = FakeHelper.Create<LrfUserInfo>();
            _lrfAuthService.Setup(x => x.ValidateUser(
                    It.Is<string>(_ => _.Equals(username)),
                    It.Is<string>(_ => _.Equals(password)),
                    It.Is<string>(_ => _.Equals(loginToken)),
                    It.Is<string>(_ => _.Equals(clientIP))))
                .Returns(Task.FromResult((0, "", userInfo)));
            var licenseeInfo = FakeHelper.Create<LicenseeInformation>();
            _extraInformationService.Setup(x => x.GetLicenseeBelongingLrfUser(userInfo.LicenseeId, userInfo.UserID))
                .Returns(Task.FromResult(licenseeInfo));
            SetupExtraInformationServices(userInfo.LicenseeId, licenseeInfo);

            const string accessToken = "AnyToken";
            _context.Request.Raw.Add("access_token", accessToken);
            _context.Request.Raw.Add("lang_code", "2");

            var tokenInfo = new Token();
            var clientId = FakeHelper.Create<int>().ToString();
            tokenInfo.ClientId = clientId;
            _context.Request.ClientId = clientId;

            _referenceTokenStore.Setup(x => x.GetReferenceTokenAsync(It.Is<string>(_ => _.Equals(accessToken))))
                .Returns(Task.FromResult(tokenInfo));

            var validator = new LrfPasswordDelegationValidator(_referenceTokenStore.Object, _lrfAuthService.Object,
                _lrfFunctionService.Object,
                _extraInformationService.Object, _logger.Object);
            // Act
            await validator.ValidateAsync(_context);

            // Assert
            Assert.NotNull(_context.Result.Subject.Identity);
            Assert.Equal(CustomGrantType.LrfPassword, _context.Result.Subject.Identity.AuthenticationType);
        }

        [Fact]
        public async Task ValidateAsync_NotRenewToken_ValidateError_BindContextError()
        {
            // Arrange
            _context.Request.Raw.Add("is_renew_token", "Any");

            var userInfo = FakeHelper.Create<LrfUserInfo>();
            _lrfAuthService.Setup(x => x.ValidateUser(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult((1, "", userInfo)));

            var validator = new LrfPasswordDelegationValidator(_referenceTokenStore.Object, _lrfAuthService.Object,
                _lrfFunctionService.Object,
                _extraInformationService.Object, _logger.Object);
            // Act
            await validator.ValidateAsync(_context);

            // Assert
            Assert.True(_context.Result.IsError);
        }

        [Fact]
        public async Task ValidateAsync_RenewToken_ValidateLicenseeId()
        {
            // Arrange
            _context.Request.Raw.Add("is_renew_token", "1");
            const int licenseeId = 10;
            _context.Request.Raw.Add("licensee_id", licenseeId.ToString());

            const string accessToken = "AnyToken";
            _context.Request.Raw.Add("access_token", accessToken);
            _context.Request.Raw.Add("lang_code", "2");

            var tokenInfo = new Token();
            var clientId = FakeHelper.Create<int>().ToString();
            tokenInfo.ClientId = clientId;
            _context.Request.ClientId = clientId;

            _referenceTokenStore.Setup(x => x.GetReferenceTokenAsync(It.Is<string>(_ => _.Equals(accessToken))))
                .Returns(Task.FromResult(tokenInfo));

            const int userId = 5;
            tokenInfo.Claims.Add(new Claim(CustomClaimType.UserSEQ, userId.ToString()));
            tokenInfo.Claims.Add(new Claim(CustomClaimType.UserID, "3"));
            SetupUserClaim(tokenInfo.Claims);

            var licenseeInfo = FakeHelper.Create<LicenseeInformation>();
            _extraInformationService.Setup(x => x.GetLicenseeBelongingLrfUser(licenseeId, userId))
                .Returns(Task.FromResult(licenseeInfo));
            SetupExtraInformationServices(userId, licenseeInfo);

            var validator = new LrfPasswordDelegationValidator(_referenceTokenStore.Object, _lrfAuthService.Object,
                _lrfFunctionService.Object,
                _extraInformationService.Object, _logger.Object);
            // Act
            await validator.ValidateAsync(_context);

            // Assert
            Assert.NotNull(_context.Result.Subject.Identity);
            Assert.Equal(CustomGrantType.LrfPassword, _context.Result.Subject.Identity.AuthenticationType);
        }

        [Fact]
        public async Task ValidateAsync_RenewToken_NullAccessToken_BindErrorContext()
        {
            // Arrange
            _context.Request.Raw.Add("is_renew_token", "1");

            var validator = new LrfPasswordDelegationValidator(_referenceTokenStore.Object, _lrfAuthService.Object,
                _lrfFunctionService.Object,
                _extraInformationService.Object, _logger.Object);
            // Act
            await validator.ValidateAsync(_context);

            // Assert
            Assert.True(_context.Result.IsError);
        }

        #region TestHelpers

        private void SetupExtraInformationServices(int userId, LicenseeInformation licenseeInfo)
        {
            _extraInformationService.Setup(x => x.GetOfferedSubProducts(licenseeInfo.Id))
                .Returns(Task.FromResult(FakeHelper.Create<string>()));

            _extraInformationService.Setup(x => x.IsEnableSingleWallet(It.Is<string>(_ => _.Equals(licenseeInfo.Name))))
                .Returns(Task.FromResult(true));

            _extraInformationService.Setup(x => x.GetCurrencyAgentListByLrfUser(licenseeInfo.Id, userId))
                .Returns(Task.FromResult(FakeHelper.CreateMany<CurrencyAgent>()));

            _lrfFunctionService.Setup(x => x.GetUserFunctions(licenseeInfo.Id, userId))
                .Returns(Task.FromResult(FakeHelper.CreateMany<LrfFunction>()));

            _extraInformationService.Setup(x => x.GetOffereOddGroups(It.Is<string>(_ => _.Equals(licenseeInfo.Name))))
                .Returns(FakeHelper.Create<string>());

            _extraInformationService.Setup(x => x.IsOfferShowIP(It.Is<string>(_ => _.Equals(licenseeInfo.Name))))
                .Returns(FakeHelper.Create<bool>());
            _extraInformationService.Setup(x => x.IsFakeLicensee(It.Is<string>(_ => _.Equals(licenseeInfo.Name))))
                .Returns(FakeHelper.Create<bool>());

            _extraInformationService.Setup(x => x.GetAuditHardCodeCurrencies(licenseeInfo.Name))
                .Returns(FakeHelper.CreateMany<AuditCurrency>());

            _extraInformationService.Setup(x => x.GetSpecialPlatformByLicenseeName(licenseeInfo.Name))
                .Returns(Task.FromResult(FakeHelper.Create<LrfSpecialPlatform>()));
        }

        private void SetupUserClaim(ICollection<Claim> claims)
        {
            claims.Add(new Claim(CustomClaimType.Username, FakeHelper.Create<string>()));
            claims.Add(new Claim(CustomClaimType.BaseCurrency, FakeHelper.Create<string>()));
            claims.Add(new Claim(CustomClaimType.OnUserId, FakeHelper.Create<string>()));
            claims.Add(new Claim(CustomClaimType.IsInternal, FakeHelper.Create<string>()));
            claims.Add(new Claim(CustomClaimType.UserLevel, FakeHelper.Create<string>()));
            claims.Add(new Claim(CustomClaimType.ClientIP, FakeHelper.Create<string>()));
            claims.Add(new Claim(CustomClaimType.Administrator, FakeHelper.Create<string>()));
        }

        #endregion
    }
}