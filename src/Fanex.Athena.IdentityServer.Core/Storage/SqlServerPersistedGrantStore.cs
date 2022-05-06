using Fanex.Athena.IdentityServer.Core.Helpers;
using Fanex.Athena.IdentityServer.Core.Repositories;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Storage
{
    public class SqlServerPersistedGrantStore : IPersistedGrantStore
    {
        private readonly ILogger _logger;
        private readonly IDbFactory _dbFactory;

        public SqlServerPersistedGrantStore(IDbFactory dbFactory, ILogger<SqlServerPersistedGrantStore> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public virtual async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            const string SpName = "LAP_IdentityServer_TokenSel";
            using var db = _dbFactory.GetObjectDb(SpName);
            var result = await db.QueryAsync<PersistedGrant>(filter);
            return result;
        }

        public virtual async Task<PersistedGrant> GetAsync(string key)
        {
            const string SpName = "LAP_IdentityServer_TokenSel";
            using var db = _dbFactory.GetObjectDb(SpName);
            var result = await db.QueryEntityAsync<PersistedGrant>(new { key });
            return result;
        }

        public virtual async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            const string SpName = "LAP_IdentityServer_TokenDel";
            using var db = _dbFactory.GetObjectDb(SpName);
            try
            {
                await db.ExecuteNonQueryAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ErrorMessageHelper.GenerateMessage(ex));
            }
        }

        public virtual async Task RemoveAsync(string key)
        {
            const string SpName = "LAP_IdentityServer_TokenDel";
            using var db = _dbFactory.GetObjectDb(SpName);
            try
            {
                await db.ExecuteNonQueryAsync(new { key });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ErrorMessageHelper.GenerateMessage(ex));
            }
        }

        public virtual async Task StoreAsync(PersistedGrant grant)
        {
            const string SpName = "LAP_IdentityServer_TokenInsOrUpd";
            using var db = _dbFactory.GetObjectDb(SpName);
            await db.ExecuteNonQueryAsync(grant);
        }
    }
}
