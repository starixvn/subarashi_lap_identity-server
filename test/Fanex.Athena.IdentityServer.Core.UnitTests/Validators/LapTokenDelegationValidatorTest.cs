namespace Fanex.Athena.IdentityServer.Core.UnitTests.Validators
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading.Tasks;
    using Constants;
    using Core.Services;
    using Core.Validators;
    using IdentityServer.UnitTests.Common;
    using IdentityServer4.Stores;
    using IdentityServer4.Validation;
    using LAP.ONEBook.Security;
    using Microsoft.Extensions.Logging;
    using Models;
    using Moq;
    using Settings;
    using Xunit;

    public class LapTokenDelegationValidatorTest
    {
        private readonly Mock<IAuth<DefaultAuthInfo<LapFunction, CurrencyAgent>>> _authService;

        private readonly ExtensionGrantValidationContext _context;
        private readonly Mock<IExtraInformationService> _extraInformationService;
        private readonly Mock<ILogger<LapTokenDelegationValidator>> _logger;
        private readonly Mock<IReferenceTokenStore> _referenceTokenStore;

        public LapTokenDelegationValidatorTest()
        {
            _logger = new Mock<ILogger<LapTokenDelegationValidator>>();
            _authService = new Mock<IAuth<DefaultAuthInfo<LapFunction, CurrencyAgent>>>();
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
        public async Task ValidateAsync_WhenCall_BindGrantValidationResult()
        {
            // Arrange
            var authInfo = FakeHelper.Create<DefaultAuthInfo<LapFunction, CurrencyAgent>>();
            _authService.Setup(x => x.AuthenticateAsync(It.IsAny<string>())).Returns(Task.FromResult(authInfo));

            SetupExtraInformationServices(authInfo);

            var validator = new LapTokenDelegationValidator(_logger.Object, _authService.Object,
                _extraInformationService.Object,
                _referenceTokenStore.Object);

            // Act
            await validator.ValidateAsync(_context);

            // Assert
            Assert.NotNull(_context.Result.Subject.Identity);
            Assert.Equal(_context.Result.Subject.Identity.AuthenticationType, validator.GrantType);
        }

        [Fact]
        public async Task ValidateAsync_AuthAdmin_BindGrantValidationResult()
        {
            // Arrange
            var authInfo = FakeHelper.Create<DefaultAuthInfo<LapFunction, CurrencyAgent>>(x => x.IsAdminstrator, true);
            _authService.Setup(x => x.AuthenticateAsync(It.IsAny<string>())).Returns(Task.FromResult(authInfo));

            SetupExtraInformationServices(authInfo);

            var validator = new LapTokenDelegationValidator(_logger.Object, _authService.Object,
                _extraInformationService.Object,
                _referenceTokenStore.Object);

            // Act
            await validator.ValidateAsync(_context);
            var adminClaim = _context.Result.Subject.Claims.First(x => x.Value.Equals("super_admin"));

            // Assert
            Assert.NotNull(_context.Result.Subject.Identity);
            Assert.Equal(_context.Result.Subject.Identity.AuthenticationType, validator.GrantType);

            Assert.NotNull(adminClaim);
        }

        [Fact]
        public async Task ValidateAsync_OldToken_RevokeOldToken()
        {
            // Arrange
            const string token = "AnyToken";
            _context.Request.Raw.Add("access_token", token);

            var authInfo = FakeHelper.Create<DefaultAuthInfo<LapFunction, CurrencyAgent>>();
            _authService.Setup(x => x.AuthenticateAsync(It.IsAny<string>())).Returns(Task.FromResult(authInfo));

            SetupExtraInformationServices(authInfo);

            var validator = new LapTokenDelegationValidator(_logger.Object, _authService.Object,
                _extraInformationService.Object,
                _referenceTokenStore.Object);

            // Act
            await validator.ValidateAsync(_context);

            // Assert
            _referenceTokenStore.Verify(x => x.RemoveReferenceTokenAsync(token));
        }

        [Fact]
        public async Task ValidateAsync_HadClientIp_AddClientIpClaim()
        {
            // Arrange
            const string clientIp = "Any";
            _context.Request.Raw.Add("client_ip", clientIp);

            var authInfo = FakeHelper.Create<DefaultAuthInfo<LapFunction, CurrencyAgent>>();
            _authService.Setup(x => x.AuthenticateAsync(It.IsAny<string>())).Returns(Task.FromResult(authInfo));

            SetupExtraInformationServices(authInfo);

            var validator = new LapTokenDelegationValidator(_logger.Object, _authService.Object,
                _extraInformationService.Object,
                _referenceTokenStore.Object);

            // Act
            await validator.ValidateAsync(_context);
            var claim = _context.Result.Subject.Claims.First(x => x.Type == CustomClaimType.ClientIP);

            // Assert
            Assert.Equal(clientIp, claim.Value);
        }

        [Fact]
        public async Task ValidateAsync_InvalidToken_BindInvalidRequest()
        {
            // Arrange
            const string token = "AnyToken";
            _context.Request.Raw.Add("token", token);

            var authInfo = FakeHelper.Create<DefaultAuthInfo<LapFunction, CurrencyAgent>>(x => x.SiteName, null);
            _authService.Setup(x => x.AuthenticateAsync(It.IsAny<string>())).Returns(Task.FromResult(authInfo));

            var validator = new LapTokenDelegationValidator(_logger.Object, _authService.Object,
                _extraInformationService.Object,
                _referenceTokenStore.Object);

            // Act
            await validator.ValidateAsync(_context);
            var isErrorToken = _context.Result.ErrorDescription.Contains(token);

            // Assert
            Assert.True(isErrorToken);
            _logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(token) &&
                                                  v.ToString().Contains(authInfo.UserName) &&
                                                  v.ToString().Contains(authInfo.UserSEQ.ToString())),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        [Fact]
        public async Task ValidateAsync_NullLicenseeInfo_BindInvalidRequest()
        {
            // Arrange
            var authInfo = FakeHelper.Create<DefaultAuthInfo<LapFunction, CurrencyAgent>>();
            _authService.Setup(x => x.AuthenticateAsync(It.IsAny<string>())).Returns(Task.FromResult(authInfo));

            _extraInformationService.Setup(x => x.GetLicensee(authInfo.SiteName))
                .Returns(Task.FromResult<LicenseeInformation>(null));

            var validator = new LapTokenDelegationValidator(_logger.Object, _authService.Object,
                _extraInformationService.Object,
                _referenceTokenStore.Object);

            // Act
            await validator.ValidateAsync(_context);

            // Assert
            Assert.True(_context.Result.IsError);
            _logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()
                        .Contains($"Licensee {authInfo.SiteName} is not existed")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
        }

        #region TestHelpers

        private void SetupExtraInformationServices(DefaultAuthInfo<LapFunction, CurrencyAgent> authInfo)
        {
            var licenseeInfo = FakeHelper.Create<LicenseeInformation>();
            _extraInformationService.Setup(x => x.GetLicensee(authInfo.SiteName))
                .Returns(Task.FromResult(licenseeInfo));

            _extraInformationService.Setup(x => x.IsEnableSingleWallet(authInfo.SiteName))
                .Returns(Task.FromResult(true));

            _extraInformationService.Setup(x => x.GetOfferedSubProducts(licenseeInfo.Id))
                .Returns(Task.FromResult(FakeHelper.Create<string>()));

            _extraInformationService.Setup(x => x.GetOffereOddGroups(authInfo.SiteName))
                .Returns(FakeHelper.Create<string>());

            _extraInformationService.Setup(x => x.IsFakeLicensee(authInfo.SiteName))
                .Returns(FakeHelper.Create<bool>());

            _extraInformationService.Setup(x => x.GetAuditHardCodeCurrencies(licenseeInfo.Name))
                .Returns(FakeHelper.CreateMany<AuditCurrency>());

            _extraInformationService.Setup(x => x.GetSpecialPlatformByLicenseeName(authInfo.SiteName))
                .Returns(Task.FromResult(FakeHelper.Create<LrfSpecialPlatform>()));
        }

        #endregion
    }
}