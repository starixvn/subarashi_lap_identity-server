using Fanex.Athena.IdentityServer.Infrastructure.Constants;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace Fanex.Athena.IdentityServerAPI
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> Ids =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResource
                {
                    Name = "licensee_identity",
                    UserClaims = new [] {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimTypes.VendorID,
                        CustomClaimTypes.SiteName,
                        CustomClaimTypes.UserSEQ,
                        CustomClaimTypes.UserID,
                        CustomClaimTypes.Username,
                        CustomClaimTypes.Language,
                        CustomClaimTypes.ActionLink,
                        CustomClaimTypes.Administrator,
                        CustomClaimTypes.CurrencyAgent,
                        CustomClaimTypes.WhiteLabelID,
                        CustomClaimTypes.IsMasterLicensee,
                        CustomClaimTypes.UserLevel,
                        CustomClaimTypes.LicenseeId,
                        CustomClaimTypes.IsAllowedCustomerComm,
                        CustomClaimTypes.IsEnableBA
                    }
                }
            };

        public static IEnumerable<ApiResource> Apis =>
            new List<ApiResource>
            {
                new ApiResource("playeradmin_api", "Player Admin API")
                {
                    ApiSecrets = new [] { new Secret("playeradmin_api.secret".Sha256()) },
                    UserClaims = new [] {
                        JwtClaimTypes.Name,
                        JwtClaimTypes.Role,
                        CustomClaimTypes.VendorID,
                        CustomClaimTypes.SiteName,
                        CustomClaimTypes.UserSEQ,
                        CustomClaimTypes.UserID,
                        CustomClaimTypes.Username,
                        CustomClaimTypes.UserLevel,
                        CustomClaimTypes.Language,
                        CustomClaimTypes.ActionLink,
                        CustomClaimTypes.Administrator,
                        CustomClaimTypes.CurrencyAgent,
                        CustomClaimTypes.WhiteLabelID,
                        CustomClaimTypes.IsMasterLicensee,
                        CustomClaimTypes.LicenseeId,
                        CustomClaimTypes.IsAllowedCustomerComm,
                        CustomClaimTypes.IsEnableBA
                    },
                    Scopes = new [] { "playeradmin_api" }
                },
                new ApiResource("report_api", "Report API")
                {
                    ApiSecrets = new [] { new Secret("report_api.secret".Sha256()) },
                    UserClaims = new [] { JwtClaimTypes.Name, CustomClaimTypes.SiteName  },
                    Scopes = new [] { "report_api" }
                },
                new ApiResource("betlist_api", "Bet List API")
                {
                    ApiSecrets = new [] { new Secret("betlist_api.secret".Sha256()) },
                    UserClaims = new [] { JwtClaimTypes.Name, CustomClaimTypes.SiteName  },
                    Scopes = new [] { "betlist_api" }
                },
                new ApiResource("betlist_result_api", "Bet List Result API")
                {
                    ApiSecrets = new [] { new Secret("betlist_result_api.secret".Sha256()) },
                    UserClaims = new [] { JwtClaimTypes.Name, CustomClaimTypes.SiteName  },
                    Scopes = new [] { "betlist_result_api" }
                },
                new ApiResource("lg_api", "LG API")
                {
                    ApiSecrets = new [] { new Secret("lg_api.secret".Sha256()) },
                    UserClaims = new [] { JwtClaimTypes.Name, CustomClaimTypes.SiteName  },
                    Scopes = new [] { "lg_api" }
                },
                new ApiResource("analytics_api", "Analytics API")
                {
                    ApiSecrets = new [] { new Secret("analytics_api.secret".Sha256()) },
                    UserClaims = new [] { "user_info", "licensee_info" },
                    Scopes = new [] { "analytics_api" }
                }
            };

        public static IEnumerable<ApiScope> ScopeApis =>
            new List<ApiScope>
            {
                new ApiScope("playeradmin_api", "Player Admin API") {   }
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
                    AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId, "report_api", "betlist_api", "lg_api" }
                },
                new Client
                {
                    ClientName = "Player Admin",
                    ClientId = "playeradmin.client",
                    ClientSecrets = { new Secret("playeradmin.secret".Sha256()) },
                    AllowedGrantTypes = new List<string> { "lap_token" },
                    AccessTokenType = AccessTokenType.Reference,
                    RequireClientSecret = true,
                    AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId, "licensee_identity", "playeradmin_api", "lg_api", "betlist_api", "betlist_result_api" }
                },
                new Client
                {
                    ClientName = "Statement",
                    ClientId = "statement.client",
                    ClientSecrets = { new Secret("statement.secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
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