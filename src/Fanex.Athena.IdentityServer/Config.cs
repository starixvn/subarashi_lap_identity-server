namespace Fanex.Athena.IdentityServer
{
    using System.Collections.Generic;
    using Core.Constants;
    using IdentityModel;
    using IdentityServer4.Models;
    using Microsoft.Extensions.Configuration;

    public static class Config
    {
        private static readonly List<string> BaseUserClaims =
        [
            JwtClaimTypes.Name,
            JwtClaimTypes.Role,
            CustomClaimType.SiteName,
            CustomClaimType.UserSEQ,
            CustomClaimType.UserID,
            CustomClaimType.Language,
            CustomClaimType.Username
        ];

        public static IEnumerable<IdentityResource> Ids =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new UserInfoResource()
            };

        public static IEnumerable<ApiScope> ScopeApis =>
            new List<ApiScope>
            {
                new("playeradmin_api.external")
                {
                    UserClaims =
                    [
                        ..BaseUserClaims,
                        CustomClaimType.Administrator,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.WhiteLabelID,
                        CustomClaimType.IsMasterLicensee,
                        CustomClaimType.UserLevel,
                        CustomClaimType.OperatorID,
                        CustomClaimType.IsEnabledSubsidiary,
                        CustomClaimType.LicenseeId,
                        CustomClaimType.IsAllowedCustomerComm,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.OfferedOddsGroups,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.ClientIP,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.SpecialPlatform,
                        CustomClaimType.IsShowIPVN
                    ]
                },
                new("playeradmin_api.internal")
                {
                    UserClaims =
                    [
                        ..BaseUserClaims,
                        CustomClaimType.Administrator,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.UserLevel,
                        CustomClaimType.IsMasterLicensee,
                        CustomClaimType.LicenseeId,
                        CustomClaimType.WhiteLabelID,
                        CustomClaimType.IsAllowedCustomerComm,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.IsShowIP,
                        CustomClaimType.ActionLink,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.OfferedOddsGroups,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.SpecialPlatform
                    ]
                },
                new("betticket_api")
                {
                    UserClaims =
                    [
                        ..BaseUserClaims,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.IsShowIP,
                        CustomClaimType.ClientIP
                    ]
                },
                new("lrf")
                {
                    UserClaims =
                    [
                        ..BaseUserClaims,
                        CustomClaimType.VendorID,
                        CustomClaimType.ActionLink,
                        CustomClaimType.Administrator,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.WhiteLabelID,
                        CustomClaimType.IsMasterLicensee,
                        CustomClaimType.UserLevel,
                        CustomClaimType.IsEnabledSubsidiary,
                        CustomClaimType.OperatorID,
                        CustomClaimType.BaseCurrency,
                        CustomClaimType.LicenseeId,
                        CustomClaimType.IsAllowedCustomerComm,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.IsShowIP,
                        CustomClaimType.OnUserId,
                        CustomClaimType.IsInternal,
                        CustomClaimType.DefaultReportUrl,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.OfferedOddsGroups,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.AuditCurrencies,
                        CustomClaimType.SpecialPlatform
                    ]
                },
                new("statement_api")
                {
                    UserClaims =
                    [
                        ..BaseUserClaims,
                        CustomClaimType.Administrator,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.UserLevel,
                        CustomClaimType.IsMasterLicensee,
                        CustomClaimType.LicenseeId,
                        CustomClaimType.WhiteLabelID,
                        CustomClaimType.IsAllowedCustomerComm,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.IsShowIP,
                        CustomClaimType.ActionLink,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.OfferedOddsGroups,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.SpecialPlatform,
                        CustomClaimType.IsShowIPVN,
                        CustomClaimType.AuditCurrencies
                    ]
                },
                new("analytics_api")
                {
                    UserClaims =
                    [
                        ..BaseUserClaims,
                        CustomClaimType.LicenseeId,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.SpecialPlatform,
                        CustomClaimType.SpecialPlatformType
                    ]
                },
                new("report_api")
                {
                    UserClaims =
                    [
                        ..BaseUserClaims,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.AuditCurrencies,
                        CustomClaimType.SpecialPlatform,
                        CustomClaimType.SpecialPlatformType
                    ]
                },
                new("betlist_api")
                {
                    UserClaims =
                    [
                        ..BaseUserClaims,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.AuditCurrencies,
                        CustomClaimType.SpecialPlatform,
                        CustomClaimType.SpecialPlatformType,
                        CustomClaimType.IsAllowedCustomerComm,
                        CustomClaimType.IsShowIPVN
                    ]
                }
            };

        public static IEnumerable<ApiResource> GetApis(IConfiguration configuration)
            => configuration
                .GetSection("IdentityServer:ApiResources")
                .Get<IEnumerable<ApiResource>>();

        public static IEnumerable<Client> GetClients(IConfiguration configuration)
            => configuration
                .GetSection("IdentityServer:Clients")
                .Get<IEnumerable<Client>>();

        public class UserInfoResource : IdentityResource
        {
            public const string IdentityName = "userinfo_resource";

            public UserInfoResource()
            {
                Name = IdentityName;
                UserClaims =
                [
                    ..BaseUserClaims,
                    CustomClaimType.VendorID,
                    CustomClaimType.Administrator,
                    CustomClaimType.CurrencyAgent,
                    CustomClaimType.WhiteLabelID,
                    CustomClaimType.IsMasterLicensee,
                    CustomClaimType.UserLevel,
                    CustomClaimType.IsEnabledSubsidiary,
                    CustomClaimType.OperatorID,
                    CustomClaimType.BaseCurrency,
                    CustomClaimType.LicenseeId,
                    CustomClaimType.IsAllowedCustomerComm,
                    CustomClaimType.IsEnableSingleWallet,
                    CustomClaimType.IsShowIP,
                    CustomClaimType.IsShowIPVN,
                    CustomClaimType.OnUserId,
                    CustomClaimType.IsInternal,
                    CustomClaimType.DefaultReportUrl,
                    CustomClaimType.ActionLink,
                    CustomClaimType.OfferedSubProducts,
                    CustomClaimType.OfferedOddsGroups,
                    CustomClaimType.IsFakeLicensee,
                    CustomClaimType.ClientIP,
                    CustomClaimType.SiteDBServer,
                    CustomClaimType.AuditCurrencies,
                    CustomClaimType.SpecialPlatform
                ];
            }
        }
    }
}