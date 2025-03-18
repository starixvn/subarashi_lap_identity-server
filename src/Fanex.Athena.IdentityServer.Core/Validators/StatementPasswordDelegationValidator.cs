using Fanex.Athena.IdentityServer.Core.Constants;
using Fanex.Athena.IdentityServer.Core.Extensions;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Services;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Validators
{
    public class StatementPasswordDelegationValidator : IExtensionGrantValidator
    {
        private readonly IExtraInformationService _extraInformationService;

        public StatementPasswordDelegationValidator(IExtraInformationService extraInformationService)
        {
            _extraInformationService = extraInformationService;
        }

        public string GrantType => CustomGrantType.StatementPassword;

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var siteName = context.Request.Raw["sitename"];
            var userName = context.Request.Raw["username"];
            var isShowIPVN = context.Request.Raw["isshowipvn"];

            var licenseeInfo = await _extraInformationService.GetLicensee(siteName);
            if (licenseeInfo is null)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, $"LicenseeNotExistedErrorCode - LicenseeName: {siteName}");
                return;
            }

            var identityName = $"ext_{siteName}_{userName}";
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Name, identityName, ClaimValueType.String),
            };
            if (!string.IsNullOrEmpty(isShowIPVN))
            {
                claims.Add(new Claim(CustomClaimType.IsShowIPVN, isShowIPVN, ClaimValueType.Boolean));
            }
            else
            {
                claims.Add(new Claim(CustomClaimType.IsShowIPVN, true.ToString(), ClaimValueType.Boolean));
            }
            await GenerateLicenseeClaimAsync(licenseeInfo, claims);
            claims.Add(new Claim(JwtClaimTypes.Role, Roles.StatementServiceRoleUser, ClaimValueType.String));

            context.Result = CreateSuccessfulResult(identityName, claims);
        }

        private async Task GenerateLicenseeClaimAsync(LicenseeInformation licenseeInfo, List<Claim> claims)
        {
            var (offeredSubProducts, isEnableSingleWallet, currencyAgentList, lrfSpecialPlatform) = await TaskExtension.WhenAll(
                _extraInformationService.GetOfferedSubProducts(licenseeInfo.Id),
                _extraInformationService.IsEnableSingleWallet(licenseeInfo.Name),
                _extraInformationService.GetCurrencyAgentForExtService(licenseeInfo.Name),
                _extraInformationService.GetSpecialPlatformByLicenseeName(licenseeInfo.Name));

            claims.Add(new Claim(CustomClaimType.LicenseeId, licenseeInfo.Id.ToString(), ClaimValueType.Integer));
            claims.Add(new Claim(CustomClaimType.SiteName, licenseeInfo.Name, ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.SpecialPlatform, lrfSpecialPlatform.SpecialPlatform.ToString(), ClaimValueType.Integer));
            claims.Add(new Claim(CustomClaimType.SpecialPlatformType, lrfSpecialPlatform.SpecialPlatformType.ToString(), ClaimValueType.Integer));
            claims.Add(new Claim(CustomClaimType.SiteDBServer, licenseeInfo.SiteDBServer, ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.OfferedSubProducts, offeredSubProducts, ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.OfferedOddsGroups, _extraInformationService.GetOffereOddGroups(licenseeInfo.Name), ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.IsAllowedCustomerComm, licenseeInfo.IsAllowedCustomerComm.ToString(), ClaimValueType.Boolean));
            claims.Add(new Claim(CustomClaimType.IsEnableSingleWallet, isEnableSingleWallet.ToString(), ClaimValueType.Boolean));
            claims.Add(new Claim(CustomClaimType.IsShowIP, _extraInformationService.IsOfferShowIP(licenseeInfo.Name).ToString(), ClaimValueType.Boolean));
            claims.Add(new Claim(CustomClaimType.IsFakeLicensee, _extraInformationService.IsFakeLicensee(licenseeInfo.Name).ToString(), ClaimValueType.Boolean));
            claims.Add(new Claim(CustomClaimType.AuditCurrencies, JsonConvert.SerializeObject(_extraInformationService.GetAuditHardCodeCurrencies(licenseeInfo.Name)), ClaimValueType.Json));
            claims.Add(new Claim(CustomClaimType.CurrencyAgent, JsonConvert.SerializeObject(currencyAgentList), ClaimValueType.Json));
        }

        private static GrantValidationResult CreateSuccessfulResult(string identityName, IEnumerable<Claim> claims)
            => new GrantValidationResult(
                       identityName,
                       CustomGrantType.StatementPassword,
                       DateTimeOffset.UtcNow.DateTime, claims,
                       ClaimsIdentity.DefaultIssuer);
    }
}