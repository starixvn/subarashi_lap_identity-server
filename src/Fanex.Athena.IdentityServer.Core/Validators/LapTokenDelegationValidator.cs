using Fanex.Athena.IdentityServer.Core.Constants;
using Fanex.Athena.IdentityServer.Core.Extensions;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Services;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using LAP.ONEBook.Security;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Validators
{
    public class LapTokenDelegationValidator : IExtensionGrantValidator
    {
        private const string LicenseeNotExistedErrorCode = "200";

        public string GrantType => CustomGrantType.LapToken;

        private readonly ILogger<LapTokenDelegationValidator> _logger;
        private readonly IAuth<DefaultAuthInfo<LapFunction, CurrencyAgent>> _authService;
        private readonly IExtraInformationService _extraInformationService;
        private readonly IReferenceTokenStore _referenceTokenStore;

        public LapTokenDelegationValidator(
            ILogger<LapTokenDelegationValidator> logger,
            IAuth<DefaultAuthInfo<LapFunction, CurrencyAgent>> authService,
            IExtraInformationService extraInformationService,
            IReferenceTokenStore referenceTokenStore
            )
        {
            _logger = logger;
            _authService = authService;
            _extraInformationService = extraInformationService;
            _referenceTokenStore = referenceTokenStore;
        }

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var token = context.Request.Raw["token"];
            var authInfo = await _authService.AuthenticateAsync(token);
            if (IsInvalidToken(authInfo))
            {
                var errorMsg = $"Token {token} is invalid ";
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, errorMsg);
                _logger.LogError(errorMsg);
                return;
            }

            var licenseeInfo = await _extraInformationService.GetLicensee(authInfo.SiteName);
            if (licenseeInfo is null)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, LicenseeNotExistedErrorCode);
                _logger.LogError($"Licensee {authInfo.SiteName} is not existed");
                return;
            }

            await TryRevokeOldToken(context);

            var (userInfo, isEnableSingleWallet, offeredSubProducts) = await TaskExtension.WhenAll(
                _extraInformationService.GetLapUserInfo(authInfo.UserSEQ, authInfo.VendorID),
                _extraInformationService.IsEnableSingleWallet(authInfo.SiteName),
                _extraInformationService.GetOfferedSubProducts(licenseeInfo.Id));

            var identityName = $"portal_{authInfo.VendorID}_{authInfo.SiteName}_{authInfo.UserID}_{authInfo.UserSEQ}";

            var claims = new List<Claim>(
                new[] {
                        new Claim(JwtClaimTypes.Name, identityName),
                        new Claim(CustomClaimType.VendorID, authInfo.VendorID),
                        new Claim(CustomClaimType.SiteName, authInfo.SiteName),
                        new Claim(CustomClaimType.UserSEQ, authInfo.UserSEQ.ToString()),
                        new Claim(CustomClaimType.UserID, authInfo.UserID),
                        new Claim(CustomClaimType.Username, authInfo.UserName),
                        new Claim(CustomClaimType.Language, authInfo.Language),
                        new Claim(CustomClaimType.ActionLink, authInfo.ActionLink),
                        new Claim(CustomClaimType.Administrator, authInfo.IsAdminstrator.ToString()),
                        new Claim(CustomClaimType.WhiteLabelID, authInfo.WhiteLabelID?.ToString() ?? "-1"),
                        new Claim(CustomClaimType.IsMasterLicensee, authInfo.IsMasterLicensee.ToString()),
                        new Claim(CustomClaimType.LicenseeId, licenseeInfo.Id.ToString()),
                        new Claim(CustomClaimType.IsAllowedCustomerComm, licenseeInfo.IsAllowedCustomerComm.ToString()),
                        new Claim(CustomClaimType.IsEnableBA, licenseeInfo.IsEnableBA.ToString()),
                        new Claim(CustomClaimType.UserLevel, userInfo.UserLevel.ToString()),
                        new Claim(CustomClaimType.IsEnabledSubsidiary, userInfo.IsEnabledSubsidiary.ToString()),
                        new Claim(CustomClaimType.OperatorID, userInfo.OperatorID),
                        new Claim(CustomClaimType.BaseCurrency, userInfo.BaseCurrency),
                        new Claim(CustomClaimType.IsEnableSingleWallet, isEnableSingleWallet.ToString()),
                        new Claim(CustomClaimType.IsShowIP, _extraInformationService.IsOfferShowIP(authInfo.SiteName).ToString()),
                        new Claim(CustomClaimType.OfferedSubProducts, offeredSubProducts),
                        new Claim(CustomClaimType.OfferedOddsGroups, _extraInformationService.GetOffereOddGroups(authInfo.SiteName)),
                        new Claim(CustomClaimType.IsFakeLicensee, _extraInformationService.IsFakeLicensee(licenseeInfo.Name).ToString()),
                        new Claim(CustomClaimType.CurrencyAgent, JsonConvert.SerializeObject(authInfo.Currency), IdentityServerConstants.ClaimValueTypes.Json)
                        });

            AddExternalClaims(claims, context.Request);

            if (authInfo.IsAdminstrator)
            {
                claims.Add(new Claim(JwtClaimTypes.Role, "super_admin"));
            }
            else
            {
                var roles = authInfo.Permissions.SelectMany(_ => new List<string>(_.SubFunctionKeys ?? Enumerable.Empty<string>())
                                                                                            .Append(_.FunctionKey));
                claims.AddRange(roles.Select(_ => new Claim(JwtClaimTypes.Role, _)));
            }

            context.Result = new GrantValidationResult(
                    identityName,
                    CustomGrantType.LapToken,
                    DateTime.UtcNow, claims,
                    ClaimsIdentity.DefaultIssuer);
        }

        private void AddExternalClaims(ICollection<Claim> claims, ValidatedTokenRequest request)
        {
            var clientIp = request.Raw["client_ip"];
            if (!string.IsNullOrEmpty(clientIp))
            {
                claims.Add(new Claim(CustomClaimType.ClientIP, clientIp));
            }        
        }

        private async Task TryRevokeOldToken(ExtensionGrantValidationContext context)
        {
            var accessToken = context.Request.Raw["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                await _referenceTokenStore.RemoveReferenceTokenAsync(accessToken);
            }
        }

        private static bool IsInvalidToken(DefaultAuthInfo<LapFunction, CurrencyAgent> authInfo)
            => string.IsNullOrEmpty(authInfo?.SiteName) || string.IsNullOrEmpty(authInfo.UserName) || authInfo.UserSEQ <= 0;
    }
}