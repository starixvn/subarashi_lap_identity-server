using Fanex.Data;

namespace Fanex.Athena.IdentityServer.Infrastructure.Repositories
{
    public interface IDbFactory
    {
        IObjectDb GetObjectDb(string spName, int timeout = 120);
    }
}