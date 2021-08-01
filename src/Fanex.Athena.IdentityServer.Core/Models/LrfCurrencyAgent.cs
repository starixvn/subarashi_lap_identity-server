using System.Collections.Generic;

namespace Fanex.Athena.IdentityServer.Core.Models
{
    public class LrfCurrencyAgent
    {
        public int CurrencyId { get; set; }

        public string CurrencyName { get; set; }

        public bool IsActive { get; set; }

        public int AgentId { get; set; }

        public IEnumerable<int> AgentIdList { get; set; }
    }
}