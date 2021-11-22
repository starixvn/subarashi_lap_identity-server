using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public class ExtraInformationService : IExtraInformationService
    {
        private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);
        private static readonly IEnumerable<string> SubProductCodeList = new [] { "SLT" };

        private readonly IMemoryCache _cache;
        private readonly IDbFactory _dbFactory;
        private readonly AppSetting _appSetting;

        public ExtraInformationService(IDbFactory dbFactory, IMemoryCache cache, IOptions<AppSetting> appSetting)
        {
            _dbFactory = dbFactory;
            _cache = cache;
            _appSetting = appSetting.Value;
        }

        public Task<LicenseeInformation> GetLicensee(string licenseeName)
        {
            var cacheKey = $"GetLicensee_{licenseeName}";
            return LockHelper.LockAsync(cacheKey, () => TryGetOrAddFromCache(cacheKey, OneDay, () => GetLicenseeFromDB(licenseeName)));
        }

        public Task<LicenseeInformation> GetLicenseeBelongingLrfUser(int licenseeId, int lrfUserId)
        {
            var cacheKey = $"GetLicenseeBelongingLrfUser_{licenseeId}_{lrfUserId}";
            return LockHelper.LockAsync(cacheKey, () => TryGetOrAddFromCache(cacheKey, OneDay, () => GetLicenseeBeLongingLrfUserFromDB(licenseeId, lrfUserId)));
        }

        public async Task<string> GetOfferedSubProducts(int licenseeId)
        {
            var cacheKey = $"GetOfferedSubProducts_{licenseeId}";
            return string.Join(",", await LockHelper.LockAsync(cacheKey, () => TryGetOrAddFromCache(cacheKey, OneDay, () => GetOfferedSubProductListFromDB(licenseeId))));
        }

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
            var value = await LockHelper.LockAsync(cacheKey,
                () => TryGetOrAddFromCache(cacheKey, OneDay, () => GetConfigDetailByKeyName(licenseeName, environment, KeyName)));

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

        private async Task<T> TryGetOrAddFromCache<T>(string cacheKey, TimeSpan cacheTime, Func<Task<T>> func)
        {
            if (!_cache.TryGetValue(cacheKey, out T data))
            {
                data = await func.Invoke();
                if (data is not null)
                {
                    _cache.Set(cacheKey, data, cacheTime);
                }
            }
            return data;
        }

        private Task<LicenseeInformation> GetLicenseeFromDB(string licenseeName)
        {
            const string UspName = "Lrf_LicenseeSelBySiteName";
            var uspParams = new { siteName = licenseeName };

            using (var db = _dbFactory.GetObjectDb(UspName))
            {
                return db.QueryEntityAsync<LicenseeInformation>(uspParams);
            }
        }

        private async Task<LicenseeInformation> GetLicenseeBeLongingLrfUserFromDB(int licenseeId, int lrfUserId)
        {
            const string UspName = "Lrf_LicenseeSel";
            var uspParams = new { UserId = lrfUserId };

            using (var db = _dbFactory.GetObjectDb(UspName))
            {
                using (var reader = await db.ExecuteReaderAsync(uspParams))
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(reader.GetOrdinal("LicenseeID"));
                        if (id == licenseeId)
                        {
                            return new LicenseeInformation
                            {
                                Id = id,
                                Name = reader.GetString(reader.GetOrdinal("LicenseeName")),
                                IsAllowedCustomerComm = reader.GetBoolean(reader.GetOrdinal("IsAllowedMemberComm")),
                                IsEnableBA = reader.GetBoolean(reader.GetOrdinal("EnableBA"))
                            };
                        }
                    }
                }
            }

            return null;
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