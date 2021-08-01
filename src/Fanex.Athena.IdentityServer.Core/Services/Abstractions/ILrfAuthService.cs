using Fanex.Athena.IdentityServer.Core.Models;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public interface ILrfAuthService
    {
        Task<(int errorCode, string errorMsg, LrfUserInfo userInfo)> ValidateUser(
            string username, string password, string loginToken, string clientIP);

        Task<int> LoginOnUser(LrfUserInfo userInfo, string lrfSessionId, string clientIP);
    }
}