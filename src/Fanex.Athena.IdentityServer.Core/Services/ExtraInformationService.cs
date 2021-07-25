using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public class ExtraInformationService : IExtraInformationService
    {
        private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

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
            var cacheKey = "Lrf_LicenseeSelBySiteName_" + licenseeName;
            return LockHelper.LockAsync(cacheKey, () => TryGetOrAddFromCache(cacheKey, OneDay, () => GetLicenseeInfoFromDB(licenseeName)));
        }

        public async Task<int> GetUserLevel(int userSEQ, string vendorId)
        {
            const string UspName = "Lrf_GetUserLevelInfo";
            using (var db = _dbFactory.GetObjectDb(UspName))
            {
                db.SetParameterValue("VendorID", vendorId);
                db.SetParameterValue("UserSEQ", userSEQ);

                await db.ExecuteNonQueryAsync();

                var result = Convert.ToInt32(db.GetParameterValue("UserLevel"));
                return result;
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
            => _appSetting.LicenseeOfferShowIP.Contains(licenseeName, StringComparer.OrdinalIgnoreCase);

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

        private Task<LicenseeInformation> GetLicenseeInfoFromDB(string licenseeName)
        {
            const string UspName = "Lrf_LicenseeSelBySiteName";
            var uspParams = new { siteName = licenseeName };

            using (var db = _dbFactory.GetObjectDb(UspName))
            {
                return db.QueryEntityAsync<LicenseeInformation>(uspParams);
            }
        }
    }
}