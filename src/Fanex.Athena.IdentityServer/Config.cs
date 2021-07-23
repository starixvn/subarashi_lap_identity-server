using Fanex.Athena.IdentityServer.Core.Constants;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace Fanex.Athena.IdentityServerAPI
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
                        CustomClaimTypes.VendorID,
                        CustomClaimTypes.SiteName,
                        CustomClaimTypes.UserSEQ,
                        CustomClaimTypes.UserID,
                        CustomClaimTypes.Username,
                        CustomClaimTypes.Language,
                        CustomClaimTypes.Administrator,
                        CustomClaimTypes.CurrencyAgent,
                        CustomClaimTypes.WhiteLabelID,
                        CustomClaimTypes.IsMasterLicensee,
                        CustomClaimTypes.UserLevel,
                        CustomClaimTypes.LicenseeId,
                        CustomClaimTypes.IsAllowedCustomerComm,
                        CustomClaimTypes.IsEnableBA,
                        CustomClaimTypes.ActionLink
                };
            }
        }

        public static IEnumerable<IdentityResource> Ids =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new UserInfoResource()
            };

        public static IEnumerable<ApiResource> Apis =>
            new List<ApiResource>
            {
                new ApiResource("playeradmin_api", "Player Admin API")
                {
                    ApiSecrets = { new Secret("playeradmin_api.secret".Sha256()) },
                    Scopes = { "playeradmin_api.external" },
                    UserClaims =
                    {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimTypes.SiteName,
                        CustomClaimTypes.UserSEQ,
                        CustomClaimTypes.UserID,
                        CustomClaimTypes.Username,
                        CustomClaimTypes.Language,
                        CustomClaimTypes.Administrator,
                        CustomClaimTypes.CurrencyAgent,
                        CustomClaimTypes.WhiteLabelID,
                        CustomClaimTypes.IsMasterLicensee,
                        CustomClaimTypes.UserLevel,
                        CustomClaimTypes.LicenseeId,
                        CustomClaimTypes.IsAllowedCustomerComm,
                        CustomClaimTypes.IsEnableBA
                    },
                },
                new ApiResource("betticket_api", "Bet Ticket API")
                {
                    ApiSecrets = { new Secret("betticket_api.secret".Sha256()) },
                    Scopes = { "betticket_api.result", "betticket_api.detail" },
                    UserClaims =
                    {
                        CustomClaimTypes.SiteName,
                        CustomClaimTypes.CurrencyAgent,
                        CustomClaimTypes.Language
                    }
                },
                new ApiResource("report_api", "Report API")
                {
                    ApiSecrets = { new Secret("report_api.secret".Sha256()) },
                    UserClaims = { CustomClaimTypes.SiteName  },
                    Scopes = { "report_api" }
                },
                new ApiResource("betlist_api", "Bet List API")
                {
                    ApiSecrets = { new Secret("betlist_api.secret".Sha256()) },
                    UserClaims = { JwtClaimTypes.Name, CustomClaimTypes.SiteName  },
                    Scopes = { "betlist_api" }
                },
                new ApiResource("lg_api", "LG API")
                {
                    ApiSecrets = { new Secret("lg_api.secret".Sha256()) },
                    UserClaims = { JwtClaimTypes.Name, CustomClaimTypes.SiteName  },
                    Scopes = { "lg_api" }
                },
                new ApiResource("analytics_api", "Analytics API")
                {
                    ApiSecrets = { new Secret("analytics_api.secret".Sha256()) },
                    UserClaims = { "user_info", "licensee_info" },
                    Scopes = { "analytics_api" }
                }
            };

        public static IEnumerable<ApiScope> ScopeApis =>
            new List<ApiScope>
            {
                new ApiScope("playeradmin_api.external", "Player Admin External API"),
                new ApiScope("playeradmin_api.internal", "Player Admin Internal API")
                {
                     UserClaims =
                     {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimTypes.SiteName,
                        CustomClaimTypes.UserSEQ,
                        CustomClaimTypes.UserID,
                        CustomClaimTypes.Username,
                        CustomClaimTypes.Language,
                        CustomClaimTypes.Administrator,
                        CustomClaimTypes.CurrencyAgent,
                        CustomClaimTypes.WhiteLabelID,
                        CustomClaimTypes.IsMasterLicensee,
                        CustomClaimTypes.UserLevel,
                        CustomClaimTypes.LicenseeId,
                        CustomClaimTypes.IsAllowedCustomerComm,
                        CustomClaimTypes.IsEnableBA,
                        CustomClaimTypes.ActionLink
                     }
                },
                new ApiScope("betticket_api.result", "Bet Ticket API Result"),
                new ApiScope("betticket_api.detail", "Bet Ticket API Detail")
            };

        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                new Client
                {
                    ClientName = "LRF",
                    ClientId = "lrf.client",
                    ClientSecrets = { new Secret("lrf.secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 3600,
                    AllowOfflineAccess = true,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    SlidingRefreshTokenLifetime = 7200,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId, UserInfoResource.IdentityName,
                        "betticket_result", "betticket_detail"
                    }
                },
                new Client
                {
                    ClientName = "Player Admin",
                    ClientId = "playeradmin.client",
                    ClientSecrets = { new Secret("playeradmin.secret".Sha256()) },
                    AllowedGrantTypes = { "lap_token" },
                    AccessTokenType = AccessTokenType.Reference,
                    AccessTokenLifetime = 3600,
                    RequireClientSecret = true,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId, UserInfoResource.IdentityName,
                        "playeradmin_api.internal", "playeradmin_api.external", "betticket_api.result", "betticket_api.detail"
                    },
                    IncludeJwtId = false
                },
                new Client
                {
                    ClientName = "Statement",
                    ClientId = "statement.client",
                    ClientSecrets = { new Secret("statement.secret".Sha256()) },
                    AllowedGrantTypes = { "statement_password" },
                    AccessTokenType = AccessTokenType.Reference,
                    AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId }
                },
                new Client
                {
                    ClientName = "Machine to machine",
                    ClientId = "machine.client",
                    ClientSecrets = { new Secret("machine.secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "playeradmin_api", "report_api", "betlist_api", "lg_api" }
                }
            };
    }
}