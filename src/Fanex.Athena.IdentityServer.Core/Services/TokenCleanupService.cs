using Fanex.Athena.IdentityServer.Core.Repositories;
using Fanex.Athena.IdentityServer.Core.Settings;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public class TokenCleanupService : ITokenCleanupService
    {
        private readonly IDbFactory _dbFactory;
        private readonly ILogger _logger;
        private readonly OperationalStoreSetting _options;

        public TokenCleanupService(IDbFactory dbFactory, OperationalStoreSetting options, ILogger<TokenCleanupService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _options = options;
        }

        public async Task CleanupExpiredTokenAsync()
        {
            _logger.LogWarning("Start cleanup expired tokens");
            await RemoveGrantsAsync();
        }

        protected virtual async Task RemoveGrantsAsync()
        {
            const string SpName = "LAP_IdentityServer_ExpiredTokenDel";
            using var db = _dbFactory.GetObjectDb(SpName);
            await db.ExecuteNonQueryAsync(new { batchSize = _options.TokenCleanupBatchSize });
        }
    }
}
