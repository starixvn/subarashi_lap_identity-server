using Fanex.Data;

namespace Fanex.Athena.IdentityServer.Core.Repositories
{
    public class DbFactory : IDbFactory
    {
        public IObjectDb GetObjectDb(string spName, int timeout = 120)
            => ObjectDbFactory.CreateInstance(spName, timeout);
    }
}