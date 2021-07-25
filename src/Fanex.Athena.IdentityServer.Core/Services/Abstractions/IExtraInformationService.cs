using Fanex.Athena.IdentityServer.Core.Models;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public interface IExtraInformationService
    {
        Task<LicenseeInformation> GetLicensee(string licenseeName);

        Task<int> GetUserLevel(int userSEQ, string vendorId);

        Task<bool> IsEnableSingleWallet(string licenseeName);

        bool IsOfferShowIP(string licenseeName);
    }
}