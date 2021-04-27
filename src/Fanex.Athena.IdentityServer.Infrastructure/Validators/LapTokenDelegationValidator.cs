using Fanex.Athena.IdentityServer.Infrastructure.Constants;
using Fanex.Athena.IdentityServer.Infrastructure.Extensions;
using Fanex.Athena.IdentityServer.Infrastructure.Models;
using Fanex.Athena.IdentityServer.Infrastructure.Services;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using LAP.ONEBook.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Infrastructure.Validators
{
    public class LapTokenDelegationValidator : IExtensionGrantValidator
    {
        private const string TokenKey = "token";

        public string GrantType => "lap_token";

        private readonly IAuth<DefaultAuthInfo<LapPermission, LapCurrencyAgent>> _authService;
        private readonly IExtraInformationService _extraInformationService;
        private readonly IPersistedGrantService _persistedGrantService;

        public LapTokenDelegationValidator(
            IAuth<DefaultAuthInfo<LapPermission, LapCurrencyAgent>> authService,
            IExtraInformationService extraInformationService,
            IPersistedGrantService persistedGrantService)
        {
            _authService = authService;
            _extraInformationService = extraInformationService;
            _persistedGrantService = persistedGrantService;
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
                await _persistedGrantService.RemoveAllGrantsAsync($"{authInfo.SiteName}_{authInfo.UserSEQ}_{authInfo.UserID}_{authInfo.UserName}", context.Request.ClientId);

                var (licenseeInfo, userLevel) = await TaskExtension.WhenAll(
                    _extraInformationService.GetLicensee(authInfo.SiteName),
                    _extraInformationService.GetUserLevel(authInfo.UserSEQ, authInfo.VendorID));

                var claims = new List<Claim>(
                    new[] {
                        new Claim(JwtClaimTypes.Name, authInfo.UserID.ToString()),
                        new Claim(CustomClaimTypes.VendorID, authInfo.VendorID),
                        new Claim(CustomClaimTypes.SiteName, authInfo.SiteName),
                        new Claim(CustomClaimTypes.UserSEQ, authInfo.UserSEQ.ToString(), ClaimValueTypes.Integer32),
                        new Claim(CustomClaimTypes.UserID, authInfo.UserID),
                        new Claim(CustomClaimTypes.Username, authInfo.UserName),
                        new Claim(CustomClaimTypes.Language, authInfo.Language),
                        new Claim(CustomClaimTypes.ActionLink, authInfo.ActionLink),
                        new Claim(CustomClaimTypes.Administrator, authInfo.IsAdminstrator.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimTypes.CurrencyAgent, JsonConvert.SerializeObject(authInfo.Currency), IdentityServerConstants.ClaimValueTypes.Json),
                        new Claim(CustomClaimTypes.WhiteLabelID, authInfo.WhiteLabelID?.ToString() ?? "-1", ClaimValueTypes.Integer32),
                        new Claim(CustomClaimTypes.IsMasterLicensee, authInfo.IsMasterLicensee.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimTypes.LicenseeId, licenseeInfo.Id.ToString(), ClaimValueTypes.Integer32),
                        new Claim(CustomClaimTypes.IsAllowedCustomerComm, licenseeInfo.IsAllowedCustomerComm.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimTypes.IsEnableBA, licenseeInfo.IsEnableBA.ToString(), ClaimValueTypes.Boolean),
                        new Claim(CustomClaimTypes.UserLevel, userLevel.ToString(), ClaimValueTypes.Integer32)
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
                        "lap_token", DateTime.UtcNow, claims, ClaimsIdentity.DefaultIssuer);
            }
        }

        private static bool IsInvalidToken(DefaultAuthInfo<LapPermission, LapCurrencyAgent> authInfo)
        {
            return string.IsNullOrEmpty(authInfo?.SiteName) || string.IsNullOrEmpty(authInfo?.UserName) || authInfo?.UserSEQ <= 0;
        }
    }
}