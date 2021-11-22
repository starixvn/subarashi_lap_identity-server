using Fanex.Athena.IdentityServer.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fanex.Athena.IdentityServer.Core.Services
{
    public interface IExtraInformationService
    {
        Task<string> GetOfferedSubProducts(int licenseeId);

        Task<LicenseeInformation> GetLicensee(string licenseeName);

        Task<LicenseeInformation> GetLicenseeBelongingLrfUser(int licenseeId, int lrfUserId);

        Task<IEnumerable<CurrencyAgent>> GetCurrencyAgentListByLrfUser(int licenseeId, int userId);

        Task<LapUserInfo> GetLapUserInfo(int userSEQ, string vendorId);

        Task<bool> IsEnableSingleWallet(string licenseeName);

        bool IsOfferShowIP(string licenseeName);
    }
}