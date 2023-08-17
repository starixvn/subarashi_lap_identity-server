using Fanex.Athena.IdentityServer.Core.Extensions;
using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Athena.IdentityServer.Core.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public class ExtraInformationService : IExtraInformationService
    {
        private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);
        private static readonly IEnumerable<string> SubProductCodeList = new[] { "SLT", "BTC" };
        private readonly Lazy<IReadOnlyDictionary<int, string>> _siteDBConfig;
        private readonly ConcurrentDictionary<string, object> _longLifeData;
        private readonly IMemoryCache _cache;
        private readonly IDbFactory _dbFactory;
        private readonly AppSetting _appSetting;

        public ExtraInformationService(IDbFactory dbFactory, IMemoryCache cache, IOptions<AppSetting> appSetting)
        {
            _dbFactory = dbFactory;
            _cache = cache;
            _appSetting = appSetting.Value;
            _longLifeData = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            _siteDBConfig = new Lazy<IReadOnlyDictionary<int, string>>(() =>
            {
                const string SpName = "Acc_SiteDBConfigSel";
                var config = new Dictionary<int, string>();
                using var db = _dbFactory.GetObjectDb(SpName);
                using var reader = db.ExecuteReader();
                while (reader.Read())
                {
                    config.Add(int.Parse(reader["SiteDBGroupID"].ToString()), reader["AGIP"].ToString());
                }
                return config;
            });
        }

        public Task<LicenseeInformation> GetLicensee(string licenseeName)
        {
            var cacheKey = $"GetLicensee_{licenseeName}";
            return _cache.TryGetOrAddFromCache(cacheKey, OneHour, () => GetLicenseeFromDB(licenseeName));
        }

        public bool IsFakeLicensee(string licenseeName)
            => _appSetting.FakeLicensees.Any(_ => string.Equals(_, licenseeName, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<AuditCurrency> GetAuditHardCodeCurrencies(string licenseeName)
            => _appSetting.AuditHardCodeCurrencies.FirstOrDefault(_ => string.Equals(_.Licensee, licenseeName, StringComparison.OrdinalIgnoreCase))?.AuditCurrencies
                ?? new List<AuditCurrency>();

        public Task<LicenseeInformation> GetLicenseeBelongingLrfUser(int licenseeId, int lrfUserId)
        {
            var cacheKey = $"GetLicenseeBelongingLrfUser_{licenseeId}_{lrfUserId}";
            return _cache.TryGetOrAddFromCache(cacheKey, OneHour, () => GetLicenseeBeLongingLrfUserFromDB(licenseeId, lrfUserId));
        }

        public async Task<string> GetOfferedSubProducts(int licenseeId)
        {
            var cacheKey = $"GetOfferedSubProducts_{licenseeId}";
            return string.Join(",", await _cache.TryGetOrAddFromCache(cacheKey, OneHour, () => GetOfferedSubProductListFromDB(licenseeId)));
        }

        public string GetOffereOddGroups(string licenseeName)
            => _longLifeData.GetOrAdd($"GetOffereOddGroups_{licenseeName}", string.Join(',', _appSetting.OddsGroups
                .Where(_ => _.OfferedBy == null || _.OfferedBy.Any(s => string.Equals(s, licenseeName, StringComparison.OrdinalIgnoreCase)))
                .Select(_ => _.Value))) as string;

        public async Task<IEnumerable<CurrencyAgent>> GetCurrencyAgentListByLrfUser(int licenseeId, int userId)
        {
            const string UspName = "Lrf_SelCurrencyByLicenseeUser";
            using (var db = _dbFactory.GetObjectDb(UspName))
            {
                db.SetParameterValue("UserID", userId);
                db.SetParameterValue("LicenseeID", licenseeId);

                var currencyList = await db.QueryAsync<LrfCurrencyAgent>();
                return currencyList
                    .Where(x => x.IsActive)
                    .GroupBy(x => x.CurrencyId)
                    .Select(x => new CurrencyAgent
                    {
                        CurrencyId = x.Key,
                        Currency = x.First().CurrencyName,
                        AgentId = x.Select(y => y.AgentId).ToArray()
                    });
            }
        }

        public async Task<LapUserInfo> GetLapUserInfo(int userSEQ, string vendorId)
        {
            const string UspName = "Lrf_GetUserLevelInfo";
            using (var db = _dbFactory.GetObjectDb(UspName))
            {
                db.SetParameterValue("VendorID", vendorId);
                db.SetParameterValue("UserSEQ", userSEQ);
                db.SetParameterValue("UserLevel", null);
                db.SetParameterValue("SubsidiaryEnabled", null);
                db.SetParameterValue("OperatorID", null);
                db.SetParameterValue("BaseCurrency", null);

                await db.ExecuteNonQueryAsync();

                var userInfo = new LapUserInfo
                {
                    UserLevel = Convert.ToByte(db.GetParameterValue("UserLevel")),
                    IsEnabledSubsidiary = Convert.ToBoolean(db.GetParameterValue("SubsidiaryEnabled")),
                    OperatorID = Convert.ToString(db.GetParameterValue("OperatorID")),
                    BaseCurrency = db.GetParameterValue("BaseCurrency") != null ?
                     Convert.ToString(db.GetParameterValue("BaseCurrency")) : string.Empty
                };

                return userInfo;
            }
        }

        public async Task<bool> IsEnableSingleWallet(string licenseeName)
        {
            const string KeyName = "EnableSingleWallet";
            const string ExpectedKeyValue = "true";
            var environment = _appSetting.IsUATTW ? "UAT" : "Production";

            var cacheKey = string.Format("{0}_{1}_{2}", KeyName, licenseeName, environment);
            var value = await _cache.TryGetOrAddFromCache(cacheKey, OneHour, () => GetConfigDetailByKeyName(licenseeName, environment, KeyName));

            return !string.IsNullOrEmpty(value) && string.Equals(value, ExpectedKeyValue, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsOfferShowIP(string licenseeName)
            => true;

        private async Task<string> GetConfigDetailByKeyName(string licenseeName, string environment, string keyName)
        {
            const string UspName = "Lrf_GetConfigDetailByKeyName";
            using (var db = _dbFactory.GetObjectDb(UspName))
            {
                db.SetParameterValue("SiteName", licenseeName);
                db.SetParameterValue("Environment", environment);
                db.SetParameterValue("KeyName", keyName);
                db.SetParameterValue("KeyValue", null);

                await db.ExecuteNonQueryAsync();

                var keyValue = Convert.ToString(db.GetParameterValue("KeyValue"));

                return keyValue;
            }
        }

        private async Task<LicenseeInformation> GetLicenseeFromDB(string licenseeName)
        {
            const string UspName = "Lrf_LicenseeSelBySiteName";
            var uspParams = new { siteName = licenseeName };

            using var db = _dbFactory.GetObjectDb(UspName);
            var licenseeInfo = await db.QueryEntityAsync<LicenseeInformation>(uspParams);
            AssignSiteDBInfo(licenseeInfo);
            return licenseeInfo;
        }

        private async Task<LicenseeInformation> GetLicenseeBeLongingLrfUserFromDB(int licenseeId, int lrfUserId)
        {
            const string UspName = "Lrf_LicenseeSel";
            var uspParams = new { UserId = lrfUserId };
            LicenseeInformation licenseeInfo = null;
            using var db = _dbFactory.GetObjectDb(UspName);
            using var reader = await db.ExecuteReaderAsync(uspParams);
            while (reader.Read())
            {
                var id = reader.GetInt32(reader.GetOrdinal("LicenseeID"));
                if (id == licenseeId)
                {
                    licenseeInfo = new LicenseeInformation
                    {
                        Id = id,
                        Name = reader.GetString(reader.GetOrdinal("LicenseeName")),
                        SiteDBGroupID = reader.GetInt16(reader.GetOrdinal("SiteDBGroupID")),
                        IsAllowedCustomerComm = reader.GetBoolean(reader.GetOrdinal("IsAllowedMemberComm")),
                        IsEnableBA = reader.GetBoolean(reader.GetOrdinal("EnableBA"))
                    };
                    AssignSiteDBInfo(licenseeInfo);
                }
            }

            return licenseeInfo;
        }

        private void AssignSiteDBInfo(LicenseeInformation licenseeInfo)
        {
            if (licenseeInfo.SiteDBGroupID != 0)
            {
                licenseeInfo.SiteDBServer = _siteDBConfig.Value.TryGetValue(licenseeInfo.SiteDBGroupID, out string dbServer)
                              ? dbServer : string.Empty;
            }
        }

        private async Task<IEnumerable<string>> GetOfferedSubProductListFromDB(int licenseeId)
        {
            const string uspName = "Lrf_LicenseeProduct3rdSel";
            var allowedSubProductCodeList = new List<string>();
            using (var db = _dbFactory.GetObjectDb(uspName))
            {
                db.SetParameterValue("licenseeid", licenseeId);

                using (var reader = await db.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var productCode = Convert.ToString(reader["productprefix"]);
                        if (SubProductCodeList.Contains(productCode, StringComparer.OrdinalIgnoreCase))
                        {
                            allowedSubProductCodeList.Add(productCode);
                        }
                    }
                }

                return allowedSubProductCodeList;
            }
        }
    }
}