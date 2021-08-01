using Fanex.Athena.IdentityServer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fanex.Athena.IdentityServer.Core.Helpers
{
    public static class CommonHelper
    {
        private const int CurrencyCnyId = 13;

        private const int CurrencyHkdId = 66;

        private const int KeenOceanLicenseeId = 96;

        public static IEnumerable<CurrencyAgent> ModifyCurrenciesForKeenOcean(IEnumerable<CurrencyAgent> currencyAgents, int licenseeId)
        {
            if (licenseeId == KeenOceanLicenseeId)
            {
                var newCurrencyAgents = currencyAgents.Where(x => x.CurrencyId != CurrencyCnyId).ToList();
                var currency = newCurrencyAgents.FirstOrDefault(x => x.CurrencyId == CurrencyHkdId && !string.Equals(x.Currency, "CNY", StringComparison.OrdinalIgnoreCase));
                if (currency != null)
                {
                    currency.Currency = "CNY";
                }

                return newCurrencyAgents;
            }

            return currencyAgents;
        }
    }
}