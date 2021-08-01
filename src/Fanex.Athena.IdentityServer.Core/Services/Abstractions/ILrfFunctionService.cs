using Fanex.Athena.IdentityServer.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public interface ILrfFunctionService
    {
        Task<IEnumerable<LrfFunction>> GetUserFunctions(int licenseeId, int userId);
    }
}