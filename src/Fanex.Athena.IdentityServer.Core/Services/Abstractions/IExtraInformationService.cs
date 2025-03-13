namespace Fanex.Athena.IdentityServer.Core.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Models;
    using Settings;

    public interface IExtraInformationService
    {
        Task<string> GetOfferedSubProducts(int licenseeId);

        string GetOffereOddGroups(string licenseeName);

        Task<LicenseeInformation> GetLicensee(string licenseeName);

        Task<LrfSpecialPlatform> GetSpecialPlatformByLicenseeName(string licenseeName);

        bool IsFakeLicensee(string licenseeName);

        IEnumerable<AuditCurrency> GetAuditHardCodeCurrencies(string licenseeName);

        Task<LicenseeInformation> GetLicenseeBelongingLrfUser(int licenseeId, int lrfUserId);

        Task<IEnumerable<CurrencyAgent>> GetCurrencyAgentListByLrfUser(int licenseeId, int userId);

        Task<IEnumerable<CurrencyAgent>> GetCurrencyAgentForExtService(string siteName, int custId = 0);

        Task<bool> IsEnableSingleWallet(string licenseeName);

        bool IsOfferShowIP(string licenseeName);
    }
}