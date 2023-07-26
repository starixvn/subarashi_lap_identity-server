using Fanex.Athena.IdentityServer.Core.Constants;
using IdentityModel;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Fanex.Athena.IdentityServer
{
    public static class Config
    {
        public class UserInfoResource : IdentityResource
        {
            public const string IdentityName = "userinfo_resource";

            public UserInfoResource()
            {
                base.Name = IdentityName;
                UserClaims = new[]
                {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimType.VendorID,
                        CustomClaimType.SiteName,
                        CustomClaimType.UserSEQ,
                        CustomClaimType.UserID,
                        CustomClaimType.Username,
                        CustomClaimType.Language,
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
                        CustomClaimType.IsEnableBA,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.IsShowIP,
                        CustomClaimType.OnUserId,
                        CustomClaimType.IsInternal,
                        CustomClaimType.DefaultReportUrl,
                        CustomClaimType.ActionLink,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.OfferedOddsGroups,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.ClientIP,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.AuditCurrencies
                };
            }
        }

        public static IEnumerable<IdentityResource> Ids =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new UserInfoResource()
            };

        public static IEnumerable<ApiScope> ScopeApis =>
            new List<ApiScope>
            {
                new ApiScope("playeradmin_api.external")
                {
                    UserClaims =
                    {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimType.SiteName,
                        CustomClaimType.UserSEQ,
                        CustomClaimType.UserID,
                        CustomClaimType.Username,
                        CustomClaimType.Language,
                        CustomClaimType.Administrator,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.WhiteLabelID,
                        CustomClaimType.IsMasterLicensee,
                        CustomClaimType.UserLevel,
                        CustomClaimType.OperatorID,
                        CustomClaimType.IsEnabledSubsidiary,
                        CustomClaimType.LicenseeId,
                        CustomClaimType.IsAllowedCustomerComm,
                        CustomClaimType.IsEnableBA,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.OfferedOddsGroups,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.ClientIP,
                        CustomClaimType.SiteDBServer
                    }
                },
                new ApiScope("playeradmin_api.internal")
                {
                     UserClaims =
                     {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimType.SiteName,
                        CustomClaimType.UserSEQ,
                        CustomClaimType.UserID,
                        CustomClaimType.Username,
                        CustomClaimType.Language,
                        CustomClaimType.Administrator,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.UserLevel,
                        CustomClaimType.IsMasterLicensee,
                        CustomClaimType.LicenseeId,
                        CustomClaimType.WhiteLabelID,
                        CustomClaimType.IsAllowedCustomerComm,
                        CustomClaimType.IsEnableBA,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.IsShowIP,
                        CustomClaimType.ActionLink,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.OfferedOddsGroups,
                        CustomClaimType.IsFakeLicensee  ,
                        CustomClaimType.SiteDBServer
                     }
                },
                new ApiScope("betticket_api")
                {
                    UserClaims =
                    {
                        CustomClaimType.SiteName,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.IsShowIP,
                        CustomClaimType.Language,
                        CustomClaimType.ClientIP
                    }
                },
                new ApiScope("lrf")
                {
                    UserClaims =
                    {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimType.VendorID,
                        CustomClaimType.SiteName,
                        CustomClaimType.UserSEQ,
                        CustomClaimType.UserID,
                        CustomClaimType.Username,
                        CustomClaimType.Language,
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
                        CustomClaimType.IsEnableBA,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.IsShowIP,
                        CustomClaimType.OnUserId,
                        CustomClaimType.IsInternal,
                        CustomClaimType.DefaultReportUrl,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.OfferedOddsGroups,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.AuditCurrencies
                    }
                },
                new ApiScope("statement_api")
                {
                    UserClaims =
                    {
                        CustomClaimType.SiteName,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.IsEnableSingleWallet,
                        CustomClaimType.OfferedSubProducts
                    }
                },
                new ApiScope("analytics_api")
                {
                    UserClaims =
                    {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimType.SiteName,
                        CustomClaimType.LicenseeId,
                        CustomClaimType.UserSEQ,
                        CustomClaimType.UserID,
                        CustomClaimType.Language,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.OfferedSubProducts
                    }
                },
                new ApiScope("report_api")
                {
                    UserClaims =
                    {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimType.SiteName,
                        CustomClaimType.UserSEQ,
                        CustomClaimType.UserID,
                        CustomClaimType.Language,
                        CustomClaimType.CurrencyAgent,
                        CustomClaimType.OfferedSubProducts,
                        CustomClaimType.IsFakeLicensee,
                        CustomClaimType.SiteDBServer,
                        CustomClaimType.AuditCurrencies
                    }
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
    }
}