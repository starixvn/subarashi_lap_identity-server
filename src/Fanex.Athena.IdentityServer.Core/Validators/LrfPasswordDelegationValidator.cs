using Fanex.Athena.IdentityServer.Core.Constants;
using Fanex.Athena.IdentityServer.Core.Extensions;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Services;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Validators
{
    public class LrfPasswordDelegationValidator : IExtensionGrantValidator
    {
        public string GrantType => CustomGrantType.LrfPassword;

        private readonly IReferenceTokenStore _referenceTokenStore;
        private readonly ILrfAuthService _lrfAuthService;
        private readonly ILrfFunctionService _lrfFunctionService;
        private readonly IExtraInformationService _extraInformationService;
        private readonly ILogger _logger;

        public LrfPasswordDelegationValidator(
            IReferenceTokenStore referenceTokenStore,
            ILrfAuthService lrfAuthService,
            ILrfFunctionService lrfFunctionService,
            IExtraInformationService extraInformationService,
            ILogger<LrfPasswordDelegationValidator> logger
            )
        {
            _referenceTokenStore = referenceTokenStore;
            _lrfAuthService = lrfAuthService;
            _lrfFunctionService = lrfFunctionService;
            _extraInformationService = extraInformationService;
            _logger = logger;
        }

        public Task ValidateAsync(ExtensionGrantValidationContext context)
            => string.Equals(context.Request.Raw["is_renew_token"], "1") ? ValidateLicenseeId(context) : ValidatePassword(context);

        private async Task ValidateLicenseeId(ExtensionGrantValidationContext context)
        {
            if (!int.TryParse(context.Request.Raw["licensee_id"], out int licenseeId) && licenseeId <= 0)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "licenseeId is invalid");
            }

            var accessToken = context.Request.Raw["access_token"];
            var langCode = context.Request.Raw["lang_code"];

            var accessTokenInfo = await _referenceTokenStore.GetReferenceTokenAsync(accessToken);
            if (accessTokenInfo is not null && accessTokenInfo.ClientId == context.Request.ClientId)
            {
                await _referenceTokenStore.RemoveReferenceTokenAsync(accessToken);

                var originalClaims = accessTokenInfo.Claims;
                var userID = GetClaimValue<int>(originalClaims, CustomClaimType.UserSEQ);
                var licenseeInfo = await _extraInformationService.GetLicenseeBelongingLrfUser(licenseeId, userID);
                var identityName = $"lrf_{licenseeInfo.Name}_{GetClaimValue<string>(originalClaims, CustomClaimType.UserID)}_{userID}";

                var claims = new List<Claim>();
                claims.Add(new Claim(JwtClaimTypes.Name, identityName, ClaimValueType.String));
                GenerateUserClaimsFromAccessToken(claims, originalClaims, langCode);
                await GenerateLicenseeClaims(claims, userID, licenseeInfo);
                context.Result = CreateSuccessfulResult(identityName, claims);
            }
            else
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "access token is invalid");
            }
        }

        private async Task ValidatePassword(ExtensionGrantValidationContext context)
        {
            var username = context.Request.Raw["username"];
            var password = context.Request.Raw["password"];
            var loginToken = context.Request.Raw["login_token"];
            var sessionId = context.Request.Raw["session_id"];
            var clientIP = context.Request.Raw["client_ip"];

            var (errorCode, errorMsg, userInfo) = await _lrfAuthService.ValidateUser(username, password, loginToken, clientIP);
            if (errorCode > 0)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, errorMsg);
                _logger.LogInformation($"Failed to validate user {username} - error code: {errorCode}");
            }
            else
            {
                var licenseeInfo = await (userInfo.LicenseeId > 0 ?
                    _extraInformationService.GetLicenseeBelongingLrfUser(userInfo.LicenseeId, userInfo.UserID) : Task.FromResult<LicenseeInformation>(null));

                var identityName = $"lrf_{licenseeInfo?.Name}_{userInfo.Username}_{userInfo.UserID}";
                var claims = new List<Claim>
                {
                    new Claim(JwtClaimTypes.Name, identityName, ClaimValueType.String),
                    new Claim(CustomClaimType.OnUserId, (await _lrfAuthService.LoginOnUser(userInfo, sessionId, clientIP)).ToString(), ClaimValueType.Integer)
                };
                GenerateUserClaims(claims, userInfo, clientIP);
                if (licenseeInfo is not null)
                {
                    await GenerateLicenseeClaims(claims, userInfo.UserID, licenseeInfo);
                }
                context.Result = CreateSuccessfulResult(identityName, claims);
            }
        }

        private static void GenerateUserClaims(List<Claim> claims, LrfUserInfo userInfo, string clientIP)
        {
            const string DefaultLang = "en";
            const string DefaultBaseCurrency = "EUR";

            claims.Add(new Claim(CustomClaimType.UserSEQ, userInfo.UserID.ToString(), ClaimValueType.Integer));
            claims.Add(new Claim(CustomClaimType.UserID, userInfo.Username, ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.Username, userInfo.FirstName, ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.Language, DefaultLang, ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.BaseCurrency, DefaultBaseCurrency, ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.IsInternal, userInfo.IsInternal.ToString(), ClaimValueType.Boolean));
            claims.Add(new Claim(CustomClaimType.UserLevel, "1", ClaimValueType.Integer));
            claims.Add(new Claim(CustomClaimType.ClientIP, clientIP, ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.Administrator, userInfo.Role.Equals("admin", StringComparison.OrdinalIgnoreCase).ToString(), ClaimValueType.Boolean));
        }

        private async Task GenerateLicenseeClaims(List<Claim> claims, int userID, LicenseeInformation licenseeInfo)
        {
            var (offeredSubProducts, isEnableSingleWallet, currencyAgentList, functionList, lrfSpecialPlatform) = await TaskExtension.WhenAll(
                _extraInformationService.GetOfferedSubProducts(licenseeInfo.Id),
                _extraInformationService.IsEnableSingleWallet(licenseeInfo.Name),
                _extraInformationService.GetCurrencyAgentListByLrfUser(licenseeInfo.Id, userID),
                _lrfFunctionService.GetUserFunctions(licenseeInfo.Id, userID),
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
            claims.Add(new Claim(CustomClaimType.DefaultReportUrl, GetDefaultReportUrl(functionList), ClaimValueType.String));
            claims.Add(new Claim(CustomClaimType.CurrencyAgent, JsonConvert.SerializeObject(currencyAgentList), ClaimValueType.Json));
            claims.AddRange(functionList.Select(_ => new Claim(JwtClaimTypes.Role, _.FunctionKey, ClaimValueType.String)));
        }

        private static void GenerateUserClaimsFromAccessToken(ICollection<Claim> claims, ICollection<Claim> originalClaims, string langCode)
        {
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.UserSEQ)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.UserID)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.Username)));
            claims.Add(string.IsNullOrEmpty(langCode) ?
                originalClaims.First(_ => _.Type.Equals(CustomClaimType.Language)) : new Claim(CustomClaimType.Language, langCode));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.BaseCurrency)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.OnUserId)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.IsInternal)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.UserLevel)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.ClientIP)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.Administrator)));
        }

        private static GrantValidationResult CreateSuccessfulResult(string identityName, IEnumerable<Claim> claims)
            => new GrantValidationResult(
                       identityName,
                       CustomGrantType.LrfPassword,
                       DateTimeOffset.UtcNow.DateTime, claims,
                       ClaimsIdentity.DefaultIssuer);

        private static T GetClaimValue<T>(IEnumerable<Claim> claims, string claimType)
            => (T)Convert.ChangeType(GetClaimValue(claims, claimType), typeof(T));

        private static string GetClaimValue(IEnumerable<Claim> claims, string claimType)
            => claims.FirstOrDefault(_ => _.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase))?.Value
            ?? throw new InvalidOperationException($"The {claimType} claim was not found");

        private static string GetDefaultReportUrl(IEnumerable<LrfFunction> functionList)
        {
            const string DefaultReportFunction = "WinlossAnalysis";
            const string DefaultReportUrl = "WinlossAnalysisReport";
            if (!functionList.Any(x => x.FunctionKey.Equals(DefaultReportFunction)))
            {
                var firstFunction = functionList.FirstOrDefault(x => x.ParentId > 0);
                if (firstFunction != null)
                {
                    return firstFunction.Url;
                }
            }

            return DefaultReportUrl;
        }
    }
}