using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Models;
using Fanex.Athena.IdentityServer.Core.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public class ExtraInformationService : IExtraInformationService
    {
        private readonly IMemoryCache _cache;
        private readonly IDbFactory _dbFactory;

        public ExtraInformationService(IDbFactory dbFactory, IMemoryCache cache)
        {
            _dbFactory = dbFactory;
            _cache = cache;
        }

        public Task<LicenseeInformation> GetLicensee(string licenseeName)
        {
            var cacheKey = "Lrf_LicenseeSelBySiteName_" + licenseeName;
            return LockHelper.LockAsync(cacheKey, () => TryGetOrAddFromCache(cacheKey, TimeSpan.FromDays(1), () => GetLicenseeInfoFromDB(licenseeName)));
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