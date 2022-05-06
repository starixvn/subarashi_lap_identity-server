using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public interface ITokenCleanupService
    {
        Task CleanupExpiredTokenAsync();
    }

}
