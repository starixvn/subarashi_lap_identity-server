using Fanex.Athena.IdentityServer.Core.Constants;
using Fanex.Athena.IdentityServer.Core.Extensions;
using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Services;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
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
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ILrfAuthService _lrfAuthService;
        private readonly ILrfFunctionService _lrfFunctionService;
        private readonly IExtraInformationService _extraInformationService;

        public LrfPasswordDelegationValidator(
            IReferenceTokenStore referenceTokenStore,
            IRefreshTokenStore refreshTokenStore,
            ILrfAuthService lrfAuthService,
            ILrfFunctionService lrfFunctionService,
            IExtraInformationService extraInformationService
            )
        {
            _referenceTokenStore = referenceTokenStore;
            _refreshTokenStore = refreshTokenStore;
            _lrfAuthService = lrfAuthService;
            _lrfFunctionService = lrfFunctionService;
            _extraInformationService = extraInformationService;
        }

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            if (string.Equals(context.Request.Raw["is_renew_token"], "1"))
            {
                await ValidateLicenseeId(context);
            }
            else
            {
                await ValidatePassword(context);
            }
        }

        private async Task ValidateLicenseeId(ExtensionGrantValidationContext context)
        {
            if (!int.TryParse(context.Request.Raw["licensee_id"], out int licenseeId) && licenseeId <= 0)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "licenseeId is invalid");
            }

            var accessToken = context.Request.Raw["access_token"];
            var refreshToken = context.Request.Raw["refresh_token"];

            var fullAccessToken = await _referenceTokenStore.GetReferenceTokenAsync(accessToken);
            if (fullAccessToken != null && fullAccessToken.ClientId == context.Request.ClientId)
            {
                await _referenceTokenStore.RemoveReferenceTokenAsync(accessToken);
                await _refreshTokenStore.RemoveRefreshTokenAsync(refreshToken);

                var originalClaims = fullAccessToken.Claims;
                var userID = GetClaimValue<int>(originalClaims, CustomClaimType.UserSEQ);
                var licenseeInfo = await _extraInformationService.GetLicenseeBelongingLrfUser(licenseeId, userID);
                var (isEnableSingleWallet, currencyAgentList, functionList) = await GetIdentityInfo(licenseeInfo, userID);

                var identityName = $"lrf_{licenseeInfo?.Name}_{GetClaimValue<string>(originalClaims, CustomClaimType.Username)}_{userID}";

                var claims = new List<Claim>();
                claims.Add(new Claim(JwtClaimTypes.Name, identityName));
                claims.Add(new Claim(CustomClaimType.SiteName, licenseeInfo?.Name ?? string.Empty));
                AddReusedClaims(claims, originalClaims);
                AddNewClaims(claims, licenseeInfo, functionList, isEnableSingleWallet, currencyAgentList);

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
            }
            else
            {
                var licenseeInfo = await (userInfo.LicenseeId > 0 ?
                    _extraInformationService.GetLicenseeBelongingLrfUser(userInfo.LicenseeId, userInfo.UserID) : Task.FromResult<LicenseeInformation>(null));

                var (onUserId, isEnableSingleWallet, currencyAgentList, functionList) = await TaskExtension.WhenAll(
                    _lrfAuthService.LoginOnUser(userInfo, sessionId, clientIP),
                    licenseeInfo == null ? Task.FromResult(false) : _extraInformationService.IsEnableSingleWallet(licenseeInfo.Name),
                    licenseeInfo == null ? Task.FromResult(Enumerable.Empty<CurrencyAgent>()) : _extraInformationService.GetCurrencyAgentListByLrfUser(licenseeInfo.Id, userInfo.UserID),
                    licenseeInfo == null ? Task.FromResult(Enumerable.Empty<LrfFunction>()) : _lrfFunctionService.GetUserFunctions(licenseeInfo.Id, userInfo.UserID));

                var identityName = $"lrf_{licenseeInfo?.Name}_{userInfo.Username}_{userInfo.UserID}";
                var claims = new List<Claim>();
                claims.Add(new Claim(JwtClaimTypes.Name, identityName));
                AddNewClaims(claims, licenseeInfo, userInfo, onUserId, functionList, isEnableSingleWallet);
                claims.Add(new Claim(CustomClaimType.CurrencyAgent, JsonConvert.SerializeObject(
                      CommonHelper.ModifyCurrenciesForKeenOcean(currencyAgentList, licenseeInfo?.Id ?? 0)),
                      IdentityServerConstants.ClaimValueTypes.Json));
                claims.AddRange(functionList.Select(_ => new Claim(JwtClaimTypes.Role, _.FunctionKey)));

                context.Result = CreateSuccessfulResult(identityName, claims);
            }
        }

        private Task<(bool isEnableSingleWallet, IEnumerable<CurrencyAgent> currencyAgentList, IEnumerable<LrfFunction> functionList)> GetIdentityInfo(LicenseeInformation licenseeInfo, int userID)
             => TaskExtension.WhenAll(
             licenseeInfo == null ? Task.FromResult(false) : _extraInformationService.IsEnableSingleWallet(licenseeInfo.Name),
             licenseeInfo == null ? Task.FromResult(Enumerable.Empty<CurrencyAgent>()) : _extraInformationService.GetCurrencyAgentListByLrfUser(licenseeInfo.Id, userID),
             licenseeInfo == null ? Task.FromResult(Enumerable.Empty<LrfFunction>()) : _lrfFunctionService.GetUserFunctions(licenseeInfo.Id, userID));

        private void AddNewClaims(List<Claim> claims,
            LicenseeInformation licenseeInfo,
            IEnumerable<LrfFunction> functionList,
            bool isEnableSingleWallet,
            IEnumerable<CurrencyAgent> currencyAgentList)
        {
            claims.Add(new Claim(CustomClaimType.DefaultReportUrl, GetDefaultReportUrl(functionList)));
            claims.Add(new Claim(CustomClaimType.LicenseeId, licenseeInfo?.Id.ToString() ?? "-1", ClaimValueTypes.Integer32));
            claims.Add(new Claim(CustomClaimType.IsAllowedCustomerComm, (licenseeInfo?.IsAllowedCustomerComm ?? false).ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.IsEnableBA, (licenseeInfo?.IsEnableBA ?? false).ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.UserLevel, "1", ClaimValueTypes.Integer32));
            claims.Add(new Claim(CustomClaimType.IsEnableSingleWallet, isEnableSingleWallet.ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.IsShowIP, _extraInformationService.IsOfferShowIP(licenseeInfo?.Name).ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.CurrencyAgent, JsonConvert.SerializeObject(
                  CommonHelper.ModifyCurrenciesForKeenOcean(currencyAgentList, licenseeInfo?.Id ?? 0)), IdentityServerConstants.ClaimValueTypes.Json));

            claims.AddRange(functionList.Select(_ => new Claim(JwtClaimTypes.Role, _.FunctionKey)));
        }

        private void AddNewClaims(List<Claim> claims,
            LicenseeInformation licenseeInfo,
            LrfUserInfo userInfo,
            int onUserId,
            IEnumerable<LrfFunction> functionList,
            bool isEnableSingleWallet)
        {
            claims.Add(new Claim(CustomClaimType.SiteName, licenseeInfo?.Name ?? string.Empty));
            claims.Add(new Claim(CustomClaimType.UserSEQ, userInfo.UserID.ToString()));
            claims.Add(new Claim(CustomClaimType.UserID, userInfo.Username));
            claims.Add(new Claim(CustomClaimType.Username, userInfo.FirstName));
            claims.Add(new Claim(CustomClaimType.Language, "en-US"));
            claims.Add(new Claim(CustomClaimType.BaseCurrency, "EUR"));
            claims.Add(new Claim(CustomClaimType.OnUserId, onUserId.ToString()));
            claims.Add(new Claim(CustomClaimType.IsInternal, userInfo.IsInternal.ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.UserLevel, "1", ClaimValueTypes.Integer32));
            claims.Add(new Claim(CustomClaimType.Administrator, userInfo.Role.Equals("admin", StringComparison.OrdinalIgnoreCase).ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.DefaultReportUrl, GetDefaultReportUrl(functionList)));
            claims.Add(new Claim(CustomClaimType.LicenseeId, licenseeInfo?.Id.ToString() ?? "-1", ClaimValueTypes.Integer32));
            claims.Add(new Claim(CustomClaimType.IsAllowedCustomerComm, (licenseeInfo?.IsAllowedCustomerComm ?? false).ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.IsEnableBA, (licenseeInfo?.IsEnableBA ?? false).ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.IsEnableSingleWallet, isEnableSingleWallet.ToString(), ClaimValueTypes.Boolean));
            claims.Add(new Claim(CustomClaimType.IsShowIP, _extraInformationService.IsOfferShowIP(licenseeInfo?.Name).ToString(), ClaimValueTypes.Boolean));
        }

        private static void AddReusedClaims(ICollection<Claim> claims, ICollection<Claim> originalClaims)
        {
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.UserSEQ)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.UserID)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.Username)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.Language)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.BaseCurrency)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.OnUserId)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.IsInternal)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.UserLevel)));
            claims.Add(originalClaims.First(_ => _.Type.Equals(CustomClaimType.Administrator)));
        }

        private static GrantValidationResult CreateSuccessfulResult(string identityName, IEnumerable<Claim> claims)
            => new GrantValidationResult(
                       identityName,
                       CustomGrantType.LrfPassword,
                       DateTime.UtcNow, claims,
                       ClaimsIdentity.DefaultIssuer);

        private static T GetClaimValue<T>(IEnumerable<Claim> claims, string claimType)
            => (T)Convert.ChangeType(GetClaimValue(claims, claimType), typeof(T));

        private static string GetClaimValue(IEnumerable<Claim> claims, string claimType)
            => claims.FirstOrDefault(_ => _.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase))?.Value
            ?? throw new InvalidOperationException($"The {claimType} claim was not found");

        private static string GetDefaultReportUrl(IEnumerable<LrfFunction> functionList)
        {
            const string DefaultReportFunction = "LicenseeWinLoss27";
            const string DefaultReportUrl = "LicenseeWinLossReport";
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