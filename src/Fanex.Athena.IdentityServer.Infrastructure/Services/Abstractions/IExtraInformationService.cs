using Fanex.Athena.IdentityServer.Infrastructure.Models;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Infrastructure.Services
{
    public interface IExtraInformationService
    {
        Task<LicenseeInformation> GetLicensee(string licenseeName);

        Task<int> GetUserLevel(int userSEQ, string vendorId);
    }
}