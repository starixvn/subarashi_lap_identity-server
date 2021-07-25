using Fanex.Athena.IdentityServer.Core.Constants;
using Fanex.Athena.IdentityServer.Core.Extensions;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Services;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using LAP.ONEBook.Security;
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
        private const string TokenKey = "token";

        public string GrantType => CustomGrantType.LapToken;

        private readonly IAuth<DefaultAuthInfo<LapPermission, LapCurrencyAgent>> _authService;
        private readonly IExtraInformationService _extraInformationService;

        public LapTokenDelegationValidator(
            IAuth<DefaultAuthInfo<LapPermission, LapCurrencyAgent>> authService,
            IExtraInformationService extraInformationService)
        {
            _authService = authService;
            _extraInformationService = extraInformationService;
        }

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var token = context.Request.Raw[TokenKey];
            var authInfo = await _authService.AuthenticateAsync(token);
            if (IsInvalidToken(authInfo))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, $"token {token} is invalid");
            }
            else
            {
                var (licenseeInfo, userLevel, isEnableSingleWallet) = await TaskExtension.WhenAll(
                    _extraInformationService.GetLicensee(authInfo.SiteName),
                    _extraInformationService.GetUserLevel(authInfo.UserSEQ, authInfo.VendorID),
                    _extraInformationService.IsEnableSingleWallet(authInfo.SiteName));

                var claims = new List<Claim>(
                    new[] {
                        new Claim(JwtClaimTypes.Name, authInfo.UserID.ToString()),
                        new Claim(CustomClaimType.VendorID, authInfo.VendorID),
                        new Claim(CustomClaimType.SiteName, authInfo.SiteName),
                        new Claim(CustomClaimType.UserSEQ, authInfo.UserSEQ.ToString(), ClaimValueTypes.Integer32),
                        new Claim(CustomClaimType.UserID, authInfo.UserID),
                        new Claim(CustomClaimType.Username, authInfo.UserName),
                        new Claim(CustomClaimType.Language, authInfo.Language),
                        new Claim(CustomClaimType.ActionLink, authInfo.ActionLink),
                        new Claim(CustomClaimType.Administrator, authInfo.IsAdminstrator.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimType.CurrencyAgent, JsonConvert.SerializeObject(authInfo.Currency), IdentityServerConstants.ClaimValueTypes.Json),
                        new Claim(CustomClaimType.WhiteLabelID, authInfo.WhiteLabelID?.ToString() ?? "-1", ClaimValueTypes.Integer32),
                        new Claim(CustomClaimType.IsMasterLicensee, authInfo.IsMasterLicensee.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimType.LicenseeId, licenseeInfo.Id.ToString(), ClaimValueTypes.Integer32),
                        new Claim(CustomClaimType.IsAllowedCustomerComm, licenseeInfo.IsAllowedCustomerComm.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimType.IsEnableBA, licenseeInfo.IsEnableBA.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimType.UserLevel, userLevel.ToString(), ClaimValueTypes.Integer32),
                        new Claim(CustomClaimType.IsEnableSingleWallet, isEnableSingleWallet.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimType.IsShowIP, _extraInformationService.IsOfferShowIP(authInfo.SiteName).ToString(), ClaimValueTypes.Boolean)
                    });

                if (authInfo.IsAdminstrator)
                {
                    claims.Add(new Claim(JwtClaimTypes.Role, "super_admin"));
                }
                else
                {
                    var neededPermissions = authInfo.Permissions.Where(_ => _.FunctionKey.StartsWith("pa_", StringComparison.OrdinalIgnoreCase) || _.FunctionKey.StartsWith("PlayerAdmin", StringComparison.OrdinalIgnoreCase));
                    var roles = neededPermissions.SelectMany(_ => new List<string>(_.SubFunctionKeys ?? Enumerable.Empty<string>())
                                                                                                .Append(_.FunctionKey));
                    claims.AddRange(roles.Select(_ => new Claim(JwtClaimTypes.Role, _)));
                }

                context.Result = new GrantValidationResult(
                        $"{authInfo.SiteName}_{authInfo.UserSEQ}_{authInfo.UserID}_{authInfo.UserName}",
                        CustomGrantType.LapToken,
                        DateTime.UtcNow, claims,
                        ClaimsIdentity.DefaultIssuer);
            }
        }

        private static bool IsInvalidToken(DefaultAuthInfo<LapPermission, LapCurrencyAgent> authInfo)
            => string.IsNullOrEmpty(authInfo?.SiteName) || string.IsNullOrEmpty(authInfo.UserName) || authInfo.UserSEQ <= 0;
    }
}